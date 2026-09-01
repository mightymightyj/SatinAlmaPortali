using System.Threading.Tasks;
using Iyzipay;
using satinalmaportali2.Models;

namespace satinalmaportali2.Services
{
    public interface IIyzicoPaymentService
    {
        Options GetOptions();
        IyzicoSettings GetSettings();

        /// <summary>
        /// Tedarikçi için iyzico Alt Üye İşyeri (Sub-Merchant) oluşturur veya günceller.
        /// </summary>
        Task<(bool Success, string? SubMerchantKey, string Message)> CreateOrUpdateSubMerchantAsync(SubMerchantRegisterDto dto);

        /// <summary>
        /// Sipariş için 3D Secure ödeme başlatır ve tutarı iyzico havuzunda bloke eder.
        /// </summary>
        Task<IyzicoPaymentResultDto> Initialize3DCheckoutAsync(Order order, CardPaymentInputDto cardInfo, string ipAddress, string? callbackUrl = null);

        /// <summary>
        /// 3D Secure callback yanıtını doğrular ve havuz ödeme kaydını oluşturur.
        /// </summary>
        Task<IyzicoPaymentResultDto> Complete3DPaymentAsync(string conversationId, string paymentId, string? conversationData);

        /// <summary>
        /// Alıcı veya Admin onayı ile havuzdaki bloke parayı çözer ve net tutarı tedarikçiye aktarır.
        /// </summary>
        Task<(bool Success, string Message)> ApproveEscrowReleaseAsync(int orderId, int currentCompanyId, bool isAdmin = false);

        /// <summary>
        /// İade/İptal durumunda havuzdaki bloke tutarı alıcının kartına geri iade eder.
        /// </summary>
        Task<(bool Success, string Message)> DisapproveEscrowRefundAsync(int orderId, int currentCompanyId, string reason, bool isAdmin = false);

        /// <summary>
        /// Alıcı firma tarafından itiraz/iade talebi açılmasını sağlar.
        /// </summary>
        Task<(bool Success, string Message)> CreateDisputeAsync(DisputeCreateInputDto dto, int buyerCompanyId);
    }
}
