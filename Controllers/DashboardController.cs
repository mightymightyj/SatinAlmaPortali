using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using satinalmaportali2.Services;

namespace satinalmaportali2.Controllers
{
    /// <summary>
    /// Satın alma ve talep analitiği gösterge paneli denetleyicisi.
    /// Tüm veri çekme, haritalama ve önbellekleme işlemleri IDashboardService tarafından yürütülür.
    /// </summary>
    [Authorize]
    public class DashboardController : Controller
    {
        public const string ConnectionName = "TedarikDb";

        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Dashboard ana görünümü (Varsayılan olarak Alıcı / Buyer paneline yönlendirir veya seçilen rolü açar).
        /// </summary>
        /// <param name="yil">Filtrelenecek yıl (isteğe bağlı).</param>
        /// <param name="role">Rol seçimi: "buyer" veya "supplier".</param>
        public async Task<IActionResult> Index(int? yil, string? role, CancellationToken cancellationToken)
        {
            if (string.Equals(role, "supplier", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Supplier), new { yil });
            }

            return RedirectToAction(nameof(Buyer), new { yil });
        }

        /// <summary>
        /// Alıcı (Buyer) kullanıcı rolü dashboard görünümü.
        /// </summary>
        public async Task<IActionResult> Buyer(int? yil, CancellationToken cancellationToken)
        {
            ViewBag.DashboardRole = "Buyer";
            ViewData["Title"] = "Alıcı Portalı - Genel Bakış";
            var viewModel = await _dashboardService.GetBuyerDashboardAsync(yil, cancellationToken);
            return View(viewModel);
        }

        /// <summary>
        /// Tedarikçi (Supplier) kullanıcı rolü dashboard görünümü.
        /// </summary>
        public async Task<IActionResult> Supplier(int? yil, CancellationToken cancellationToken)
        {
            ViewBag.DashboardRole = "Supplier";
            ViewData["Title"] = "Tedarikçi Portalı - Mağaza Genel Bakış";
            var viewModel = await _dashboardService.GetSupplierDashboardAsync(yil, cancellationToken);
            return View(viewModel);
        }

        /// <summary>
        /// Sistem genel analitik görünümü.
        /// </summary>
        public async Task<IActionResult> Analytics(int? yil, CancellationToken cancellationToken)
        {
            ViewBag.DashboardRole = "Buyer";
            ViewData["Title"] = "Satın Alma ve Talep Analitiği";
            var viewModel = await _dashboardService.GetDashboardAsync(yil, cancellationToken);
            return View("Analytics", viewModel);
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
