using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using satinalmaportali2.Data;
using satinalmaportali2.Helpers;
using satinalmaportali2.Hubs;
using satinalmaportali2.Models;
using satinalmaportali2.Services;
using System.Security.Claims;

namespace satinalmaportali2.Controllers
{
    /// <summary>
    /// Süper Admin Yönetim Merkezi Denetleyicisi.
    /// Yalnızca firma türü 'Süper Admin' (CompanyTypeID = 4) veya 'admin' rolüne sahip hesaplar erişebilir.
    /// Standart kullanıcı arayüzlerinde doğrudan link/buton bulunmaz, gizli ve güvenli URL üzerinden çalışır.
    /// </summary>
    [Authorize]
    [Route("SuperAdmin")]
    [Route("Admin")]
    public class SuperAdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly IHubContext<PortalHub> _hubContext;

        public SuperAdminController(ApplicationDbContext context, EmailService emailService, IHubContext<PortalHub> hubContext)
        {
            _context = context;
            _emailService = emailService;
            _hubContext = hubContext;
        }

        private int GetCurrentCompanyId()
        {
            var cidClaim = User.FindFirst("CompanyID")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(cidClaim) && int.TryParse(cidClaim, out int cid))
            {
                return cid;
            }
            return 0;
        }

        /// <summary>
        /// Aktif kullanıcının Süper Admin yetkisini 3 katmanlı doğrular (Claims, DB Firma Türü, E-posta).
        /// </summary>
        private async Task<bool> CheckIsSuperAdminAsync()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return false;
            }

            var userType = User.FindFirst("UserType")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;
            var compTypeClaim = User.FindFirst("CompanyTypeID")?.Value;

            if (string.Equals(userType, "admin", StringComparison.OrdinalIgnoreCase) || compTypeClaim == "4")
            {
                return true;
            }

            int companyId = GetCurrentCompanyId();
            if (companyId > 0)
            {
                var company = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == companyId);
                if (company != null)
                {
                    if (company.CompanyTypeID == 4 || string.Equals(company.CompanyMail, "admin@tedarikportal.com", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static List<CategoryAdminDto> BuildCategoryHierarchy(List<Category> allCategories, int? parentId, string? parentName)
        {
            var children = allCategories
                .Where(c => parentId == null 
                    ? (!c.ParentCategoryID.HasValue || c.ParentCategoryID.Value == 0)
                    : (c.ParentCategoryID == parentId.Value))
                .OrderBy(c => c.CategoryName)
                .ToList();

            var result = new List<CategoryAdminDto>();
            foreach (var c in children)
            {
                var node = new CategoryAdminDto
                {
                    CategoryID = c.CategoryID,
                    CategoryName = c.CategoryName ?? "İsimsiz Kategori",
                    ParentCategoryID = c.ParentCategoryID,
                    ParentCategoryName = parentName
                };
                node.SubCategories = BuildCategoryHierarchy(allCategories, c.CategoryID, c.CategoryName);
                node.SubCategoryCount = node.SubCategories.Count;
                result.Add(node);
            }
            return result;
        }

        private static void FlattenCategoriesForDropdown(List<CategoryAdminDto> nodes, int depth, List<CategoryDropdownDto> result)
        {
            foreach (var n in nodes)
            {
                string indent = depth > 0 ? (new string('—', depth) + " ") : "";
                result.Add(new CategoryDropdownDto
                {
                    CategoryID = n.CategoryID,
                    FormattedName = indent + n.CategoryName,
                    Level = depth
                });
                if (n.SubCategories.Count > 0)
                {
                    FlattenCategoriesForDropdown(n.SubCategories, depth + 1, result);
                }
            }
        }

        /// <summary>
        /// Süper Admin Ana Kontrol Paneli (Genel Bakış, Kategoriler, Firmalar, Ayarlar)
        /// </summary>
        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index(string? tab = "overview", string? search = null, int? filterType = null, string? status = null)
        {
            if (!await CheckIsSuperAdminAsync())
            {
                TempData["ErrorMessage"] = "Bu yönetim paneline yalnızca Süper Admin hesapları erişebilir.";
                return RedirectToAction("Index", "Home");
            }

            var viewModel = new SuperAdminDashboardViewModel
            {
                ActiveTab = string.IsNullOrWhiteSpace(tab) ? "overview" : tab.ToLowerInvariant(),
                SearchQuery = search,
                FilterCompanyType = filterType,
                FilterOrderStatus = status
            };

            try
            {
                // 1. Firma Türleri ve Üyelik Türleri
                viewModel.AvailableCompanyTypes = await _context.CompanyTypes.AsNoTracking().OrderBy(ct => ct.CompanyTypeID).ToListAsync();
                viewModel.AvailableMembershipTypes = await _context.MembershipTypes.AsNoTracking().OrderBy(mt => mt.MembershipTypeID).ToListAsync();

                var typeMap = viewModel.AvailableCompanyTypes.ToDictionary(ct => ct.CompanyTypeID ?? 0, ct => ct.CompanyTypeName ?? "Bilinmiyor");
                var memberMap = viewModel.AvailableMembershipTypes.ToDictionary(mt => mt.MembershipTypeID ?? 0, mt => mt.MembershipTypeName ?? "Standart");

                // 2. Kategoriler (N-Seviyeli Tam Hiyerarşik Ağaç Yapısı)
                var dbCategories = await _context.Category.AsNoTracking().ToListAsync();
                var catMap = dbCategories.ToDictionary(c => c.CategoryID, c => c.CategoryName ?? "");

                var categoryTree = BuildCategoryHierarchy(dbCategories, null, null);

                // Ağaca bağlanmamış (yetim / geçersiz parent ID'ye sahip) kategorileri de ekle
                var visitedIds = new HashSet<int>();
                void CollectIds(List<CategoryAdminDto> nodes)
                {
                    foreach (var n in nodes)
                    {
                        visitedIds.Add(n.CategoryID);
                        CollectIds(n.SubCategories);
                    }
                }
                CollectIds(categoryTree);

                var orphans = dbCategories.Where(c => !visitedIds.Contains(c.CategoryID)).ToList();
                foreach (var orphan in orphans)
                {
                    var orphanNode = new CategoryAdminDto
                    {
                        CategoryID = orphan.CategoryID,
                        CategoryName = orphan.CategoryName ?? "İsimsiz Kategori",
                        ParentCategoryID = orphan.ParentCategoryID,
                        ParentCategoryName = orphan.ParentCategoryID.HasValue && catMap.TryGetValue(orphan.ParentCategoryID.Value, out var pName) ? pName : "Bilinmeyen Üst Kategori",
                        SubCategories = BuildCategoryHierarchy(dbCategories, orphan.CategoryID, orphan.CategoryName)
                    };
                    orphanNode.SubCategoryCount = orphanNode.SubCategories.Count;
                    categoryTree.Add(orphanNode);
                }

                viewModel.Categories = categoryTree;

                var parentDropdownList = new List<CategoryDropdownDto>();
                FlattenCategoriesForDropdown(categoryTree, 0, parentDropdownList);
                viewModel.AvailableParentCategories = parentDropdownList;

                // 3. Firmalar
                var dbCompanies = await _context.Companies.AsNoTracking().OrderByDescending(c => c.CompanyID).ToListAsync();
                var dbAddresses = await _context.Addresses.AsNoTracking().ToDictionaryAsync(a => a.AddressID, a => a.AddressText ?? "");

                var requestsBySender = await _context.Requests.AsNoTracking()
                    .Where(r => r.SenderID.HasValue)
                    .GroupBy(r => r.SenderID!.Value)
                    .Select(g => new { SenderID = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.SenderID, x => x.Count);

                var offersBySupplier = await _context.RequestOffers.AsNoTracking()
                    .GroupBy(o => o.SupplierID)
                    .Select(g => new { SupplierID = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.SupplierID, x => x.Count);

                viewModel.Companies = dbCompanies.Select(c => new CompanyAdminDto
                {
                    CompanyID = c.CompanyID,
                    CompanyName = c.CompanyName ?? "İsimsiz Firma",
                    CompanyOfficial = c.CompanyOfficial ?? "-",
                    CompanyMail = c.CompanyMail ?? "-",
                    CompanyPhone = c.CompanyPhone,
                    TCNO = c.TCNO,
                    TaxOffice = c.TaxOffice,
                    CompanyTypeID = c.CompanyTypeID,
                    CompanyTypeName = (c.CompanyTypeID == 4 || string.Equals(c.CompanyMail, "admin@tedarikportal.com", StringComparison.OrdinalIgnoreCase)) ? "Süper Admin" : "Kurumsal Hesap (Alıcı & Tedarikçi)",
                    MembershipTypeID = c.MembershipTypeID,
                    MembershipTypeName = c.MembershipTypeID.HasValue && memberMap.TryGetValue(c.MembershipTypeID.Value, out var mName) ? mName : "Standart",
                    CompanyLogo = c.CompanyLogo,
                    CompanyDetailText = c.CompanyDetailText,
                    AddressText = c.AddressID.HasValue && dbAddresses.TryGetValue(c.AddressID.Value, out var addr) ? addr : null,
                    RequestCount = requestsBySender.TryGetValue(c.CompanyID, out var rCount) ? rCount : 0,
                    OfferCount = offersBySupplier.TryGetValue(c.CompanyID, out var oCount) ? oCount : 0
                }).ToList();

                // 4. İstatistikler
                var dbRequests = await _context.Requests.AsNoTracking().ToListAsync();
                var dbOffers = await _context.RequestOffers.AsNoTracking().ToListAsync();
                var dbItems = await _context.Items.AsNoTracking().OrderByDescending(i => i.ItemID).ToListAsync();

                viewModel.Stats = new SuperAdminStatsDto
                {
                    TotalCompanies = dbCompanies.Count,
                    CorporateCompaniesCount = dbCompanies.Count(c => c.CompanyTypeID != 4 && !string.Equals(c.CompanyMail, "admin@tedarikportal.com", StringComparison.OrdinalIgnoreCase)),
                    SupplierCount = 0,
                    BuyerCount = 0,
                    BothCount = dbCompanies.Count(c => c.CompanyTypeID != 4 && !string.Equals(c.CompanyMail, "admin@tedarikportal.com", StringComparison.OrdinalIgnoreCase)),
                    SuperAdminCount = dbCompanies.Count(c => c.CompanyTypeID == 4 || string.Equals(c.CompanyMail, "admin@tedarikportal.com", StringComparison.OrdinalIgnoreCase)),

                    TotalCategories = dbCategories.Count,
                    RootCategoriesCount = dbCategories.Count(c => !c.ParentCategoryID.HasValue || c.ParentCategoryID.Value == 0),
                    SubCategoriesCount = dbCategories.Count(c => c.ParentCategoryID.HasValue && c.ParentCategoryID.Value > 0),

                    TotalRequests = dbRequests.Count,
                    TotalOffers = dbOffers.Count,
                    TotalEstimatedBudget = dbRequests.Sum(r => r.EstimatedBudget ?? 0),
                    TotalOfferedVolume = dbOffers.Sum(o => o.OfferedPrice),

                    TotalListings = dbItems.Count,
                    TotalListingValue = (decimal)dbItems.Sum(i => (i.ItemPrice ?? 0) * (i.Quantity ?? 0))
                };

                // 5. Canlı Vitrin & Satış İlanları Listesi
                viewModel.RecentListings = dbItems.Select(item =>
                {
                    string catName = "Endüstriyel Ürünler";
                    if (item.CategoryID.HasValue && catMap.TryGetValue(item.CategoryID.Value, out var cName))
                    {
                        catName = cName;
                    }
                    else if (!string.IsNullOrWhiteSpace(item.Properties) && item.Properties.StartsWith("Kategori: "))
                    {
                        catName = item.Properties.Substring("Kategori: ".Length);
                    }

                    return new ItemSummaryDto
                    {
                        ItemID = item.ItemID,
                        ItemName = item.ItemName ?? "İsimsiz Ürün",
                        Brand = item.Brand ?? "Onaylı Tedarikçi",
                        CategoryName = catName,
                        UnitPrice = (decimal)(item.ItemPrice ?? 0),
                        Quantity = item.Quantity ?? 0,
                        Properties = item.Properties,
                        ItemImage = null
                    };
                }).ToList();

                // 6. Açık / Mevcut Talepler Listesi (Simülasyon ve Hızlı Teklif için)
                viewModel.OpenRequests = dbRequests.OrderByDescending(r => r.RequestID).Select(r => new RequestSummaryDto
                {
                    RequestID = r.RequestID,
                    ProductName = r.ProductName ?? "İsimsiz Talep",
                    BuyerInfo = r.BuyerInfo ?? "Alıcı Firma",
                    SenderID = r.SenderID,
                    Status = r.Status ?? "Beklemede",
                    Quantity = r.Quantity ?? 1,
                    EstimatedBudget = r.EstimatedBudget,
                    OfferCount = dbOffers.Count(o => o.RequestID == r.RequestID),
                    RequestDate = r.RequestDate
                }).ToList();

                // 7. Değerlendirmeler Listesi & Siparişler (10★ İtibar Karnesi)
                var dbReviews = await _context.Reviews
                    .Include(r => r.Order)
                    .Include(r => r.TargetCompany)
                    .Include(r => r.ReviewerCompany)
                    .AsNoTracking()
                    .OrderByDescending(r => r.ReviewDate)
                    .ToListAsync();

                var dbOrders = await _context.Orders
                    .Include(o => o.OrderItems)
                    .AsNoTracking()
                    .OrderByDescending(o => o.OrderID)
                    .ToListAsync();

                viewModel.Reviews = dbReviews.Select(r => new ReviewAdminDto
                {
                    ReviewID = r.ReviewID,
                    CompanyID = r.CompanyID,
                    TargetCompanyName = r.TargetCompany?.CompanyName ?? (!string.IsNullOrWhiteSpace(r.Order?.SupplierCompanyName) ? r.Order.SupplierCompanyName : "Firma #" + r.CompanyID),
                    ReviewerCompanyID = r.ReviewerCompanyID,
                    ReviewerName = !string.IsNullOrWhiteSpace(r.ReviewerName) ? r.ReviewerName : (r.ReviewerCompany?.CompanyName ?? "Onaylı B2B Kullanıcı"),
                    ReviewType = r.ReviewType ?? "BuyerToSeller",
                    ReviewScore = r.ReviewScore ?? 10.0,
                    DeliveryScore = r.DeliveryScore,
                    QualityScore = r.QualityScore,
                    CommunicationScore = r.CommunicationScore,
                    PaymentPunctualityScore = r.DeliveryScore,
                    ReviewText = r.ReviewText,
                    ReviewDate = r.ReviewDate,
                    OrderNumber = r.Order?.OrderNumber,
                    OrderID = r.OrderID,
                    IsVerifiedPurchase = r.IsVerifiedPurchase
                }).ToList();

                viewModel.Orders = dbOrders.Select(o =>
                {
                    var distinctSuppliers = o.OrderItems
                        .Select(i => i.SupplierCompanyID)
                        .Where(id => id.HasValue && id.Value > 0)
                        .Distinct()
                        .Count();

                    if (distinctSuppliers == 0 && o.SupplierCompanyID.HasValue) distinctSuppliers = 1;

                    return new OrderAdminDto
                    {
                        OrderID = o.OrderID,
                        OrderNumber = o.OrderNumber,
                        BuyerCompanyID = o.BuyerCompanyID,
                        BuyerCompanyName = !string.IsNullOrWhiteSpace(o.BuyerCompanyName) ? o.BuyerCompanyName : $"Firma #{o.BuyerCompanyID}",
                        BuyerOfficial = o.ReceiverName,
                        BuyerEmail = o.BuyerEmail,
                        BuyerPhone = o.BuyerPhone ?? o.ReceiverPhone,
                        SupplierCompanyID = o.SupplierCompanyID,
                        SupplierCompanyName = !string.IsNullOrWhiteSpace(o.SupplierCompanyName) ? o.SupplierCompanyName : "Çoklu Tedarikçi",
                        SubTotal = o.TotalAmount,
                        TaxAmount = o.TaxAmount,
                        GrandTotal = o.GrandTotal,
                        PaymentStatus = o.PaymentStatus ?? "Ödendi",
                        PaymentMethod = o.PaymentMethod,
                        OrderStatus = o.OrderStatus ?? "Onaylandı",
                        OrderDate = o.OrderDate,
                        DeliveryAddress = o.DeliveryAddress ?? (string.IsNullOrWhiteSpace(o.DeliveryCity) ? "-" : o.DeliveryCity),
                        ShippingCarrier = o.ShippingCarrier,
                        TrackingNumber = o.TrackingNumber,
                        Notes = o.Notes,
                        SupplierCount = distinctSuppliers,
                        Items = o.OrderItems.Select(i => new OrderItemAdminDto
                        {
                            OrderItemID = i.OrderItemID,
                            OrderID = i.OrderID,
                            ItemID = i.ItemID,
                            ProductName = i.ProductName ?? "Ürün",
                            Sku = i.Sku,
                            CategoryName = i.CategoryName ?? "Kategori Belirtilmemiş",
                            UnitPrice = i.UnitPrice,
                            Quantity = i.Quantity,
                            Unit = "Adet",
                            LineTotal = i.LineTotal,
                            SupplierID = i.SupplierCompanyID,
                            SupplierCompanyName = i.SupplierCompanyName ?? "Onaylı Tedarikçi",
                            ItemStatus = i.ItemStatus ?? o.OrderStatus ?? "Hazırlanıyor",
                            TrackingNumber = i.TrackingNumber ?? o.TrackingNumber,
                            ShippingCarrier = i.ShippingCarrier ?? o.ShippingCarrier
                        }).ToList()
                    };
                }).ToList();

                viewModel.AvailableOrders = dbOrders.Select(o => new OrderSummaryDto
                {
                    OrderID = o.OrderID,
                    OrderNumber = o.OrderNumber,
                    BuyerCompanyName = o.BuyerCompanyName,
                    SupplierCompanyName = o.SupplierCompanyName,
                    GrandTotal = o.GrandTotal,
                    OrderDate = o.OrderDate,
                    OrderStatus = o.OrderStatus
                }).ToList();

                viewModel.Stats.TotalOrders = dbOrders.Count;
                viewModel.Stats.TotalOrderVolume = dbOrders.Sum(o => o.GrandTotal);
                viewModel.Stats.TotalOrderItemsCount = dbOrders.Sum(o => o.OrderItems.Count);
                viewModel.Stats.MultiSupplierOrdersCount = viewModel.Orders.Count(o => o.SupplierCount > 1);

                viewModel.Stats.TotalReviews = dbReviews.Count;
                viewModel.Stats.AveragePlatformScore = dbReviews.Any() ? Math.Round(dbReviews.Average(r => r.ReviewScore ?? 10.0), 1) : 10.0;
                viewModel.Stats.BuyerToSellerReviewsCount = dbReviews.Count(r => r.ReviewType == "BuyerToSeller");
                viewModel.Stats.SellerToBuyerReviewsCount = dbReviews.Count(r => r.ReviewType == "SellerToBuyer");

                // Filtreleme (Kategoriler & Firmalar & Siparişler)
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var q = search.Trim().ToLowerInvariant();
                    if (viewModel.ActiveTab == "categories")
                    {
                        viewModel.Categories = viewModel.Categories.Where(c => 
                            c.CategoryName.ToLowerInvariant().Contains(q) || 
                            c.SubCategories.Any(sc => sc.CategoryName.ToLowerInvariant().Contains(q)) ||
                            (c.ParentCategoryName != null && c.ParentCategoryName.ToLowerInvariant().Contains(q))
                        ).ToList();
                    }
                    else if (viewModel.ActiveTab == "companies")
                    {
                        viewModel.Companies = viewModel.Companies.Where(c => c.CompanyName.ToLowerInvariant().Contains(q) || c.CompanyOfficial.ToLowerInvariant().Contains(q) || c.CompanyMail.ToLowerInvariant().Contains(q) || (c.TCNO != null && c.TCNO.Contains(q))).ToList();
                    }
                    else if (viewModel.ActiveTab == "orders")
                    {
                        viewModel.Orders = viewModel.Orders.Where(o => 
                            o.OrderNumber.ToLowerInvariant().Contains(q) || 
                            o.BuyerCompanyName.ToLowerInvariant().Contains(q) || 
                            (o.SupplierCompanyName != null && o.SupplierCompanyName.ToLowerInvariant().Contains(q)) ||
                            (o.TrackingNumber != null && o.TrackingNumber.ToLowerInvariant().Contains(q)) ||
                            o.Items.Any(it => it.ProductName.ToLowerInvariant().Contains(q) || (it.SupplierCompanyName != null && it.SupplierCompanyName.ToLowerInvariant().Contains(q)))
                        ).ToList();
                    }
                }

                if (filterType.HasValue && filterType.Value > 0)
                {
                    viewModel.Companies = viewModel.Companies.Where(c => c.CompanyTypeID == filterType.Value).ToList();
                }

                if (!string.IsNullOrWhiteSpace(viewModel.FilterOrderStatus) && viewModel.FilterOrderStatus != "all")
                {
                    viewModel.Orders = viewModel.Orders.Where(o => o.OrderStatus == viewModel.FilterOrderStatus).ToList();
                }
            }
            catch (Exception ex)
            {
                TempData["AdminError"] = "Veriler yüklenirken hata oluştu: " + ex.Message;
            }

            return View(viewModel);
        }

        // =========================================================================
        // KATEGORİ YÖNETİMİ ACTIONS
        // =========================================================================

        [HttpPost]
        private bool IsAjaxRequest() =>
            Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
            (Request.Headers.Accept.ToString().Contains("application/json") && !Request.Headers.Accept.ToString().Contains("text/html"));

        [HttpPost]
        [Route("CreateCategory")]
        public async Task<IActionResult> CreateCategory(string categoryName, int? parentCategoryId)
        {
            if (!await CheckIsSuperAdminAsync())
            {
                if (IsAjaxRequest()) return Json(new { success = false, message = "Yetkisiz işlem." });
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                var reqMsg = "Kategori adı boş bırakılamaz.";
                if (IsAjaxRequest()) return Json(new { success = false, message = reqMsg });
                TempData["AdminError"] = reqMsg;
                return RedirectToAction(nameof(Index), new { tab = "categories" });
            }

            try
            {
                var newCat = new Category
                {
                    CategoryName = categoryName.Trim(),
                    ParentCategoryID = (parentCategoryId.HasValue && parentCategoryId.Value > 0) ? parentCategoryId.Value : null
                };

                _context.Category.Add(newCat);
                await _context.SaveChangesAsync();

                try
                {
                    await _hubContext.Clients.All.SendAsync("OnCategoryCreated", new
                    {
                        categoryId = newCat.CategoryID,
                        categoryName = newCat.CategoryName,
                        parentCategoryId = newCat.ParentCategoryID
                    });
                }
                catch (Exception hubEx)
                {
                    Console.WriteLine($"SignalR SuperAdmin CreateCategory error: {hubEx.Message}");
                }

                var succMsg = $"'{newCat.CategoryName}' kategorisi başarıyla eklendi!";
                if (IsAjaxRequest()) return Json(new { success = true, message = succMsg, categoryId = newCat.CategoryID, categoryName = newCat.CategoryName });
                TempData["AdminSuccess"] = succMsg;
            }
            catch (Exception ex)
            {
                var errMsg = "Kategori ekleme hatası: " + ex.Message;
                if (IsAjaxRequest()) return Json(new { success = false, message = errMsg });
                TempData["AdminError"] = errMsg;
            }

            return RedirectToAction(nameof(Index), new { tab = "categories" });
        }

        [HttpPost]
        [Route("UpdateCategory")]
        public async Task<IActionResult> UpdateCategory(EditCategoryInputModel model)
        {
            if (!await CheckIsSuperAdminAsync())
            {
                if (IsAjaxRequest()) return Json(new { success = false, message = "Yetkisiz işlem." });
                return Forbid();
            }

            if (!ModelState.IsValid || model.CategoryID <= 0)
            {
                var errInvalid = "Geçersiz kategori bilgisi.";
                if (IsAjaxRequest()) return Json(new { success = false, message = errInvalid });
                TempData["AdminError"] = errInvalid;
                return RedirectToAction(nameof(Index), new { tab = "categories" });
            }

            try
            {
                var category = await _context.Category.FirstOrDefaultAsync(c => c.CategoryID == model.CategoryID);
                if (category == null)
                {
                    var notFoundMsg = "Güncellenecek kategori bulunamadı.";
                    if (IsAjaxRequest()) return Json(new { success = false, message = notFoundMsg });
                    TempData["AdminError"] = notFoundMsg;
                    return RedirectToAction(nameof(Index), new { tab = "categories" });
                }

                if (model.ParentCategoryID == model.CategoryID)
                {
                    var selfParentMsg = "Bir kategori kendisinin üst kategorisi olamaz.";
                    if (IsAjaxRequest()) return Json(new { success = false, message = selfParentMsg });
                    TempData["AdminError"] = selfParentMsg;
                    return RedirectToAction(nameof(Index), new { tab = "categories" });
                }

                category.CategoryName = model.CategoryName.Trim();
                category.ParentCategoryID = (model.ParentCategoryID.HasValue && model.ParentCategoryID.Value > 0) ? model.ParentCategoryID.Value : null;

                await _context.SaveChangesAsync();

                try
                {
                    await _hubContext.Clients.All.SendAsync("OnCategoryUpdated", new
                    {
                        categoryId = category.CategoryID,
                        categoryName = category.CategoryName,
                        parentCategoryId = category.ParentCategoryID
                    });
                }
                catch (Exception hubEx)
                {
                    Console.WriteLine($"SignalR SuperAdmin UpdateCategory error: {hubEx.Message}");
                }

                var succMsg = $"'{category.CategoryName}' kategorisi başarıyla güncellendi!";
                if (IsAjaxRequest()) return Json(new { success = true, message = succMsg, categoryId = category.CategoryID, categoryName = category.CategoryName });
                TempData["AdminSuccess"] = succMsg;
            }
            catch (Exception ex)
            {
                var errMsg = "Kategori güncelleme hatası: " + ex.Message;
                if (IsAjaxRequest()) return Json(new { success = false, message = errMsg });
                TempData["AdminError"] = errMsg;
            }

            return RedirectToAction(nameof(Index), new { tab = "categories" });
        }

        [HttpPost]
        [Route("DeleteCategory")]
        public async Task<IActionResult> DeleteCategory(int categoryId)
        {
            if (!await CheckIsSuperAdminAsync())
            {
                if (IsAjaxRequest()) return Json(new { success = false, message = "Yetkisiz işlem." });
                return Forbid();
            }

            try
            {
                var category = await _context.Category.FirstOrDefaultAsync(c => c.CategoryID == categoryId);
                if (category == null)
                {
                    var notFoundMsg = "Silinecek kategori bulunamadı.";
                    if (IsAjaxRequest()) return Json(new { success = false, message = notFoundMsg });
                    TempData["AdminError"] = notFoundMsg;
                    return RedirectToAction(nameof(Index), new { tab = "categories" });
                }

                // Alt kategorileri kontrol et
                var subCats = await _context.Category.Where(c => c.ParentCategoryID == categoryId).ToListAsync();
                foreach (var sc in subCats)
                {
                    sc.ParentCategoryID = null; // Alt kategorileri ana kategoriye çek
                }

                _context.Category.Remove(category);
                await _context.SaveChangesAsync();

                try
                {
                    await _hubContext.Clients.All.SendAsync("OnCategoryDeleted", new { categoryId = categoryId });
                }
                catch (Exception hubEx)
                {
                    Console.WriteLine($"SignalR SuperAdmin DeleteCategory error: {hubEx.Message}");
                }

                var succMsg = $"'{category.CategoryName}' kategorisi başarıyla silindi.";
                if (IsAjaxRequest()) return Json(new { success = true, message = succMsg, categoryId = categoryId });
                TempData["AdminSuccess"] = succMsg;
            }
            catch (Exception ex)
            {
                var errMsg = "Kategori silinirken hata oluştu: " + ex.Message;
                if (IsAjaxRequest()) return Json(new { success = false, message = errMsg });
                TempData["AdminError"] = errMsg;
            }

            return RedirectToAction(nameof(Index), new { tab = "categories" });
        }

        [HttpGet]
        [Route("GetCategory/{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            if (!await CheckIsSuperAdminAsync()) return Forbid();

            var category = await _context.Category.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryID == id);
            if (category == null) return NotFound(new { success = false, message = "Kategori bulunamadı." });

            return Json(new
            {
                success = true,
                categoryId = category.CategoryID,
                categoryName = category.CategoryName,
                parentCategoryId = category.ParentCategoryID
            });
        }

        // =========================================================================
        // FİRMA VE FİRMA AYARLARI ACTIONS
        // =========================================================================

        [HttpPost]
        [Route("UpdateCompany")]
        public async Task<IActionResult> UpdateCompany(EditCompanyInputModel model)
        {
            if (!await CheckIsSuperAdminAsync())
            {
                if (IsAjaxRequest()) return Json(new { success = false, message = "Yetkisiz işlem." });
                return Forbid();
            }

            if (!ModelState.IsValid || model.CompanyID <= 0)
            {
                var reqMsg = "Lütfen tüm zorunlu firma bilgilerini eksiksiz doldurun.";
                if (IsAjaxRequest()) return Json(new { success = false, message = reqMsg });
                TempData["AdminError"] = reqMsg;
                return RedirectToAction(nameof(Index), new { tab = "companies" });
            }

            try
            {
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyID == model.CompanyID);
                if (company == null)
                {
                    var notFoundMsg = "Güncellenecek firma bulunamadı.";
                    if (IsAjaxRequest()) return Json(new { success = false, message = notFoundMsg });
                    TempData["AdminError"] = notFoundMsg;
                    return RedirectToAction(nameof(Index), new { tab = "companies" });
                }

                // E-posta çakışma kontrolü
                var emailTrim = model.CompanyMail.Trim().ToLowerInvariant();
                var emailExists = await _context.Companies.AnyAsync(c => c.CompanyID != model.CompanyID && c.CompanyMail != null && c.CompanyMail.ToLower() == emailTrim);
                if (emailExists)
                {
                    var emailExistMsg = "Bu e-posta adresi başka bir firma tarafından kullanılmaktadır.";
                    if (IsAjaxRequest()) return Json(new { success = false, message = emailExistMsg });
                    TempData["AdminError"] = emailExistMsg;
                    return RedirectToAction(nameof(Index), new { tab = "companies" });
                }

                company.CompanyName = model.CompanyName.Trim();
                company.CompanyOfficial = model.CompanyOfficial.Trim();
                company.CompanyMail = emailTrim;
                company.CompanyPhone = string.IsNullOrWhiteSpace(model.CompanyPhone) ? null : model.CompanyPhone.Trim();
                company.TCNO = string.IsNullOrWhiteSpace(model.TCNO) ? null : model.TCNO.Trim();
                company.TaxOffice = string.IsNullOrWhiteSpace(model.TaxOffice) ? null : model.TaxOffice.Trim();
                company.CompanyTypeID = model.CompanyTypeID == 4 ? 4 : 3;
                company.MembershipTypeID = model.MembershipTypeID ?? company.MembershipTypeID ?? 1;
                company.CompanyDetailText = string.IsNullOrWhiteSpace(model.CompanyDetailText) ? null : model.CompanyDetailText.Trim();

                // Adres Güncelleme
                if (!string.IsNullOrWhiteSpace(model.AddressText))
                {
                    if (company.AddressID.HasValue)
                    {
                        var addr = await _context.Addresses.FirstOrDefaultAsync(a => a.AddressID == company.AddressID.Value);
                        if (addr != null)
                        {
                            addr.AddressText = model.AddressText.Trim();
                        }
                    }
                    else
                    {
                        var newAddr = new Addresses
                        {
                            AddressText = model.AddressText.Trim(),
                            CompanyID = company.CompanyID
                        };
                        _context.Addresses.Add(newAddr);
                        await _context.SaveChangesAsync();
                        company.AddressID = newAddr.AddressID;
                    }
                }

                await _context.SaveChangesAsync();
                var succMsg = $"'{company.CompanyName}' firması ve tüm ayarları başarıyla güncellendi!";
                if (IsAjaxRequest()) return Json(new { success = true, message = succMsg, companyId = company.CompanyID, companyName = company.CompanyName });
                TempData["AdminSuccess"] = succMsg;
            }
            catch (Exception ex)
            {
                var errMsg = "Firma güncelleme hatası: " + ex.Message;
                if (IsAjaxRequest()) return Json(new { success = false, message = errMsg });
                TempData["AdminError"] = errMsg;
            }

            return RedirectToAction(nameof(Index), new { tab = "companies" });
        }

        [HttpPost]
        [Route("ResetCompanyPassword")]
        public async Task<IActionResult> ResetCompanyPassword(ResetPasswordInputModel model)
        {
            if (!await CheckIsSuperAdminAsync())
            {
                if (IsAjaxRequest()) return Json(new { success = false, message = "Yetkisiz işlem." });
                return Forbid();
            }

            if (model.CompanyID <= 0 || string.IsNullOrWhiteSpace(model.NewPassword) || model.NewPassword.Length < 6)
            {
                var reqMsg = "Şifre en az 6 karakter olmalıdır.";
                if (IsAjaxRequest()) return Json(new { success = false, message = reqMsg });
                TempData["AdminError"] = reqMsg;
                return RedirectToAction(nameof(Index), new { tab = "companies" });
            }

            try
            {
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyID == model.CompanyID);
                if (company == null)
                {
                    var notFoundMsg = "Firma bulunamadı.";
                    if (IsAjaxRequest()) return Json(new { success = false, message = notFoundMsg });
                    TempData["AdminError"] = notFoundMsg;
                    return RedirectToAction(nameof(Index), new { tab = "companies" });
                }

                company.CompanyPassword = PasswordHasher.HashPassword(model.NewPassword.Trim());
                await _context.SaveChangesAsync();

                var succMsg = $"'{company.CompanyName}' firmasının şifresi başarıyla sıfırlandı!";
                if (IsAjaxRequest()) return Json(new { success = true, message = succMsg, companyId = company.CompanyID });
                TempData["AdminSuccess"] = succMsg;
            }
            catch (Exception ex)
            {
                var errMsg = "Şifre sıfırlama hatası: " + ex.Message;
                if (IsAjaxRequest()) return Json(new { success = false, message = errMsg });
                TempData["AdminError"] = errMsg;
            }

            return RedirectToAction(nameof(Index), new { tab = "companies" });
        }

        [HttpPost]
        [Route("QuickChangeCompanyType")]
        public async Task<IActionResult> QuickChangeCompanyType(int companyId, int companyTypeId)
        {
            if (!await CheckIsSuperAdminAsync())
            {
                if (IsAjaxRequest()) return Json(new { success = false, message = "Yetkisiz işlem." });
                return Forbid();
            }

            try
            {
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyID == companyId);
                if (company == null)
                {
                    var notFoundMsg = "Firma bulunamadı.";
                    if (IsAjaxRequest()) return Json(new { success = false, message = notFoundMsg });
                    TempData["AdminError"] = notFoundMsg;
                    return RedirectToAction(nameof(Index), new { tab = "companies" });
                }

                company.CompanyTypeID = companyTypeId;
                await _context.SaveChangesAsync();

                var succMsg = $"'{company.CompanyName}' firmasının türü güncellendi!";
                if (IsAjaxRequest()) return Json(new { success = true, message = succMsg, companyId = companyId, companyTypeId = companyTypeId });
                TempData["AdminSuccess"] = succMsg;
            }
            catch (Exception ex)
            {
                var errMsg = "Firma türü değiştirme hatası: " + ex.Message;
                if (IsAjaxRequest()) return Json(new { success = false, message = errMsg });
                TempData["AdminError"] = errMsg;
            }

            return RedirectToAction(nameof(Index), new { tab = "companies" });
        }

        [HttpPost]
        [Route("DeleteCompany")]
        public async Task<IActionResult> DeleteCompany(int companyId)
        {
            if (!await CheckIsSuperAdminAsync())
            {
                if (IsAjaxRequest()) return Json(new { success = false, message = "Yetkisiz işlem." });
                return Forbid();
            }

            int currentCompId = GetCurrentCompanyId();
            if (companyId == currentCompId)
            {
                var selfMsg = "Kendi aktif oturum hesabınızı silemezsiniz!";
                if (IsAjaxRequest()) return Json(new { success = false, message = selfMsg });
                TempData["AdminError"] = selfMsg;
                return RedirectToAction(nameof(Index), new { tab = "companies" });
            }

            try
            {
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyID == companyId);
                if (company == null)
                {
                    var notFoundMsg = "Silinecek firma bulunamadı.";
                    if (IsAjaxRequest()) return Json(new { success = false, message = notFoundMsg });
                    TempData["AdminError"] = notFoundMsg;
                    return RedirectToAction(nameof(Index), new { tab = "companies" });
                }

                _context.Companies.Remove(company);
                await _context.SaveChangesAsync();

                var succMsg = $"'{company.CompanyName}' firması kalıcı olarak silindi.";
                if (IsAjaxRequest()) return Json(new { success = true, message = succMsg, companyId = companyId });
                TempData["AdminSuccess"] = succMsg;
            }
            catch (Exception ex)
            {
                var errMsg = "Firma silinirken hata oluştu: " + ex.Message;
                if (IsAjaxRequest()) return Json(new { success = false, message = errMsg });
                TempData["AdminError"] = errMsg;
            }

            return RedirectToAction(nameof(Index), new { tab = "companies" });
        }

        [HttpGet]
        [Route("GetCompany/{id}")]
        public async Task<IActionResult> GetCompany(int id)
        {
            if (!await CheckIsSuperAdminAsync()) return Forbid();

            var company = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == id);
            if (company == null) return NotFound(new { success = false, message = "Firma bulunamadı." });

            string addressText = "";
            if (company.AddressID.HasValue)
            {
                var addr = await _context.Addresses.AsNoTracking().FirstOrDefaultAsync(a => a.AddressID == company.AddressID.Value);
                addressText = addr?.AddressText ?? "";
            }

            return Json(new
            {
                success = true,
                companyId = company.CompanyID,
                companyName = company.CompanyName,
                companyOfficial = company.CompanyOfficial,
                companyMail = company.CompanyMail,
                companyPhone = company.CompanyPhone,
                tcno = company.TCNO,
                taxOffice = company.TaxOffice,
                companyTypeId = company.CompanyTypeID,
                membershipTypeId = company.MembershipTypeID,
                companyDetailText = company.CompanyDetailText,
                addressText = addressText
            });
        }

        // =========================================================================
        // SİSTEM ARAÇLARI & SİMÜLASYON ACTIONS
        // =========================================================================

        [HttpGet]
        [Route("GetRequestDetailsForSim")]
        public async Task<IActionResult> GetRequestDetailsForSim(int id)
        {
            if (!await CheckIsSuperAdminAsync())
            {
                return Json(new { success = false, message = "Yetkisiz işlem." });
            }

            try
            {
                var req = await _context.Requests
                    .Include(r => r.Items)
                    .Include(r => r.Offers)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.RequestID == id);

                if (req == null)
                {
                    return Json(new { success = false, message = "Talep bulunamadı." });
                }

                var itemsList = req.Items.OrderBy(i => i.ItemOrder).Select(i => new
                {
                    requestItemId = i.RequestItemID,
                    itemOrder = i.ItemOrder,
                    itemName = i.ItemName,
                    quantity = i.Quantity,
                    unit = i.Unit ?? "Adet",
                    estimatedUnitPrice = i.EstimatedUnitPrice,
                    estimatedUnitPriceFormatted = i.EstimatedUnitPrice.HasValue ? i.EstimatedUnitPrice.Value.ToString("N2") + " ₺" : null,
                    estimatedTotalPrice = i.EstimatedUnitPrice.HasValue ? (i.EstimatedUnitPrice.Value * i.Quantity) : (decimal?)null,
                    estimatedTotalPriceFormatted = i.EstimatedUnitPrice.HasValue ? (i.EstimatedUnitPrice.Value * i.Quantity).ToString("N2") + " ₺" : null,
                    itemDescription = i.ItemDescription ?? ""
                }).ToList();

                if (itemsList.Count == 0 && !string.IsNullOrWhiteSpace(req.ProductName))
                {
                    itemsList.Add(new
                    {
                        requestItemId = 0,
                        itemOrder = 1,
                        itemName = req.ProductName,
                        quantity = (decimal)(req.Quantity ?? 1),
                        unit = "Adet",
                        estimatedUnitPrice = req.EstimatedBudget.HasValue && req.Quantity > 0 ? (req.EstimatedBudget.Value / req.Quantity.Value) : (decimal?)null,
                        estimatedUnitPriceFormatted = req.EstimatedBudget.HasValue && req.Quantity > 0 ? (req.EstimatedBudget.Value / req.Quantity.Value).ToString("N2") + " ₺" : null,
                        estimatedTotalPrice = req.EstimatedBudget,
                        estimatedTotalPriceFormatted = req.EstimatedBudget.HasValue ? req.EstimatedBudget.Value.ToString("N2") + " ₺" : null,
                        itemDescription = req.Notes ?? ""
                    });
                }

                return Json(new
                {
                    success = true,
                    requestId = req.RequestID,
                    requestCode = $"REQ-{req.RequestID:D4}",
                    productName = req.ProductName ?? "Satın Alma Talebi",
                    buyerInfo = req.BuyerInfo ?? "Kurumsal Alıcı",
                    senderId = req.SenderID,
                    quantity = req.Quantity ?? 1,
                    estimatedBudget = req.EstimatedBudget,
                    estimatedBudgetFormatted = req.EstimatedBudget.HasValue ? req.EstimatedBudget.Value.ToString("N2") + " ₺" : "Belirtilmedi",
                    items = itemsList
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("SimulateCreateRequest")]
        public async Task<IActionResult> SimulateCreateRequest([FromBody] SimulateRequestInputModel input)
        {
            if (!await CheckIsSuperAdminAsync())
            {
                if (IsAjaxRequest()) return Json(new { success = false, message = "Yetkisiz işlem." });
                return Forbid();
            }

            if (input == null || input.SenderCompanyID <= 0)
            {
                var reqMsg = "Lütfen talep oluşturan firmayı seçiniz.";
                if (IsAjaxRequest()) return Json(new { success = false, message = reqMsg });
                TempData["AdminError"] = reqMsg;
                return RedirectToAction(nameof(Index), new { tab = "simulation" });
            }

            var validItems = input.Items != null 
                ? input.Items.Where(i => !string.IsNullOrWhiteSpace(i.ItemName) && i.Quantity > 0).ToList() 
                : new List<SimulateRequestItemDto>();

            if (validItems.Count == 0 && string.IsNullOrWhiteSpace(input.ProductName))
            {
                var reqMsg = "Lütfen bir talep başlığı veya en az 1 adet istenilen malzeme/hizmet kalemi ekleyiniz.";
                if (IsAjaxRequest()) return Json(new { success = false, message = reqMsg });
                TempData["AdminError"] = reqMsg;
                return RedirectToAction(nameof(Index), new { tab = "simulation" });
            }

            try
            {
                var senderComp = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyID == input.SenderCompanyID);
                if (senderComp == null)
                {
                    var notFoundMsg = "Seçilen firma bulunamadı.";
                    if (IsAjaxRequest()) return Json(new { success = false, message = notFoundMsg });
                    TempData["AdminError"] = notFoundMsg;
                    return RedirectToAction(nameof(Index), new { tab = "simulation" });
                }

                string buyerInfo = !string.IsNullOrWhiteSpace(senderComp.CompanyName) ? senderComp.CompanyName : (!string.IsNullOrWhiteSpace(senderComp.CompanyOfficial) ? senderComp.CompanyOfficial : "Kurumsal Alıcı");

                string finalTitle = !string.IsNullOrWhiteSpace(input.ProductName) 
                    ? input.ProductName.Trim() 
                    : (validItems.Count > 0 ? (validItems[0].ItemName + (validItems.Count > 1 ? $" ve {validItems.Count - 1} diğer kalem" : "")) : "Satın Alma Talebi");

                int totalQuantity = validItems.Count > 0 ? (int)Math.Round(validItems.Sum(i => i.Quantity)) : (input.Quantity > 0 ? input.Quantity : 1);

                decimal? totalEstimatedBudget = input.EstimatedBudget;
                if (!totalEstimatedBudget.HasValue || totalEstimatedBudget.Value <= 0)
                {
                    decimal calcBudget = validItems.Sum(i => (i.EstimatedUnitPrice ?? 0) * i.Quantity);
                    if (calcBudget > 0)
                    {
                        totalEstimatedBudget = calcBudget;
                    }
                }

                var newRequest = new Requests
                {
                    RequestDate = DateTime.UtcNow,
                    SenderID = senderComp.CompanyID,
                    BuyerInfo = buyerInfo,
                    ProductName = finalTitle,
                    Quantity = totalQuantity,
                    RequestType = string.IsNullOrWhiteSpace(input.RequestType) ? "Satın Alma" : input.RequestType.Trim(),
                    CategoryID = input.CategoryID,
                    EstimatedBudget = totalEstimatedBudget,
                    TargetDeliveryDate = input.TargetDeliveryDate,
                    Status = "Beklemede",
                    Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim()
                };

                if (validItems.Count > 0)
                {
                    int order = 1;
                    foreach (var item in validItems)
                    {
                        newRequest.Items.Add(new RequestItem
                        {
                            ItemOrder = order++,
                            ItemName = item.ItemName.Trim(),
                            Quantity = item.Quantity,
                            Unit = string.IsNullOrWhiteSpace(item.Unit) ? "Adet" : item.Unit.Trim(),
                            EstimatedUnitPrice = item.EstimatedUnitPrice,
                            ItemDescription = item.ItemDescription?.Trim(),
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                else
                {
                    newRequest.Items.Add(new RequestItem
                    {
                        ItemOrder = 1,
                        ItemName = finalTitle,
                        Quantity = totalQuantity,
                        Unit = "Adet",
                        EstimatedUnitPrice = totalEstimatedBudget.HasValue && totalQuantity > 0 ? totalEstimatedBudget.Value / totalQuantity : null,
                        ItemDescription = input.Notes,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                _context.Requests.Add(newRequest);
                await _context.SaveChangesAsync();

                try
                {
                    await _hubContext.Clients.All.SendAsync("OnRequestCreated", new
                    {
                        requestId = newRequest.RequestID,
                        requestCode = $"REQ-{newRequest.RequestID:D4}",
                        productName = newRequest.ProductName ?? "Satın Alma Talebi",
                        category = newRequest.RequestType ?? "Satın Alma",
                        quantity = totalQuantity,
                        itemCount = newRequest.Items.Count,
                        estimatedBudget = newRequest.EstimatedBudget,
                        estimatedBudgetFormatted = newRequest.EstimatedBudget.HasValue ? newRequest.EstimatedBudget.Value.ToString("N2") + " ₺" : "Belirtilmedi",
                        senderId = newRequest.SenderID,
                        buyerInfo = newRequest.BuyerInfo ?? "Kurumsal Alıcı",
                        status = newRequest.Status ?? "Beklemede",
                        requestDate = newRequest.RequestDate?.ToString("dd.MM.yyyy HH:mm") ?? DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                        targetDeliveryDate = newRequest.TargetDeliveryDate?.ToString("dd.MM.yyyy") ?? "Belirtilmedi",
                        notes = newRequest.Notes ?? "",
                        offerCount = 0
                    });
                }
                catch (Exception hubEx)
                {
                    Console.WriteLine($"SignalR SimulateCreateRequest error: {hubEx.Message}");
                }

                var succMsg = $"'{senderComp.CompanyName}' adına '#REQ-{newRequest.RequestID} - {newRequest.ProductName}' talebi ({newRequest.Items.Count} Kalem) başarıyla oluşturuldu ve yayınlandı!";
                if (IsAjaxRequest()) return Json(new { success = true, message = succMsg, requestId = newRequest.RequestID, productName = newRequest.ProductName, itemCount = newRequest.Items.Count });
                TempData["AdminSuccess"] = succMsg;
            }
            catch (Exception ex)
            {
                var errMsg = "Talep simülasyonu hatası: " + ex.Message;
                if (IsAjaxRequest()) return Json(new { success = false, message = errMsg });
                TempData["AdminError"] = errMsg;
            }

            return RedirectToAction(nameof(Index), new { tab = "simulation" });
        }

        [HttpPost]
        [Route("SimulateCreateOffer")]
        public async Task<IActionResult> SimulateCreateOffer([FromBody] SimulateOfferInputModel input)
        {
            if (!await CheckIsSuperAdminAsync())
            {
                if (IsAjaxRequest()) return Json(new { success = false, message = "Yetkisiz işlem." });
                return Forbid();
            }

            if (input == null || input.RequestID <= 0 || input.SupplierCompanyID <= 0)
            {
                var reqMsg = "Lütfen teklif verilecek talebi ve tedarikçi firmayı seçiniz.";
                if (IsAjaxRequest()) return Json(new { success = false, message = reqMsg });
                TempData["AdminError"] = reqMsg;
                return RedirectToAction(nameof(Index), new { tab = "simulation" });
            }

            try
            {
                var request = await _context.Requests
                    .Include(r => r.Offers)
                    .Include(r => r.Items)
                    .FirstOrDefaultAsync(r => r.RequestID == input.RequestID);

                if (request == null)
                {
                    var notFoundMsg = "Teklif verilmek istenen talep bulunamadı.";
                    if (IsAjaxRequest()) return Json(new { success = false, message = notFoundMsg });
                    TempData["AdminError"] = notFoundMsg;
                    return RedirectToAction(nameof(Index), new { tab = "simulation" });
                }

                var supplier = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyID == input.SupplierCompanyID);
                if (supplier == null)
                {
                    var notFoundMsg = "Seçilen tedarikçi firma bulunamadı.";
                    if (IsAjaxRequest()) return Json(new { success = false, message = notFoundMsg });
                    TempData["AdminError"] = notFoundMsg;
                    return RedirectToAction(nameof(Index), new { tab = "simulation" });
                }

                // Kendi talebine teklif vermesini engelle
                if (request.SenderID.HasValue && request.SenderID.Value == supplier.CompanyID)
                {
                    var selfOfferMsg = "Bir firma kendi oluşturduğu talebe teklif veremez!";
                    if (IsAjaxRequest()) return Json(new { success = false, message = selfOfferMsg });
                    TempData["AdminError"] = selfOfferMsg;
                    return RedirectToAction(nameof(Index), new { tab = "simulation" });
                }

                var reqItems = request.Items != null ? request.Items.OrderBy(i => i.ItemOrder).ToList() : new List<RequestItem>();

                // Kalem bazlı teklif tutarı hesaplama
                decimal calculatedTotalPrice = 0;
                var validOfferItems = input.Items != null ? input.Items.Where(it => it.UnitPrice >= 0).ToList() : new List<SimulateOfferItemDto>();

                if (validOfferItems.Count > 0)
                {
                    foreach (var it in validOfferItems)
                    {
                        var reqItem = reqItems.FirstOrDefault(ri => ri.RequestItemID == it.RequestItemID);
                        decimal qty = reqItem?.Quantity ?? 1;
                        calculatedTotalPrice += (it.UnitPrice * qty);
                    }
                }
                else if (input.OfferedPrice > 0)
                {
                    calculatedTotalPrice = input.OfferedPrice;
                }
                else
                {
                    var noPriceMsg = "Lütfen en az bir kalem için birim fiyat veya geçerli bir toplam teklif tutarı giriniz.";
                    if (IsAjaxRequest()) return Json(new { success = false, message = noPriceMsg });
                    TempData["AdminError"] = noPriceMsg;
                    return RedirectToAction(nameof(Index), new { tab = "simulation" });
                }

                var existingOffer = await _context.RequestOffers
                    .Include(o => o.OfferItems)
                    .FirstOrDefaultAsync(o => o.RequestID == input.RequestID && o.SupplierID == supplier.CompanyID);

                string succMsg;
                bool wasRejected = false;
                decimal? prevPrice = null;

                if (existingOffer != null)
                {
                    var (cleanNotes, prevReason, isRev, prevP, prevRReason) = ParseNotesForSimulation(existingOffer.SupplierNotes);
                    wasRejected = string.Equals(existingOffer.Status, "Reddedildi", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(prevReason) || isRev;
                    prevPrice = existingOffer.OfferedPrice;
                    string prevRejText = prevReason ?? prevRReason ?? "Alıcı tarafından reddedildi";

                    existingOffer.OfferedPrice = calculatedTotalPrice;
                    existingOffer.DeliveryDays = input.DeliveryDays ?? 7;
                    existingOffer.ValidityDays = input.ValidityDays ?? 30;
                    existingOffer.OfferDate = DateTime.UtcNow;
                    existingOffer.Status = "Beklemede";

                    if (wasRejected)
                    {
                        string revisionTag = $"[Revize:EskiFiyat={prevPrice.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}|EskiRed={prevRejText.Replace("|", " ").Replace("]", "")}]";
                        string cleanUserNote = input.SupplierNotes?.Trim() ?? "";
                        existingOffer.SupplierNotes = string.IsNullOrWhiteSpace(cleanUserNote) ? revisionTag : $"{cleanUserNote} {revisionTag}";
                    }
                    else
                    {
                        existingOffer.SupplierNotes = string.IsNullOrWhiteSpace(input.SupplierNotes) ? null : input.SupplierNotes.Trim();
                    }

                    // Kalem bazlı teklifleri yenile
                    if (existingOffer.OfferItems != null && existingOffer.OfferItems.Any())
                    {
                        _context.RequestOfferItems.RemoveRange(existingOffer.OfferItems);
                    }

                    existingOffer.OfferItems ??= new List<RequestOfferItem>();

                    if (validOfferItems.Count > 0)
                    {
                        foreach (var it in validOfferItems)
                        {
                            var reqItem = reqItems.FirstOrDefault(ri => ri.RequestItemID == it.RequestItemID);
                            decimal qty = reqItem?.Quantity ?? 1;
                            existingOffer.OfferItems.Add(new RequestOfferItem
                            {
                                OfferID = existingOffer.OfferID,
                                RequestItemID = it.RequestItemID,
                                UnitPrice = it.UnitPrice,
                                LineTotal = it.UnitPrice * qty,
                                Notes = it.Notes?.Trim()
                            });
                        }
                    }
                    else if (reqItems.Count > 0)
                    {
                        decimal totalQty = reqItems.Sum(ri => ri.Quantity);
                        foreach (var ri in reqItems)
                        {
                            decimal proportion = totalQty > 0 ? (ri.Quantity / totalQty) : (1m / reqItems.Count);
                            decimal lineTot = Math.Round(calculatedTotalPrice * proportion, 2);
                            decimal uPrice = ri.Quantity > 0 ? Math.Round(lineTot / ri.Quantity, 2) : lineTot;
                            existingOffer.OfferItems.Add(new RequestOfferItem
                            {
                                OfferID = existingOffer.OfferID,
                                RequestItemID = ri.RequestItemID,
                                UnitPrice = uPrice,
                                LineTotal = lineTot,
                                Notes = null
                            });
                        }
                    }

                    succMsg = wasRejected
                        ? $"'{supplier.CompanyName}' adına '#REQ-{request.RequestID}' talebine verilen reddedilmiş teklif {calculatedTotalPrice:N2} ₺ olarak revize edildi!"
                        : $"'{supplier.CompanyName}' adına '#REQ-{request.RequestID}' talebine verilen teklif {calculatedTotalPrice:N2} ₺ olarak güncellendi!";
                }
                else
                {
                    var newOffer = new RequestOffers
                    {
                        RequestID = input.RequestID,
                        SupplierID = supplier.CompanyID,
                        OfferedPrice = calculatedTotalPrice,
                        DeliveryDays = input.DeliveryDays ?? 7,
                        ValidityDays = input.ValidityDays ?? 30,
                        SupplierNotes = string.IsNullOrWhiteSpace(input.SupplierNotes) ? null : input.SupplierNotes.Trim(),
                        OfferDate = DateTime.UtcNow,
                        Status = "Beklemede"
                    };

                    _context.RequestOffers.Add(newOffer);
                    await _context.SaveChangesAsync(); // OfferID üretilmesi için

                    if (validOfferItems.Count > 0)
                    {
                        foreach (var it in validOfferItems)
                        {
                            var reqItem = reqItems.FirstOrDefault(ri => ri.RequestItemID == it.RequestItemID);
                            decimal qty = reqItem?.Quantity ?? 1;
                            _context.RequestOfferItems.Add(new RequestOfferItem
                            {
                                OfferID = newOffer.OfferID,
                                RequestItemID = it.RequestItemID,
                                UnitPrice = it.UnitPrice,
                                LineTotal = it.UnitPrice * qty,
                                Notes = it.Notes?.Trim()
                            });
                        }
                        await _context.SaveChangesAsync();
                    }
                    else if (reqItems.Count > 0)
                    {
                        decimal totalQty = reqItems.Sum(ri => ri.Quantity);
                        foreach (var ri in reqItems)
                        {
                            decimal proportion = totalQty > 0 ? (ri.Quantity / totalQty) : (1m / reqItems.Count);
                            decimal lineTot = Math.Round(calculatedTotalPrice * proportion, 2);
                            decimal uPrice = ri.Quantity > 0 ? Math.Round(lineTot / ri.Quantity, 2) : lineTot;
                            _context.RequestOfferItems.Add(new RequestOfferItem
                            {
                                OfferID = newOffer.OfferID,
                                RequestItemID = ri.RequestItemID,
                                UnitPrice = uPrice,
                                LineTotal = lineTot,
                                Notes = null
                            });
                        }
                        await _context.SaveChangesAsync();
                    }

                    succMsg = $"'{supplier.CompanyName}' adına '#REQ-{request.RequestID} - {request.ProductName}' talebine {calculatedTotalPrice:N2} ₺ tutarında teklif başarıyla iletildi!";
                }

                if (request.Status == "Beklemede")
                {
                    request.Status = "Teklif Alındı";
                }

                await _context.SaveChangesAsync();

                try
                {
                    var offerToSend = existingOffer ?? await _context.RequestOffers
                        .Include(o => o.OfferItems)
                        .FirstOrDefaultAsync(o => o.RequestID == input.RequestID && o.SupplierID == supplier.CompanyID);

                    if (offerToSend != null)
                    {
                        var supplierName = supplier.CompanyName ?? supplier.CompanyOfficial ?? $"Tedarikçi #{supplier.CompanyID}";
                        var payload = new
                        {
                            requestId = request.RequestID,
                            requestCode = $"REQ-{request.RequestID:D4}",
                            productName = request.ProductName ?? "Satın Alma Talebi",
                            offerId = offerToSend.OfferID,
                            supplierId = offerToSend.SupplierID,
                            supplierName = supplierName,
                            supplierOfficial = supplier.CompanyOfficial ?? "",
                            supplierPhone = supplier.CompanyPhone ?? "",
                            supplierMail = supplier.CompanyMail ?? "",
                            offeredPrice = offerToSend.OfferedPrice,
                            offeredPriceFormatted = offerToSend.OfferedPrice.ToString("N2") + " ₺",
                            deliveryDays = offerToSend.DeliveryDays,
                            validityDays = offerToSend.ValidityDays,
                            offerDate = offerToSend.OfferDate.ToString("dd.MM.yyyy HH:mm"),
                            status = offerToSend.Status ?? "Beklemede",
                            supplierNotes = offerToSend.SupplierNotes ?? "",
                            isRevised = (existingOffer != null),
                            totalOffersCount = request.Offers.Count,
                            bestOfferPrice = request.Offers.Any() ? request.Offers.Min(o => o.OfferedPrice) : offerToSend.OfferedPrice
                        };

                        await _hubContext.Clients.Group($"request_{request.RequestID}").SendAsync("OnOfferSubmitted", payload);

                        if (request.SenderID.HasValue && request.SenderID.Value > 0)
                        {
                            await _hubContext.Clients.Group($"company_{request.SenderID.Value}").SendAsync("OnOfferReceivedNotification", new
                            {
                                requestId = request.RequestID,
                                productName = request.ProductName,
                                supplierName = supplierName,
                                offeredPrice = offerToSend.OfferedPrice,
                                offeredPriceFormatted = offerToSend.OfferedPrice.ToString("N2") + " ₺",
                                isRevised = (existingOffer != null)
                            });
                        }

                        await _hubContext.Clients.All.SendAsync("OnRequestOfferCountUpdated", new
                        {
                            requestId = request.RequestID,
                            offerCount = request.Offers.Count,
                            bestOfferPrice = request.Offers.Min(o => o.OfferedPrice),
                            bestOfferPriceFormatted = request.Offers.Min(o => o.OfferedPrice).ToString("N2") + " ₺",
                            status = request.Status
                        });
                    }
                }
                catch (Exception hubEx)
                {
                    Console.WriteLine($"SignalR SimulateCreateOffer error: {hubEx.Message}");
                }

                // E-posta Bildirimi Gönderimi (Talep sahibine kalem dökümüyle)
                if (request.SenderID.HasValue)
                {
                    var ownerCompany = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == request.SenderID.Value);
                    if (ownerCompany != null && !string.IsNullOrWhiteSpace(ownerCompany.CompanyMail))
                    {
                        string ownerEmail = ownerCompany.CompanyMail;
                        string ownerName = ownerCompany.CompanyOfficial ?? ownerCompany.CompanyName ?? "Sayın Alıcı";
                        string supplierName = supplier.CompanyName ?? supplier.CompanyOfficial ?? "Tedarikçi";

                        var emailItems = new List<OfferItemEmailDto>();
                        if (reqItems.Any())
                        {
                            int order = 1;
                            foreach (var ri in reqItems)
                            {
                                var matchingOfferItem = validOfferItems.FirstOrDefault(it => it.RequestItemID == ri.RequestItemID);
                                decimal uPrice = matchingOfferItem != null && matchingOfferItem.UnitPrice >= 0 
                                    ? matchingOfferItem.UnitPrice 
                                    : (ri.Quantity > 0 ? Math.Round(calculatedTotalPrice / reqItems.Count / ri.Quantity, 2) : 0);
                                decimal lineTot = uPrice * ri.Quantity;

                                emailItems.Add(new OfferItemEmailDto
                                {
                                    ItemOrder = order++,
                                    ItemName = ri.ItemName,
                                    Quantity = ri.Quantity,
                                    Unit = ri.Unit ?? "Adet",
                                    UnitPrice = uPrice,
                                    LineTotal = lineTot,
                                    ItemDescription = ri.ItemDescription,
                                    Notes = matchingOfferItem?.Notes
                                });
                            }
                        }

                        string requestsUrl = $"{Request.Scheme}://{Request.Host}/Requests?tab=incoming-offers&requestId={request.RequestID}";

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _emailService.SendOfferReceivedEmailAsync(
                                    toEmail: ownerEmail,
                                    buyerName: ownerName,
                                    supplierName: supplierName,
                                    rfqCode: $"REQ-{request.RequestID:D4}",
                                    productName: request.ProductName ?? "Talep Edilen Ürün",
                                    productImageUrl: null,
                                    quantity: request.Quantity ?? 1,
                                    unit: "Adet",
                                    offerPrice: calculatedTotalPrice,
                                    deliveryDays: input.DeliveryDays ?? 7,
                                    validityDays: input.ValidityDays ?? 30,
                                    offerNotes: input.SupplierNotes,
                                    requestsUrl: requestsUrl,
                                    items: emailItems.Any() ? emailItems : null);
                            }
                            catch (Exception emailEx)
                            {
                                Console.WriteLine($"Simulated offer email note: {emailEx.Message}");
                            }
                        });
                    }
                }

                if (IsAjaxRequest()) return Json(new { success = true, message = succMsg, requestId = request.RequestID, offeredPrice = calculatedTotalPrice });
                TempData["AdminSuccess"] = succMsg;
            }
            catch (Exception ex)
            {
                var errMsg = "Teklif simülasyonu hatası: " + ex.Message;
                if (IsAjaxRequest()) return Json(new { success = false, message = errMsg });
                TempData["AdminError"] = errMsg;
            }

            return RedirectToAction(nameof(Index), new { tab = "simulation" });
        }

        private (string? CleanNotes, string? RejectionReason, bool IsRevised, decimal? PreviousPrice, string? PreviousRejectionReason) ParseNotesForSimulation(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
                return (null, null, false, null, null);

            string clean = notes;
            string? reason = null;
            bool isRevised = false;
            decimal? prevPrice = null;
            string? prevRej = null;

            if (clean.Contains("[Red Gerekçesi:"))
            {
                int start = clean.IndexOf("[Red Gerekçesi:");
                int end = clean.IndexOf("]", start);
                if (end > start)
                {
                    reason = clean.Substring(start + 15, end - (start + 15)).Trim();
                    clean = clean.Remove(start, end - start + 1).Trim();
                }
            }

            if (clean.Contains("[Revize:"))
            {
                int start = clean.IndexOf("[Revize:");
                int end = clean.IndexOf("]", start);
                if (end > start)
                {
                    isRevised = true;
                    string tagContent = clean.Substring(start + 8, end - (start + 8));
                    var parts = tagContent.Split('|');
                    foreach (var p in parts)
                    {
                        if (p.StartsWith("EskiFiyat="))
                        {
                            var valStr = p.Substring("EskiFiyat=".Length);
                            if (decimal.TryParse(valStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedVal))
                            {
                                prevPrice = parsedVal;
                            }
                        }
                        else if (p.StartsWith("EskiRed="))
                        {
                            prevRej = p.Substring("EskiRed=".Length).Trim();
                        }
                    }
                    clean = clean.Remove(start, end - start + 1).Trim();
                }
            }

            clean = clean.TrimEnd('|', ' ', '-');
            return (string.IsNullOrWhiteSpace(clean) ? null : clean, reason, isRevised, prevPrice, prevRej);
        }

        [HttpPost]
        [Route("SimulateCreateListing")]
        public async Task<IActionResult> SimulateCreateListing(SimulateListingInputModel input)
        {
            if (!await CheckIsSuperAdminAsync())
            {
                if (IsAjaxRequest()) return Json(new { success = false, message = "Yetkisiz işlem." });
                return Forbid();
            }

            if (!ModelState.IsValid || input.SupplierCompanyID <= 0 || string.IsNullOrWhiteSpace(input.ItemName) || input.ItemPrice <= 0 || input.Quantity <= 0)
            {
                var reqMsg = "Lütfen tedarikçi firma, ürün adı, geçerli birim fiyat ve stok adedi giriniz.";
                if (IsAjaxRequest()) return Json(new { success = false, message = reqMsg });
                TempData["AdminError"] = reqMsg;
                return RedirectToAction(nameof(Index), new { tab = "simulation" });
            }

            try
            {
                var supplier = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyID == input.SupplierCompanyID);
                if (supplier == null)
                {
                    var notFoundMsg = "Seçilen tedarikçi firma bulunamadı.";
                    if (IsAjaxRequest()) return Json(new { success = false, message = notFoundMsg });
                    TempData["AdminError"] = notFoundMsg;
                    return RedirectToAction(nameof(Index), new { tab = "simulation" });
                }

                string finalBrand = !string.IsNullOrWhiteSpace(input.Brand) 
                    ? input.Brand.Trim() 
                    : (!string.IsNullOrWhiteSpace(supplier.CompanyName) ? supplier.CompanyName.Trim() : (supplier.CompanyOfficial ?? "Onaylı Tedarikçi"));

                string finalCatName = "Endüstriyel Ürünler";
                int? finalCatId = input.CategoryID;
                if (finalCatId.HasValue && finalCatId.Value > 0)
                {
                    var cat = await _context.Category.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryID == finalCatId.Value);
                    if (cat != null) finalCatName = cat.CategoryName ?? finalCatName;
                }

                string finalProps = !string.IsNullOrWhiteSpace(input.Properties)
                    ? input.Properties.Trim()
                    : $"Kategori: {finalCatName}";

                var newItem = new ItemsModel
                {
                    ItemName = input.ItemName.Trim(),
                    Brand = finalBrand,
                    CategoryID = finalCatId,
                    ItemPrice = (double)input.ItemPrice,
                    Quantity = input.Quantity,
                    Properties = finalProps,
                    ItemDescryption = string.IsNullOrWhiteSpace(input.ItemDescryption) ? null : input.ItemDescryption.Trim()
                };

                _context.Items.Add(newItem);
                await _context.SaveChangesAsync();

                var succMsg = $"'{supplier.CompanyName}' adına '{newItem.ItemName}' (Birim Fiyat: {newItem.ItemPrice:N0} ₺ - Stok: {newItem.Quantity} Adet) ürün ilanı başarıyla vitrine eklendi ve yayına alındı! (Ürün ID: #{newItem.ItemID})";
                if (IsAjaxRequest()) return Json(new { success = true, message = succMsg, itemId = newItem.ItemID, itemName = newItem.ItemName, price = newItem.ItemPrice, quantity = newItem.Quantity });
                TempData["AdminSuccess"] = succMsg;
            }
            catch (Exception ex)
            {
                var errMsg = "Satış ilanı simülasyonu hatası: " + ex.Message;
                if (IsAjaxRequest()) return Json(new { success = false, message = errMsg });
                TempData["AdminError"] = errMsg;
            }

            return RedirectToAction(nameof(Index), new { tab = "simulation" });
        }

        [HttpPost]
        [Route("DeleteListing")]
        public async Task<IActionResult> DeleteListing(int id)
        {
            if (!await CheckIsSuperAdminAsync())
            {
                if (IsAjaxRequest()) return Json(new { success = false, message = "Yetkisiz işlem." });
                return Forbid();
            }

            if (id <= 0)
            {
                var reqMsg = "Geçersiz ürün seçildi.";
                if (IsAjaxRequest()) return Json(new { success = false, message = reqMsg });
                TempData["AdminError"] = reqMsg;
                return RedirectToAction(nameof(Index), new { tab = "simulation" });
            }

            try
            {
                var item = await _context.Items.FirstOrDefaultAsync(i => i.ItemID == id);
                if (item == null)
                {
                    var notFoundMsg = "Silinecek ürün ilanı bulunamadı.";
                    if (IsAjaxRequest()) return Json(new { success = false, message = notFoundMsg });
                    TempData["AdminError"] = notFoundMsg;
                    return RedirectToAction(nameof(Index), new { tab = "simulation" });
                }

                _context.Items.Remove(item);
                await _context.SaveChangesAsync();

                var succMsg = $"'{item.ItemName}' ürün ilanı vitrinden başarıyla kaldırıldı.";
                if (IsAjaxRequest()) return Json(new { success = true, message = succMsg, itemId = id });
                TempData["AdminSuccess"] = succMsg;
            }
            catch (Exception ex)
            {
                var errMsg = "İlan silinirken hata oluştu: " + ex.Message;
                if (IsAjaxRequest()) return Json(new { success = false, message = errMsg });
                TempData["AdminError"] = errMsg;
            }

            return RedirectToAction(nameof(Index), new { tab = "simulation" });
        }

        [HttpPost]
        [Route("SeedSystemDefaults")]
        public async Task<IActionResult> SeedSystemDefaults()
        {
            if (!await CheckIsSuperAdminAsync())
            {
                if (IsAjaxRequest()) return Json(new { success = false, message = "Yetkisiz işlem." });
                return Forbid();
            }

            try
            {
                await DbInitializer.SeedDataAsync(_context);
                var succMsg = "Sistem varsayılan verileri ve tür tabloları başarıyla senkronize edildi!";
                if (IsAjaxRequest()) return Json(new { success = true, message = succMsg });
                TempData["AdminSuccess"] = succMsg;
            }
            catch (Exception ex)
            {
                var errMsg = "Senkronizasyon hatası: " + ex.Message;
                if (IsAjaxRequest()) return Json(new { success = false, message = errMsg });
                TempData["AdminError"] = errMsg;
            }

            return RedirectToAction(nameof(Index), new { tab = "settings" });
        }

        // =========================================================================
        // 10★ DEĞERLENDİRME & KARNE TEST SİMÜLASYONU ACTIONS
        // =========================================================================

        [HttpPost]
        [Route("SimulateReview")]
        public async Task<IActionResult> SimulateReview(SimulateReviewInputModel input)
        {
            if (!await CheckIsSuperAdminAsync())
            {
                if (IsAjaxRequest()) return Json(new { success = false, message = "Yetkisiz işlem." });
                return Forbid();
            }

            if (!ModelState.IsValid || input.TargetCompanyID <= 0 || input.ReviewScore < 1.0 || input.ReviewScore > 10.0)
            {
                var reqMsg = "Lütfen değerlendirilecek hedef firmayı ve 1-10 arasında geçerli bir puan seçiniz.";
                if (IsAjaxRequest()) return Json(new { success = false, message = reqMsg });
                TempData["AdminError"] = reqMsg;
                return RedirectToAction(nameof(Index), new { tab = "reviews" });
            }

            try
            {
                var targetComp = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyID == input.TargetCompanyID);
                if (targetComp == null)
                {
                    var notFoundMsg = "Değerlendirilecek hedef firma sistemde bulunamadı.";
                    if (IsAjaxRequest()) return Json(new { success = false, message = notFoundMsg });
                    TempData["AdminError"] = notFoundMsg;
                    return RedirectToAction(nameof(Index), new { tab = "reviews" });
                }

                string reviewerName = input.ReviewerName?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(reviewerName) && input.ReviewerCompanyID.HasValue && input.ReviewerCompanyID.Value > 0)
                {
                    var revComp = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == input.ReviewerCompanyID.Value);
                    if (revComp != null) reviewerName = revComp.CompanyName;
                }
                if (string.IsNullOrWhiteSpace(reviewerName))
                {
                    reviewerName = input.ReviewType == "BuyerToSeller" ? "Doğrulanmış Kurumsal Alıcı (Test)" : "Onaylı Tedarikçi Firma (Test)";
                }

                var newReview = new Reviews
                {
                    CompanyID = input.TargetCompanyID,
                    ReviewerCompanyID = input.ReviewerCompanyID,
                    ReviewerName = reviewerName,
                    ReviewType = input.ReviewType,
                    OrderID = input.OrderID,
                    ReviewScore = input.ReviewScore,
                    DeliveryScore = input.PaymentPunctualityScore ?? input.DeliveryScore ?? input.ReviewScore,
                    QualityScore = input.QualityScore ?? input.ReviewScore,
                    CommunicationScore = input.CommunicationScore ?? input.ReviewScore,
                    ReviewText = input.ReviewText?.Trim(),
                    ReviewDate = DateTime.UtcNow,
                    IsVerifiedPurchase = input.IsVerifiedPurchase
                };

                _context.Reviews.Add(newReview);
                await _context.SaveChangesAsync();

                var succMsg = $"'{targetComp.CompanyName}' firması için {input.ReviewScore:0.0} puanındaki test değerlendirmesi başarıyla kaydedildi! (ID: #{newReview.ReviewID})";
                if (IsAjaxRequest()) return Json(new { success = true, message = succMsg, reviewId = newReview.ReviewID });
                TempData["AdminSuccess"] = succMsg;
            }
            catch (Exception ex)
            {
                var errMsg = "Değerlendirme simülasyonu hatası: " + ex.Message;
                if (IsAjaxRequest()) return Json(new { success = false, message = errMsg });
                TempData["AdminError"] = errMsg;
            }

            return RedirectToAction(nameof(Index), new { tab = "reviews" });
        }

        [HttpPost]
        [Route("DeleteReview")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            if (!await CheckIsSuperAdminAsync())
            {
                if (IsAjaxRequest()) return Json(new { success = false, message = "Yetkisiz işlem." });
                return Forbid();
            }

            if (id <= 0)
            {
                var reqMsg = "Geçersiz değerlendirme seçildi.";
                if (IsAjaxRequest()) return Json(new { success = false, message = reqMsg });
                TempData["AdminError"] = reqMsg;
                return RedirectToAction(nameof(Index), new { tab = "reviews" });
            }

            try
            {
                var review = await _context.Reviews.FirstOrDefaultAsync(r => r.ReviewID == id);
                if (review == null)
                {
                    var notFoundMsg = "Silinecek değerlendirme bulunamadı.";
                    if (IsAjaxRequest()) return Json(new { success = false, message = notFoundMsg });
                    TempData["AdminError"] = notFoundMsg;
                    return RedirectToAction(nameof(Index), new { tab = "reviews" });
                }

                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();

                var succMsg = $"#{id} numaralı değerlendirme başarıyla silindi.";
                if (IsAjaxRequest()) return Json(new { success = true, message = succMsg, reviewId = id });
                TempData["AdminSuccess"] = succMsg;
            }
            catch (Exception ex)
            {
                var errMsg = "Değerlendirme silinirken hata oluştu: " + ex.Message;
                if (IsAjaxRequest()) return Json(new { success = false, message = errMsg });
                TempData["AdminError"] = errMsg;
            }

            return RedirectToAction(nameof(Index), new { tab = "reviews" });
        }

        [HttpPost]
        [Route("GenerateDemoReviews")]
        public async Task<IActionResult> GenerateDemoReviews()
        {
            if (!await CheckIsSuperAdminAsync())
            {
                if (IsAjaxRequest()) return Json(new { success = false, message = "Yetkisiz işlem." });
                return Forbid();
            }

            try
            {
                var corporateCompanies = await _context.Companies
                    .Where(c => c.CompanyTypeID != 4 && !string.Equals(c.CompanyMail, "admin@tedarikportal.com", StringComparison.OrdinalIgnoreCase))
                    .ToListAsync();

                if (!corporateCompanies.Any())
                {
                    var noCompMsg = "Sistemde değerlendirme yapılacak kurumsal firma bulunamadı.";
                    if (IsAjaxRequest()) return Json(new { success = false, message = noCompMsg });
                    TempData["AdminError"] = noCompMsg;
                    return RedirectToAction(nameof(Index), new { tab = "reviews" });
                }

                var sampleBuyerComments = new[]
                {
                    "Siparişimiz eksiksiz ve taahhüt edilen süreden daha hızlı ulaştı. Ürün ambalajı ve kalitesi mükemmel.",
                    "Kurumsal satın alma sürecimizde gösterilen ilgi ve teknik destek için teşekkür ederiz. 10/10 hizmet.",
                    "Fiyat/performans açısından sektördeki en güvenilir tedarikçilerden biri. Sevkiyat çok hızlıydı.",
                    "Malzeme kalitesi tam şartnamemize uygun. Faturalandırma ve sevkiyat süreçleri çok profesyonelce yürütüldü.",
                    "Ürünler sağlam ulaştı, teknik dokümanlar eksiksiz iletildi. Tekrar çalışacağız."
                };

                var sampleSellerComments = new[]
                {
                    "Ödeme tam vadesinde ve eksiksiz gerçekleşti. Mal kabul süreci son derece hızlı ve profesyoneldi.",
                    "Kurumsal iletişim son derece net ve yapıcı. Güvenilir ve saygın bir iş ortağı.",
                    "Teslimat onayını ve evrak sürecini aynı gün tamamladılar. Çalışmaktan memnuniyet duyduğumuz bir alıcı.",
                    "Sipariş şartlarına ve anlaşmaya tam uyum sağlandı. Teşekkür ederiz."
                };

                int createdCount = 0;
                var rand = new Random();

                foreach (var target in corporateCompanies)
                {
                    // Satıcı Değerlendirmesi (BuyerToSeller)
                    var otherComps = corporateCompanies.Where(c => c.CompanyID != target.CompanyID).ToList();
                    var reviewer = otherComps.Any() ? otherComps[rand.Next(otherComps.Count)] : null;

                    var bReview = new Reviews
                    {
                        CompanyID = target.CompanyID,
                        ReviewerCompanyID = reviewer?.CompanyID,
                        ReviewerName = reviewer?.CompanyName ?? "Atlas Endüstri A.Ş. (Onaylı Alıcı)",
                        ReviewType = "BuyerToSeller",
                        ReviewScore = Math.Round(9.0 + rand.NextDouble(), 1),
                        DeliveryScore = 10.0,
                        QualityScore = 9.8,
                        CommunicationScore = 10.0,
                        ReviewText = sampleBuyerComments[rand.Next(sampleBuyerComments.Length)],
                        ReviewDate = DateTime.UtcNow.AddDays(-rand.Next(1, 15)),
                        IsVerifiedPurchase = true
                    };
                    _context.Reviews.Add(bReview);
                    createdCount++;

                    // Alıcı Değerlendirmesi (SellerToBuyer)
                    var sReview = new Reviews
                    {
                        CompanyID = target.CompanyID,
                        ReviewerCompanyID = reviewer?.CompanyID,
                        ReviewerName = reviewer?.CompanyName ?? "Mega Lojistik ve Ticaret A.Ş. (Onaylı Tedarikçi)",
                        ReviewType = "SellerToBuyer",
                        ReviewScore = Math.Round(9.2 + (rand.NextDouble() * 0.8), 1),
                        DeliveryScore = 10.0,
                        QualityScore = 10.0,
                        CommunicationScore = 9.5,
                        ReviewText = sampleSellerComments[rand.Next(sampleSellerComments.Length)],
                        ReviewDate = DateTime.UtcNow.AddDays(-rand.Next(1, 15)),
                        IsVerifiedPurchase = true
                    };
                    _context.Reviews.Add(sReview);
                    createdCount++;
                }

                await _context.SaveChangesAsync();

                var succMsg = $"Sistemdeki {corporateCompanies.Count} firma için toplam {createdCount} adet gerçekçi demo değerlendirme üretildi!";
                if (IsAjaxRequest()) return Json(new { success = true, message = succMsg, count = createdCount });
                TempData["AdminSuccess"] = succMsg;
            }
            catch (Exception ex)
            {
                var errMsg = "Demo değerlendirmeler üretilirken hata oluştu: " + ex.Message;
                if (IsAjaxRequest()) return Json(new { success = false, message = errMsg });
                TempData["AdminError"] = errMsg;
            }

            return RedirectToAction(nameof(Index), new { tab = "reviews" });
        }

        // =========================================================================
        // SİPARİŞ & ÇOKLU TEDARİKÇİ SEVKİYAT YÖNETİMİ ACTIONS
        // =========================================================================

        [HttpGet]
        [Route("GetOrderDetails/{id}")]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            if (!await CheckIsSuperAdminAsync()) return Forbid();

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderID == id);

            if (order == null) return NotFound(new { success = false, message = "Sipariş bulunamadı." });

            var distinctSuppliers = order.OrderItems
                .Select(i => i.SupplierCompanyID)
                .Where(sid => sid.HasValue && sid.Value > 0)
                .Distinct()
                .Count();
            if (distinctSuppliers == 0 && order.SupplierCompanyID.HasValue) distinctSuppliers = 1;

            return Json(new
            {
                success = true,
                orderId = order.OrderID,
                orderNumber = order.OrderNumber,
                buyerCompanyId = order.BuyerCompanyID,
                buyerCompanyName = !string.IsNullOrWhiteSpace(order.BuyerCompanyName) ? order.BuyerCompanyName : $"Firma #{order.BuyerCompanyID}",
                buyerOfficial = order.ReceiverName ?? "-",
                buyerEmail = order.BuyerEmail ?? "-",
                buyerPhone = order.BuyerPhone ?? order.ReceiverPhone ?? "-",
                supplierCompanyId = order.SupplierCompanyID,
                supplierCompanyName = !string.IsNullOrWhiteSpace(order.SupplierCompanyName) ? order.SupplierCompanyName : "Çoklu Tedarikçi",
                subTotal = order.TotalAmount,
                taxAmount = order.TaxAmount,
                grandTotal = order.GrandTotal,
                paymentStatus = order.PaymentStatus ?? "Ödendi",
                paymentMethod = order.PaymentMethod ?? "B2B Kurumsal Kredi Kartı / Cari Hesap",
                orderStatus = order.OrderStatus ?? "Onaylandı",
                orderDate = order.OrderDate.ToString("dd.MM.yyyy HH:mm"),
                deliveryAddress = order.DeliveryAddress ?? (string.IsNullOrWhiteSpace(order.DeliveryCity) ? "-" : order.DeliveryCity),
                shippingCarrier = order.ShippingCarrier ?? "-",
                trackingNumber = order.TrackingNumber ?? "-",
                notes = order.Notes ?? "",
                supplierCount = distinctSuppliers,
                items = order.OrderItems.Select(i => new
                {
                    orderItemId = i.OrderItemID,
                    itemId = i.ItemID,
                    productName = i.ProductName ?? "Ürün",
                    sku = i.Sku ?? $"SKU-{i.ItemID}",
                    categoryName = i.CategoryName ?? "Kategori Belirtilmemiş",
                    unitPrice = i.UnitPrice,
                    quantity = i.Quantity,
                    unit = "Adet",
                    lineTotal = i.LineTotal,
                    supplierId = i.SupplierCompanyID,
                    supplierCompanyName = i.SupplierCompanyName ?? "Onaylı Tedarikçi",
                    itemStatus = i.ItemStatus ?? order.OrderStatus ?? "Hazırlanıyor",
                    trackingNumber = i.TrackingNumber ?? order.TrackingNumber ?? "-",
                    shippingCarrier = i.ShippingCarrier ?? order.ShippingCarrier ?? "-"
                }).ToList()
            });
        }

        [HttpPost]
        [Route("UpdateOrderStatus")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string orderStatus, string? shippingCarrier, string? trackingNumber)
        {
            if (!await CheckIsSuperAdminAsync())
            {
                if (IsAjaxRequest()) return Json(new { success = false, message = "Yetkisiz işlem." });
                return Forbid();
            }

            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderID == orderId);

                if (order == null)
                {
                    var notFoundMsg = "Sipariş bulunamadı.";
                    if (IsAjaxRequest()) return Json(new { success = false, message = notFoundMsg });
                    TempData["AdminError"] = notFoundMsg;
                    return RedirectToAction(nameof(Index), new { tab = "orders" });
                }

                order.OrderStatus = orderStatus.Trim();
                if (!string.IsNullOrWhiteSpace(shippingCarrier)) order.ShippingCarrier = shippingCarrier.Trim();
                if (!string.IsNullOrWhiteSpace(trackingNumber)) order.TrackingNumber = trackingNumber.Trim();

                foreach (var item in order.OrderItems)
                {
                    item.ItemStatus = orderStatus.Trim();
                    if (!string.IsNullOrWhiteSpace(shippingCarrier)) item.ShippingCarrier = shippingCarrier.Trim();
                    if (!string.IsNullOrWhiteSpace(trackingNumber)) item.TrackingNumber = trackingNumber.Trim();
                }

                await _context.SaveChangesAsync();

                var succMsg = $"#{order.OrderNumber} numaralı siparişin durumu '{orderStatus}' olarak güncellendi.";
                if (IsAjaxRequest()) return Json(new { success = true, message = succMsg, orderId = orderId, orderStatus = orderStatus });
                TempData["AdminSuccess"] = succMsg;
            }
            catch (Exception ex)
            {
                var errMsg = "Sipariş güncellenirken hata oluştu: " + ex.Message;
                if (IsAjaxRequest()) return Json(new { success = false, message = errMsg });
                TempData["AdminError"] = errMsg;
            }

            return RedirectToAction(nameof(Index), new { tab = "orders" });
        }

        [HttpPost]
        [Route("UpdateOrderItemStatus")]
        public async Task<IActionResult> UpdateOrderItemStatus(int orderItemId, string itemStatus, string? shippingCarrier, string? trackingNumber)
        {
            if (!await CheckIsSuperAdminAsync())
            {
                if (IsAjaxRequest()) return Json(new { success = false, message = "Yetkisiz işlem." });
                return Forbid();
            }

            try
            {
                var item = await _context.OrderItems
                    .Include(i => i.Order)
                    .FirstOrDefaultAsync(i => i.OrderItemID == orderItemId);

                if (item == null)
                {
                    var notFoundMsg = "Sipariş kalemi bulunamadı.";
                    if (IsAjaxRequest()) return Json(new { success = false, message = notFoundMsg });
                    TempData["AdminError"] = notFoundMsg;
                    return RedirectToAction(nameof(Index), new { tab = "orders" });
                }

                item.ItemStatus = itemStatus.Trim();
                if (!string.IsNullOrWhiteSpace(shippingCarrier)) item.ShippingCarrier = shippingCarrier.Trim();
                if (!string.IsNullOrWhiteSpace(trackingNumber)) item.TrackingNumber = trackingNumber.Trim();

                await _context.SaveChangesAsync();

                var succMsg = $"'{item.ProductName}' ürününün durumu '{itemStatus}' olarak güncellendi.";
                if (IsAjaxRequest()) return Json(new { success = true, message = succMsg, orderItemId = orderItemId, itemStatus = itemStatus });
                TempData["AdminSuccess"] = succMsg;
            }
            catch (Exception ex)
            {
                var errMsg = "Sipariş kalemi güncellenirken hata: " + ex.Message;
                if (IsAjaxRequest()) return Json(new { success = false, message = errMsg });
                TempData["AdminError"] = errMsg;
            }

            return RedirectToAction(nameof(Index), new { tab = "orders" });
        }

        [HttpPost]
        [Route("CancelOrder")]
        public async Task<IActionResult> CancelOrder(int orderId, string? reason)
        {
            if (!await CheckIsSuperAdminAsync())
            {
                if (IsAjaxRequest()) return Json(new { success = false, message = "Yetkisiz işlem." });
                return Forbid();
            }

            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderID == orderId);

                if (order == null)
                {
                    var notFoundMsg = "İptal edilecek sipariş bulunamadı.";
                    if (IsAjaxRequest()) return Json(new { success = false, message = notFoundMsg });
                    TempData["AdminError"] = notFoundMsg;
                    return RedirectToAction(nameof(Index), new { tab = "orders" });
                }

                order.OrderStatus = "İptal Edildi";
                order.PaymentStatus = "İade Edildi";
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    order.Notes = string.IsNullOrWhiteSpace(order.Notes) ? $"[İptal Gerekçesi: {reason.Trim()}]" : $"{order.Notes} [İptal Gerekçesi: {reason.Trim()}]";
                }

                foreach (var item in order.OrderItems)
                {
                    item.ItemStatus = "İptal Edildi";
                }

                await _context.SaveChangesAsync();

                var succMsg = $"#{order.OrderNumber} numaralı sipariş başarıyla iptal edildi ve ödeme durumu 'İade Edildi' olarak güncellendi.";
                if (IsAjaxRequest()) return Json(new { success = true, message = succMsg, orderId = orderId });
                TempData["AdminSuccess"] = succMsg;
            }
            catch (Exception ex)
            {
                var errMsg = "Sipariş iptal edilirken hata oluştu: " + ex.Message;
                if (IsAjaxRequest()) return Json(new { success = false, message = errMsg });
                TempData["AdminError"] = errMsg;
            }

            return RedirectToAction(nameof(Index), new { tab = "orders" });
        }
    }
}
