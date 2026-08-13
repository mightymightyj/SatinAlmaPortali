using Microsoft.AspNetCore.Mvc;
using satinalmaportali2.Services;

namespace satinalmaportali2.Controllers
{
    /// <summary>
    /// Satın alma ve talep analitiği gösterge paneli denetleyicisi.
    /// Tüm veri çekme, haritalama ve önbellekleme işlemleri IDashboardService tarafından yürütülür.
    /// </summary>
    public class DashboardController : Controller
    {
        public const string ConnectionName = "TedarikDb";

        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Dashboard ana görünümü.
        /// </summary>
        /// <param name="yil">Filtrelenecek yıl (isteğe bağlı).</param>
        public async Task<IActionResult> Index(int? yil, CancellationToken cancellationToken)
        {
            var viewModel = await _dashboardService.GetDashboardAsync(yil, cancellationToken);
            return View(viewModel);
        }
    }
}
