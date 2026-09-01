using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using satinalmaportali2.Data;

namespace satinalmaportali2.Services
{
    /// <summary>
    /// Teslim edilen ve alıcı tarafından onay süresi (örn. 7 gün) dolan siparişlerin
    /// havuzdaki ödemesini otomatik olarak tedarikçiye aktaran arka plan servisidir.
    /// </summary>
    public class EscrowAutoApprovalBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EscrowAutoApprovalBackgroundService> _logger;
        private readonly TimeSpan _checkPeriod = TimeSpan.FromHours(4); // 4 saatte bir kontrol

        public EscrowAutoApprovalBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<EscrowAutoApprovalBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EscrowAutoApprovalBackgroundService başlatıldı.");

            // Uygulama ve veritabanı başlangıç (SeedData) işlemlerinin tamamlanması için başlangıç gecikmesi
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndAutoApproveOrdersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Otomatik havuz onayı kontrolü sırasında hata oluştu.");
                }

                await Task.Delay(_checkPeriod, stoppingToken);
            }
        }

        private async Task CheckAndAutoApproveOrdersAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var paymentService = scope.ServiceProvider.GetRequiredService<IIyzicoPaymentService>();

            var now = DateTime.UtcNow;

            // Kural: EscrowStatus == 'InEscrow' veya 'Delivered_WaitingApproval' olan,
            // IsEscrowApproved == false ve AutoApprovalDeadline < now olan siparişler
            var expiredOrders = await context.Orders
                .Where(o => !o.IsEscrowApproved && 
                            o.EscrowStatus != "Released_ToSeller" && 
                            o.EscrowStatus != "Refunded" &&
                            o.EscrowStatus != "Disputed" &&
                            o.AutoApprovalDeadline.HasValue && 
                            o.AutoApprovalDeadline.Value <= now)
                .ToListAsync();

            if (expiredOrders.Any())
            {
                _logger.LogInformation("{Count} adet siparişin otomatik onay süresi doldu. Havuz çözülüyor...", expiredOrders.Count);

                foreach (var order in expiredOrders)
                {
                    try
                    {
                        var (success, message) = await paymentService.ApproveEscrowReleaseAsync(order.OrderID, 0, isAdmin: true);
                        _logger.LogInformation("Sipariş #{OrderNumber} otomatik onay sonucu: {Success} - {Message}", order.OrderNumber, success, message);
                    }
                    catch (Exception orderEx)
                    {
                        _logger.LogError(orderEx, "Sipariş #{OrderNumber} otomatik onaylanırken hata oluştu.", order.OrderNumber);
                    }
                }
            }
        }
    }
}
