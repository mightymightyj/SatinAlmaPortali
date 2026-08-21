using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using satinalmaportali2.Data;
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
        /// Kullanıcının firma türü ID'sini döner:
        /// 1 = Tedarikçi Firma (Yalnızca Tedarikçi)
        /// 2 = Alıcı / Müşteri (Yalnızca Alıcı)
        /// 3 = Hem Alıcı Hem Tedarikçi (Her İkisi)
        /// </summary>
        private async Task<int> GetCurrentCompanyTypeIdAsync()
        {
            var userType = User.FindFirst("UserType")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.Equals(userType, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return 3; // Admin hesaplar her iki panele de erişebilir
            }

            var typeClaim = User.FindFirst("CompanyTypeID")?.Value;
            if (int.TryParse(typeClaim, out int claimTypeId) && claimTypeId >= 1 && claimTypeId <= 3)
            {
                return claimTypeId;
            }

            int companyId = GetCurrentCompanyId();
            if (companyId > 0)
            {
                var comp = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == companyId);
                if (comp?.CompanyTypeID.HasValue == true && comp.CompanyTypeID.Value >= 1 && comp.CompanyTypeID.Value <= 3)
                {
                    return comp.CompanyTypeID.Value;
                }
            }

            return 3; // Belirlenemezse varsayılan her iki modu da aç
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
        /// Yalnızca türü 'Hem Alıcı Hem Tedarikçi' (3) veya Admin olan hesaplar mod değiştirebilir.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SetRole(string role, string? returnUrl = null)
        {
            int companyTypeId = await GetCurrentCompanyTypeIdAsync();
            string targetRole;

            if (companyTypeId == 1) // Yalnızca Tedarikçi
            {
                targetRole = "Supplier";
            }
            else if (companyTypeId == 2) // Yalnızca Alıcı
            {
                targetRole = "Buyer";
            }
            else // Her İkisi veya Admin
            {
                targetRole = string.Equals(role, "Supplier", StringComparison.OrdinalIgnoreCase) ? "Supplier" : "Buyer";
            }

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
        /// Dashboard ana görünümü (Kullanıcının türüne ve aktif moduna göre yönlendirir).
        /// </summary>
        public async Task<IActionResult> Index(int? yil, string? role, CancellationToken cancellationToken)
        {
            int companyTypeId = await GetCurrentCompanyTypeIdAsync();

            // 1. Sadece Tedarikçi olan hesaplar doğrudan Tedarikçi ekranına gider
            if (companyTypeId == 1)
            {
                SetDashboardModeCookie("Supplier");
                return RedirectToAction(nameof(Supplier), new { yil });
            }

            // 2. Sadece Alıcı olan hesaplar doğrudan Alıcı ekranına gider
            if (companyTypeId == 2)
            {
                SetDashboardModeCookie("Buyer");
                return RedirectToAction(nameof(Buyer), new { yil });
            }

            // 3. Her İkisi olan hesaplar seçilen veya cookie'deki moda göre yönlendirilir
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
        /// Yalnızca Tedarikçi olan hesaplar bu ekrana erişemez, Tedarikçi paneline yönlendirilir.
        /// </summary>
        public async Task<IActionResult> Buyer(int? yil, CancellationToken cancellationToken)
        {
            int companyTypeId = await GetCurrentCompanyTypeIdAsync();
            if (companyTypeId == 1) // Yalnızca Tedarikçi -> Alıcı ekranını göremez
            {
                return RedirectToAction(nameof(Supplier), new { yil });
            }

            SetDashboardModeCookie("Buyer");
            ViewBag.DashboardRole = "Buyer";
            ViewBag.CanSwitchRole = (companyTypeId == 3 || string.Equals(User.FindFirst("UserType")?.Value, "admin", StringComparison.OrdinalIgnoreCase));
            ViewBag.CompanyTypeID = companyTypeId;
            ViewData["Title"] = "Alıcı Portalı - Genel Bakış";

            int companyId = GetCurrentCompanyId();
            var viewModel = await _dashboardService.GetBuyerDashboardAsync(yil, companyId > 0 ? companyId : null, cancellationToken);
            return View(viewModel);
        }

        /// <summary>
        /// Tedarikçi (Supplier) kullanıcı rolü dashboard görünümü.
        /// Yalnızca Alıcı olan hesaplar bu ekrana erişemez, Alıcı paneline yönlendirilir.
        /// </summary>
        public async Task<IActionResult> Supplier(int? yil, CancellationToken cancellationToken)
        {
            int companyTypeId = await GetCurrentCompanyTypeIdAsync();
            if (companyTypeId == 2) // Yalnızca Alıcı -> Tedarikçi ekranını göremez
            {
                return RedirectToAction(nameof(Buyer), new { yil });
            }

            SetDashboardModeCookie("Supplier");
            ViewBag.DashboardRole = "Supplier";
            ViewBag.CanSwitchRole = (companyTypeId == 3 || string.Equals(User.FindFirst("UserType")?.Value, "admin", StringComparison.OrdinalIgnoreCase));
            ViewBag.CompanyTypeID = companyTypeId;
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
            int companyTypeId = await GetCurrentCompanyTypeIdAsync();
            var currentMode = companyTypeId == 1 ? "Supplier" : (companyTypeId == 2 ? "Buyer" : GetActiveDashboardMode());
            ViewBag.DashboardRole = currentMode;
            ViewBag.CanSwitchRole = (companyTypeId == 3 || string.Equals(User.FindFirst("UserType")?.Value, "admin", StringComparison.OrdinalIgnoreCase));
            ViewBag.CompanyTypeID = companyTypeId;
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
