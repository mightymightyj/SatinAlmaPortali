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
        public async Task<IActionResult> Index(string? tab = "overview", string? search = null, int? filterType = null)
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
                FilterCompanyType = filterType
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

                // 7. Değerlendirmeler Listesi & Siparişler (10★ İtibar Karnesi Testi)
                var dbReviews = await _context.Reviews
                    .Include(r => r.Order)
                    .Include(r => r.TargetCompany)
                    .Include(r => r.ReviewerCompany)
                    .AsNoTracking()
                    .OrderByDescending(r => r.ReviewDate)
                    .ToListAsync();

                var dbOrders = await _context.Orders
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
                    ReviewText = r.ReviewText,
                    ReviewDate = r.ReviewDate,
                    OrderNumber = r.Order?.OrderNumber,
                    OrderID = r.OrderID,
                    IsVerifiedPurchase = r.IsVerifiedPurchase
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

                viewModel.Stats.TotalReviews = dbReviews.Count;
                viewModel.Stats.AveragePlatformScore = dbReviews.Any() ? Math.Round(dbReviews.Average(r => r.ReviewScore ?? 10.0), 1) : 10.0;
                viewModel.Stats.BuyerToSellerReviewsCount = dbReviews.Count(r => r.ReviewType == "BuyerToSeller");
                viewModel.Stats.SellerToBuyerReviewsCount = dbReviews.Count(r => r.ReviewType == "SellerToBuyer");

                // Filtreleme (Kategoriler & Firmalar)
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
                }

                if (filterType.HasValue && filterType.Value > 0)
                {
                    viewModel.Companies = viewModel.Companies.Where(c => c.CompanyTypeID == filterType.Value).ToList();
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

        [HttpPost]
        [Route("SimulateCreateRequest")]
        public async Task<IActionResult> SimulateCreateRequest(SimulateRequestInputModel input)
        {
            if (!await CheckIsSuperAdminAsync())
            {
                if (IsAjaxRequest()) return Json(new { success = false, message = "Yetkisiz işlem." });
                return Forbid();
            }

            if (!ModelState.IsValid || input.SenderCompanyID <= 0 || string.IsNullOrWhiteSpace(input.ProductName) || input.Quantity <= 0)
            {
                var reqMsg = "Lütfen talep açan firma, ürün adı ve geçerli bir adet giriniz.";
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

                var newRequest = new Requests
                {
                    RequestDate = DateTime.UtcNow,
                    SenderID = senderComp.CompanyID,
                    BuyerInfo = buyerInfo,
                    ProductName = input.ProductName.Trim(),
                    Quantity = input.Quantity,
                    RequestType = string.IsNullOrWhiteSpace(input.RequestType) ? "Satın Alma" : input.RequestType.Trim(),
                    CategoryID = input.CategoryID,
                    EstimatedBudget = input.EstimatedBudget,
                    TargetDeliveryDate = input.TargetDeliveryDate,
                    Status = "Beklemede",
                    Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim()
                };

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
                        quantity = newRequest.Quantity ?? 1,
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

                var succMsg = $"'{senderComp.CompanyName}' adına '#REQ-{newRequest.RequestID} - {newRequest.ProductName}' talebi başarıyla oluşturuldu ve yayınlandı!";
                if (IsAjaxRequest()) return Json(new { success = true, message = succMsg, requestId = newRequest.RequestID, productName = newRequest.ProductName });
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
        public async Task<IActionResult> SimulateCreateOffer(SimulateOfferInputModel input)
        {
            if (!await CheckIsSuperAdminAsync())
            {
                if (IsAjaxRequest()) return Json(new { success = false, message = "Yetkisiz işlem." });
                return Forbid();
            }

            if (!ModelState.IsValid || input.RequestID <= 0 || input.SupplierCompanyID <= 0 || input.OfferedPrice <= 0)
            {
                var reqMsg = "Lütfen talep, tedarikçi firma ve geçerli bir teklif tutarı giriniz.";
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

                var existingOffer = await _context.RequestOffers
                    .Include(o => o.OfferItems)
                    .FirstOrDefaultAsync(o => o.RequestID == input.RequestID && o.SupplierID == supplier.CompanyID);
                string succMsg;
                if (existingOffer != null)
                {
                    existingOffer.OfferedPrice = input.OfferedPrice;
                    existingOffer.DeliveryDays = input.DeliveryDays ?? 7;
                    existingOffer.ValidityDays = input.ValidityDays ?? 30;
                    existingOffer.SupplierNotes = string.IsNullOrWhiteSpace(input.SupplierNotes) ? null : input.SupplierNotes.Trim();
                    existingOffer.OfferDate = DateTime.UtcNow;
                    existingOffer.Status = "Beklemede";

                    succMsg = $"'{supplier.CompanyName}' adına '#REQ-{request.RequestID}' talebine verilen teklif {input.OfferedPrice:N2} ₺ olarak güncellendi!";
                }
                else
                {
                    var newOffer = new RequestOffers
                    {
                        RequestID = input.RequestID,
                        SupplierID = supplier.CompanyID,
                        OfferedPrice = input.OfferedPrice,
                        DeliveryDays = input.DeliveryDays ?? 7,
                        ValidityDays = input.ValidityDays ?? 30,
                        SupplierNotes = string.IsNullOrWhiteSpace(input.SupplierNotes) ? null : input.SupplierNotes.Trim(),
                        OfferDate = DateTime.UtcNow,
                        Status = "Beklemede"
                    };

                    _context.RequestOffers.Add(newOffer);
                    succMsg = $"'{supplier.CompanyName}' adına '#REQ-{request.RequestID} - {request.ProductName}' talebine {input.OfferedPrice:N2} ₺ teklif başarıyla iletildi!";
                }

                await _context.SaveChangesAsync();

                try
                {
                    var offerToSend = existingOffer ?? await _context.RequestOffers.FirstOrDefaultAsync(o => o.RequestID == input.RequestID && o.SupplierID == supplier.CompanyID);
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

                // E-posta Bildirimi Gönderimi (Talep sahibine)
                if (request.SenderID.HasValue)
                {
                    var ownerCompany = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == request.SenderID.Value);
                    if (ownerCompany != null && !string.IsNullOrWhiteSpace(ownerCompany.CompanyMail))
                    {
                        string ownerEmail = ownerCompany.CompanyMail;
                        string ownerName = ownerCompany.CompanyOfficial ?? ownerCompany.CompanyName ?? "Sayın Alıcı";
                        string supplierName = supplier.CompanyName ?? supplier.CompanyOfficial ?? "Tedarikçi";

                        var emailItems = new List<OfferItemEmailDto>();
                        if (request.Items != null && request.Items.Any())
                        {
                            int order = 1;
                            decimal totalQty = request.Items.Sum(ri => ri.Quantity);
                            foreach (var ri in request.Items)
                            {
                                decimal proportion = totalQty > 0 ? (ri.Quantity / totalQty) : (1m / request.Items.Count);
                                decimal lineTot = Math.Round(input.OfferedPrice * proportion, 2);
                                decimal uPrice = ri.Quantity > 0 ? Math.Round(lineTot / ri.Quantity, 2) : lineTot;

                                emailItems.Add(new OfferItemEmailDto
                                {
                                    ItemOrder = order++,
                                    ItemName = ri.ItemName,
                                    Quantity = ri.Quantity,
                                    Unit = ri.Unit ?? "Adet",
                                    UnitPrice = uPrice,
                                    LineTotal = lineTot,
                                    ItemDescription = ri.ItemDescription,
                                    Notes = null
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
                                    offerPrice: input.OfferedPrice,
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

                if (IsAjaxRequest()) return Json(new { success = true, message = succMsg, requestId = request.RequestID, offeredPrice = input.OfferedPrice });
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
                    DeliveryScore = input.DeliveryScore ?? input.ReviewScore,
                    QualityScore = input.QualityScore ?? input.ReviewScore,
                    CommunicationScore = input.CommunicationScore ?? input.ReviewScore,
                    ReviewText = input.ReviewText?.Trim(),
                    ReviewDate = DateTime.UtcNow,
                    IsVerifiedPurchase = input.IsVerifiedPurchase
                };

                _context.Reviews.Add(newReview);
                await _context.SaveChangesAsync();

                var succMsg = $"'{targetComp.CompanyName}' firması için {input.ReviewScore:0.0}★ değerindeki test değerlendirmesi başarıyla kaydedildi! (ID: #{newReview.ReviewID})";
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
                        ReviewScore = Math.Round(9.0 + rand.NextDouble(), 1), // 9.0 - 10.0
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

                var succMsg = $"Sistemdeki {corporateCompanies.Count} firma için toplam {createdCount} adet gerçekçi 10★ demo değerlendirme üretildi!";
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
    }
}
