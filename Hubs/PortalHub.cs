using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace satinalmaportali2.Hubs
{
    /// <summary>
    /// Satın Alma Portalı Gerçek Zamanlı İletişim Hub'ı (SignalR)
    /// Değerlendirmeler, canlı puan güncellemeleri ve kurumsal bildirimler bu kanal üzerinden asenkron iletilir.
    /// </summary>
    public class PortalHub : Hub
    {
        /// <summary>
        /// Giriş yapan firmanın kendi bildirim kanalına abone olması
        /// </summary>
        public async Task JoinCompanyGroup(int companyId)
        {
            if (companyId > 0)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"company_{companyId}");
            }
        }

        /// <summary>
        /// Firmanın bildirim kanalından ayrılması
        /// </summary>
        public async Task LeaveCompanyGroup(int companyId)
        {
            if (companyId > 0)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"company_{companyId}");
            }
        }

        /// <summary>
        /// Belirli bir satıcının profil sayfasını inceleyen kullanıcıların canlı değerlendirme kanalına katılması
        /// </summary>
        public async Task JoinSellerChannel(int sellerId)
        {
            if (sellerId > 0)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"seller_channel_{sellerId}");
            }
        }

        /// <summary>
        /// Satıcı kanalından ayrılma
        /// </summary>
        public async Task LeaveSellerChannel(int sellerId)
        {
            if (sellerId > 0)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"seller_channel_{sellerId}");
            }
        }
    }
}
