using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace satinalmaportali2.Hubs
{
    /// <summary>
    /// Satın Alma Portalı Merkezi Gerçek Zamanlı (SignalR) Hub'ı.
    /// Talepler, teklifler, siparişler, fiyat güncellemeleri ve bildirimlerin canlı senkronizasyonunu yönetir.
    /// </summary>
    public class PortalHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var user = Context.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                // Kullanıcının Firma ID'sini bul ve firmaya özel kanala otomatik kaydet
                var companyIdClaim = user.FindFirst("CompanyID")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(companyIdClaim))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"company_{companyIdClaim}");
                }

                // Rolü admin ise admin grubuna da ekle
                var userType = user.FindFirst("UserType")?.Value ?? user.FindFirst(ClaimTypes.Role)?.Value;
                var compTypeClaim = user.FindFirst("CompanyTypeID")?.Value;
                if (string.Equals(userType, "admin", StringComparison.OrdinalIgnoreCase) || compTypeClaim == "4")
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "role_admin");
                }
            }

            // Genel (Broadcast) gruba ekle
            await Groups.AddToGroupAsync(Context.ConnectionId, "group_global");

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// İstemcinin belirli bir firma grubuna manuel katılması (Guest veya dinamik geçişler için).
        /// </summary>
        public async Task JoinCompanyGroup(int companyId)
        {
            if (companyId > 0)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"company_{companyId}");
            }
        }

        /// <summary>
        /// İstemcinin belirli bir firmanın grubundan ayrılması.
        /// </summary>
        public async Task LeaveCompanyGroup(int companyId)
        {
            if (companyId > 0)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"company_{companyId}");
            }
        }

        /// <summary>
        /// Bir talep detay veya kıyaslama modalı açıldığında, o talebe ait anlık teklif güncellemelerini dinlemek için gruba katılım.
        /// </summary>
        public async Task JoinRequestGroup(int requestId)
        {
            if (requestId > 0)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"request_{requestId}");
            }
        }

        /// <summary>
        /// Talep detay veya kıyaslama modalı kapatıldığında gruptan ayrılma.
        /// </summary>
        public async Task LeaveRequestGroup(int requestId)
        {
            if (requestId > 0)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"request_{requestId}");
            }
        }
    }
}
