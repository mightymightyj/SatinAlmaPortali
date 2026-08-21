using satinalmaportali2.Models;

namespace satinalmaportali2.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardAsync(int? selectedYear, CancellationToken cancellationToken = default);
        Task<BuyerDashboardViewModel> GetBuyerDashboardAsync(int? selectedYear, int? currentCompanyId = null, CancellationToken cancellationToken = default);
        Task<SupplierDashboardViewModel> GetSupplierDashboardAsync(int? selectedYear, int? currentCompanyId = null, CancellationToken cancellationToken = default);
        Task<Category> CreateCategoryAsync(string categoryName, int? parentCategoryId, CancellationToken cancellationToken = default);
    }
}
