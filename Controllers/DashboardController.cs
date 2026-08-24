using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using satinalmaportali2.Data;
using satinalmaportali2.Models;
using satinalmaportali2.Services;

namespace satinalmaportali2.Controllers
{
    /// <summary>
    /// Satın alma ve talep analitiği gösterge paneli denetleyicisi.
    /// Hesap türüne göre (Alıcı, Tedarikçi, Her İkisi) erişim ve mod kontrolleri burada yönetilir.
    /// </summary>
    [Authorize]
    public class DashboardController : Controller
    {
        public const string ConnectionName = "TedarikDb";

        private readonly IDashboardService _dashboardService;
        private readonly ApplicationDbContext _context;

        public DashboardController(IDashboardService dashboardService, ApplicationDbContext context)
        {
            _dashboardService = dashboardService;
            _context = context;
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
        /// Dashboard ana yönetim merkezi görünümü.
        /// </summary>
        public async Task<IActionResult> Index(int? yil, CancellationToken cancellationToken)
        {
            ViewData["Title"] = "Tedarik Portalı - Genel Bakış";
            int companyId = GetCurrentCompanyId();
            var viewModel = await _dashboardService.GetBuyerDashboardAsync(yil, companyId > 0 ? companyId : null, cancellationToken);
            return View("Buyer", viewModel);
        }

        /// <summary>
        /// Alıcı aksiyonu (Doğrudan ana dashboard'u açar).
        /// </summary>
        public async Task<IActionResult> Buyer(int? yil, CancellationToken cancellationToken)
        {
            return await Index(yil, cancellationToken);
        }

        /// <summary>
        /// Tedarikçi gösterge paneli görünümü.
        /// </summary>
        public async Task<IActionResult> Supplier(int? yil, CancellationToken cancellationToken)
        {
            ViewData["Title"] = "Tedarikçi Satış & RFQ Yönetimi";
            int companyId = GetCurrentCompanyId();
            var viewModel = await _dashboardService.GetSupplierDashboardAsync(yil, companyId > 0 ? companyId : null, cancellationToken);
            return View("Supplier", viewModel);
        }

        /// <summary>
        /// Katalog ürününün birim fiyatını ve stok adedini günceller (AJAX).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateProductPrice([FromBody] UpdateProductPriceInputModel input)
        {
            if (input == null || input.ItemID <= 0 || input.NewUnitPrice <= 0)
            {
                return Json(new { success = false, message = "Geçerli bir ürün ve fiyat bilgisi giriniz." });
            }

            try
            {
                var item = await _context.Items.FirstOrDefaultAsync(i => i.ItemID == input.ItemID);
                if (item == null)
                {
                    return Json(new { success = false, message = "Güncellenecek ürün veritabanında bulunamadı." });
                }

                item.ItemPrice = (double)input.NewUnitPrice;
                if (input.NewStockQuantity.HasValue && input.NewStockQuantity.Value >= 0)
                {
                    item.Quantity = input.NewStockQuantity.Value;
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"'{item.ItemName}' birim fiyatı {input.NewUnitPrice:N0} ₺ olarak başarıyla güncellendi.",
                    itemId = item.ItemID,
                    newPrice = input.NewUnitPrice,
                    newStock = item.Quantity
                });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Fiyat güncellenirken hata oluştu: " + detail });
            }
        }

        /// <summary>
        /// Sistem genel analitik görünümü.
        /// </summary>
        public async Task<IActionResult> Analytics(int? yil, CancellationToken cancellationToken)
        {
            ViewData["Title"] = "Satın Alma ve Talep Analitiği";
            var viewModel = await _dashboardService.GetDashboardAsync(yil, cancellationToken);
            return View("Analytics", viewModel);
        }

        /// <summary>
        /// Tedarikçi ürün ilanları ve vitrin kataloğu yönetim sekmesi.
        /// </summary>
        public async Task<IActionResult> Listings(string? search, string? category, string? status, CancellationToken cancellationToken)
        {
            ViewData["Title"] = "Ürün İlanlarım & Vitrin Kataloğu";
            int companyId = GetCurrentCompanyId();

            string currentCompanyName = User.FindFirst("CompanyName")?.Value ?? "";
            if (companyId > 0 && string.IsNullOrWhiteSpace(currentCompanyName))
            {
                var comp = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == companyId, cancellationToken);
                currentCompanyName = comp?.CompanyName ?? comp?.CompanyOfficial ?? "";
            }

            // Veritabanındaki ürünleri sorgula
            var query = _context.Items.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(currentCompanyName))
            {
                var userItemsQuery = query.Where(i => i.Brand == currentCompanyName);
                if (await userItemsQuery.AnyAsync(cancellationToken))
                {
                    query = userItemsQuery;
                }
            }

            var dbItems = await query.OrderByDescending(i => i.ItemID).ToListAsync(cancellationToken);

            // Kategori eşleştirmeleri
            var catIds = dbItems.Where(i => i.CategoryID.HasValue).Select(i => i.CategoryID!.Value).Distinct().ToList();
            var categoriesMap = await _context.Category
                .AsNoTracking()
                .Where(c => catIds.Contains(c.CategoryID))
                .ToDictionaryAsync(c => c.CategoryID, c => c.CategoryName ?? "Genel", cancellationToken);

            var allCategoryNames = await _context.Category
                .AsNoTracking()
                .Where(c => !string.IsNullOrEmpty(c.CategoryName))
                .Select(c => c.CategoryName!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(cancellationToken);

            var listingDtos = dbItems.Select(item =>
            {
                string catName = "Endüstriyel Ürünler";
                if (item.CategoryID.HasValue && categoriesMap.TryGetValue(item.CategoryID.Value, out var cName))
                {
                    catName = cName;
                }
                else if (!string.IsNullOrWhiteSpace(item.Properties) && item.Properties.StartsWith("Kategori: "))
                {
                    catName = item.Properties.Substring("Kategori: ".Length);
                }

                int stock = item.Quantity ?? 0;
                string itemStatus = stock <= 0 ? "Tükendi" : (stock < 50 ? "Kritik" : "Stokta");

                return new SupplierListingDetailDto
                {
                    ItemId = item.ItemID,
                    ItemName = item.ItemName ?? "İsimsiz Ürün",
                    Sku = $"SKU-B2B-{item.ItemID:D4}",
                    CategoryName = catName,
                    CategoryId = item.CategoryID,
                    Stock = stock,
                    UnitPrice = (decimal)(item.ItemPrice ?? 0),
                    Brand = item.Brand ?? (!string.IsNullOrWhiteSpace(currentCompanyName) ? currentCompanyName : "Onaylı Tedarikçi"),
                    Description = item.ItemDescryption,
                    Properties = item.Properties,
                    Status = itemStatus,
                    IsLiveOnVitrin = true
                };
            }).ToList();

            // Filtrelemeler
            var filtered = listingDtos.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                filtered = filtered.Where(x => x.ItemName.ToLowerInvariant().Contains(s) || x.Sku.ToLowerInvariant().Contains(s) || x.CategoryName.ToLowerInvariant().Contains(s) || (x.Description != null && x.Description.ToLowerInvariant().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "Tümü")
            {
                filtered = filtered.Where(x => x.CategoryName.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "Tümü")
            {
                filtered = filtered.Where(x => x.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            var resultList = filtered.ToList();

            var vm = new SupplierListingsViewModel
            {
                Listings = resultList,
                TotalCount = listingDtos.Count,
                TotalCatalogValue = listingDtos.Sum(x => x.UnitPrice * x.Stock),
                ActiveCount = listingDtos.Count(x => x.Status == "Stokta"),
                LowStockCount = listingDtos.Count(x => x.Status == "Kritik" || x.Status == "Tükendi"),
                AvailableCategories = allCategoryNames,
                SelectedCategory = category,
                SearchQuery = search,
                SelectedStatus = status,
                CompanyName = !string.IsNullOrWhiteSpace(currentCompanyName) ? currentCompanyName : "Kurumsal Tedarikçi"
            };

            var allCats = await _context.Category.AsNoTracking().ToListAsync(cancellationToken);
            ViewBag.CategoryTree = BuildCategoryHierarchy(allCats);

            return View(vm);
        }

        /// <summary>
        /// Ürün ilanını veritabanından siler (AJAX).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> DeleteProductListing(int id)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "Geçersiz ürün ID." });
            }

            try
            {
                var item = await _context.Items.FirstOrDefaultAsync(i => i.ItemID == id);
                if (item == null)
                {
                    return Json(new { success = false, message = "Silinecek ürün veritabanında bulunamadı." });
                }

                _context.Items.Remove(item);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"\"{item.ItemName}\" ilanı katalogdan ve vitrinden kaldırıldı." });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "İlan silinirken hata oluştu: " + detail });
            }
        }

        private List<HierarchicalCategoryItem> BuildCategoryHierarchy(List<Category> allCategories)
        {
            var categoryMap = allCategories.ToDictionary(c => c.CategoryID, c => new HierarchicalCategoryItem
            {
                CategoryID = c.CategoryID,
                CategoryName = c.CategoryName ?? string.Empty,
                ParentCategoryID = c.ParentCategoryID,
                Children = new List<HierarchicalCategoryItem>()
            });

            var rootItems = new List<HierarchicalCategoryItem>();

            foreach (var cat in allCategories)
            {
                var item = categoryMap[cat.CategoryID];
                if (cat.ParentCategoryID.HasValue && categoryMap.TryGetValue(cat.ParentCategoryID.Value, out var parentItem))
                {
                    item.ParentCategoryName = parentItem.CategoryName;
                    parentItem.Children.Add(item);
                    parentItem.HasChildren = true;
                }
                else
                {
                    rootItems.Add(item);
                }
            }

            void ComputePaths(HierarchicalCategoryItem current, string currentPath, int level)
            {
                current.Level = level;
                current.BreadcrumbPath = string.IsNullOrEmpty(currentPath) ? current.CategoryName : $"{currentPath} > {current.CategoryName}";
                current.FormattedName = $"{new string('-', level * 2)} {current.CategoryName}".TrimStart();

                foreach (var child in current.Children)
                {
                    ComputePaths(child, current.BreadcrumbPath, level + 1);
                }
            }

            foreach (var root in rootItems)
            {
                ComputePaths(root, string.Empty, 0);
            }

            return rootItems;
        }

        /// <summary>
        /// Alıcı firmalar için verilen siparişler takip sekmesi.
        /// </summary>
        public async Task<IActionResult> Orders(string? search, string? status, CancellationToken cancellationToken)
        {
            ViewData["Title"] = "Verilen Siparişlerim & Sevkiyat Takibi";
            int companyId = GetCurrentCompanyId();
            string currentCompanyName = User.FindFirst("CompanyName")?.Value ?? "";

            var query = _context.Orders
                .Include(o => o.OrderItems)
                .AsNoTracking()
                .AsQueryable();

            if (companyId > 0)
            {
                var userOrdersQuery = query.Where(o => o.BuyerCompanyID == companyId);
                if (await userOrdersQuery.AnyAsync(cancellationToken))
                {
                    query = userOrdersQuery;
                }
            }

            var allOrders = await query.OrderByDescending(o => o.OrderID).ToListAsync(cancellationToken);

            var filtered = allOrders.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                filtered = filtered.Where(o => o.OrderNumber.ToLowerInvariant().Contains(s) || 
                                               (o.SupplierCompanyName != null && o.SupplierCompanyName.ToLowerInvariant().Contains(s)) ||
                                               (o.TrackingNumber != null && o.TrackingNumber.ToLowerInvariant().Contains(s)) ||
                                               o.OrderItems.Any(oi => oi.ProductName.ToLowerInvariant().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "Tümü")
            {
                filtered = filtered.Where(o => o.OrderStatus.Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            var result = filtered.ToList();

            var vm = new BuyerOrdersViewModel
            {
                Orders = result,
                TotalOrderCount = allOrders.Count,
                TotalSpent = allOrders.Sum(o => o.GrandTotal),
                ActiveShipmentCount = allOrders.Count(o => o.OrderStatus == "Kargoya Verildi" || o.OrderStatus == "Hazırlanıyor"),
                CompletedCount = allOrders.Count(o => o.OrderStatus == "Teslim Edildi" || o.OrderStatus == "Tamamlandı"),
                SearchQuery = search,
                SelectedStatus = status,
                CompanyName = !string.IsNullOrWhiteSpace(currentCompanyName) ? currentCompanyName : "Kurumsal Alıcı"
            };

            return View("Orders", vm);
        }

        /// <summary>
        /// Tedarikçi firmalar için gelen kurumsal siparişler ve sevkiyat yönetim sekmesi.
        /// </summary>
        public async Task<IActionResult> SupplierOrders(string? search, string? status, CancellationToken cancellationToken)
        {
            ViewData["Title"] = "Gelen Siparişler & Sevkiyat Yönetimi";
            int companyId = GetCurrentCompanyId();
            string currentCompanyName = User.FindFirst("CompanyName")?.Value ?? "";

            var query = _context.Orders
                .Include(o => o.OrderItems)
                .AsNoTracking()
                .AsQueryable();

            if (companyId > 0)
            {
                var suppOrdersQuery = query.Where(o => o.SupplierCompanyID == companyId || (o.SupplierCompanyName != null && o.SupplierCompanyName == currentCompanyName));
                if (await suppOrdersQuery.AnyAsync(cancellationToken))
                {
                    query = suppOrdersQuery;
                }
            }

            var allOrders = await query.OrderByDescending(o => o.OrderID).ToListAsync(cancellationToken);

            var filtered = allOrders.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                filtered = filtered.Where(o => o.OrderNumber.ToLowerInvariant().Contains(s) || 
                                               o.BuyerCompanyName.ToLowerInvariant().Contains(s) ||
                                               (o.TrackingNumber != null && o.TrackingNumber.ToLowerInvariant().Contains(s)) ||
                                               o.OrderItems.Any(oi => oi.ProductName.ToLowerInvariant().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "Tümü")
            {
                filtered = filtered.Where(o => o.OrderStatus.Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            var result = filtered.ToList();

            var vm = new SupplierOrdersViewModel
            {
                Orders = result,
                TotalOrderCount = allOrders.Count,
                TotalRevenue = allOrders.Sum(o => o.GrandTotal),
                PendingShipmentCount = allOrders.Count(o => o.OrderStatus == "Onaylandı" || o.OrderStatus == "Hazırlanıyor"),
                CompletedCount = allOrders.Count(o => o.OrderStatus == "Teslim Edildi" || o.OrderStatus == "Tamamlandı"),
                SearchQuery = search,
                SelectedStatus = status,
                CompanyName = !string.IsNullOrWhiteSpace(currentCompanyName) ? currentCompanyName : "Kurumsal Tedarikçi"
            };

            return View("SupplierOrders", vm);
        }

        /// <summary>
        /// Sipariş durumunu ve kargo takip bilgisini günceller (AJAX).
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusInputModel input)
        {
            if (input == null || input.OrderID <= 0)
            {
                return Json(new { success = false, message = "Geçersiz sipariş ID." });
            }

            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderID == input.OrderID);
                if (order == null)
                {
                    return Json(new { success = false, message = "Sipariş veritabanında bulunamadı." });
                }

                if (!string.IsNullOrWhiteSpace(input.OrderStatus))
                {
                    order.OrderStatus = input.OrderStatus.Trim();
                }

                if (!string.IsNullOrWhiteSpace(input.TrackingNumber))
                {
                    order.TrackingNumber = input.TrackingNumber.Trim();
                }

                if (!string.IsNullOrWhiteSpace(input.ShippingCarrier))
                {
                    order.ShippingCarrier = input.ShippingCarrier.Trim();
                }

                if (!string.IsNullOrWhiteSpace(input.Notes))
                {
                    order.Notes = input.Notes.Trim();
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"{order.OrderNumber} numaralı sipariş durumu '{order.OrderStatus}' olarak güncellendi.",
                    orderId = order.OrderID,
                    status = order.OrderStatus,
                    trackingNumber = order.TrackingNumber,
                    shippingCarrier = order.ShippingCarrier
                });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Sipariş güncellenirken hata oluştu: " + detail });
            }
        }

        /// <summary>
        /// Yeni kategori veya alt kategori oluşturur.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string categoryName, int? parentCategoryId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["CategoryError"] = "Lütfen bir kategori adı girin.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var created = await _dashboardService.CreateCategoryAsync(categoryName, parentCategoryId, cancellationToken);
                TempData["CategorySuccess"] = parentCategoryId.HasValue && parentCategoryId.Value > 0
                    ? $"Alt kategori '{created.CategoryName}' başarıyla eklendi!" 
                    : $"Kök kategori '{created.CategoryName}' (ParentCategoryID = NULL) başarıyla eklendi!";
            }
            catch (Exception ex)
            {
                TempData["CategoryError"] = "Kategori oluşturulurken hata oluştu: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
