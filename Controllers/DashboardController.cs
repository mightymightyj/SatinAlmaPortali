using System.Security.Claims;
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

        private int GetCurrentCompanyId()
        {
            var cidClaim = User.FindFirst("CompanyID")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(cidClaim) && int.TryParse(cidClaim, out int cid))
            {
                return cid;
            }
            return 0;
        }

        private void SetDashboardModeCookie(string role)
        {
            Response.Cookies.Append("ActiveDashboardMode", role, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = false,
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });
        }

        private string GetActiveDashboardMode()
        {
            var cookie = Request.Cookies["ActiveDashboardMode"];
            if (!string.IsNullOrWhiteSpace(cookie) && string.Equals(cookie, "Supplier", StringComparison.OrdinalIgnoreCase))
            {
                return "Supplier";
            }
            return "Buyer";
        }

        /// <summary>
        /// Kullanıcının aktif panel modunu (Alıcı / Tedarikçi) kalıcı olarak değiştirir.
        /// </summary>
        [HttpGet]
        public IActionResult SetRole(string role, string? returnUrl = null)
        {
            string targetRole = string.Equals(role, "Supplier", StringComparison.OrdinalIgnoreCase) ? "Supplier" : "Buyer";
            SetDashboardModeCookie(targetRole);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return targetRole == "Supplier" 
                ? RedirectToAction(nameof(Supplier)) 
                : RedirectToAction(nameof(Buyer));
        }

        /// <summary>
        /// Dashboard ana görünümü (Kullanıcının aktif moduna göre yönlendirir).
        /// </summary>
        /// <param name="yil">Filtrelenecek yıl (isteğe bağlı).</param>
        /// <param name="role">Rol seçimi: "buyer" veya "supplier".</param>
        public async Task<IActionResult> Index(int? yil, string? role, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(role))
            {
                SetDashboardModeCookie(role);
                if (string.Equals(role, "supplier", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction(nameof(Supplier), new { yil });
                }
                return RedirectToAction(nameof(Buyer), new { yil });
            }

            var currentMode = GetActiveDashboardMode();
            if (currentMode == "Supplier")
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
            SetDashboardModeCookie("Buyer");
            ViewBag.DashboardRole = "Buyer";
            ViewData["Title"] = "Alıcı Portalı - Genel Bakış";
            int companyId = GetCurrentCompanyId();
            var viewModel = await _dashboardService.GetBuyerDashboardAsync(yil, companyId > 0 ? companyId : null, cancellationToken);
            return View(viewModel);
        }

        /// <summary>
        /// Tedarikçi (Supplier) kullanıcı rolü dashboard görünümü.
        /// </summary>
        public async Task<IActionResult> Supplier(int? yil, CancellationToken cancellationToken)
        {
            SetDashboardModeCookie("Supplier");
            ViewBag.DashboardRole = "Supplier";
            ViewData["Title"] = "Tedarikçi Portalı - Mağaza Genel Bakış";
            int companyId = GetCurrentCompanyId();
            var viewModel = await _dashboardService.GetSupplierDashboardAsync(yil, companyId > 0 ? companyId : null, cancellationToken);
            return View(viewModel);
        }

        /// <summary>
        /// Sistem genel analitik görünümü.
        /// </summary>
        public async Task<IActionResult> Analytics(int? yil, CancellationToken cancellationToken)
        {
            var currentMode = GetActiveDashboardMode();
            ViewBag.DashboardRole = currentMode;
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
