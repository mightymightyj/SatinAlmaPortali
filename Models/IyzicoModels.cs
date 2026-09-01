using System;
using System.Collections.Generic;

namespace satinalmaportali2.Models
{
    public class IyzicoSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://sandbox-api.iyzipay.com";
        public string CallbackUrl { get; set; } = "https://localhost:7019/Payment/IyzicoCallback";
        public decimal DefaultCommissionRate { get; set; } = 0.05m;
        public int AutoApprovalDays { get; set; } = 7;
    }

    public class SubMerchantRegisterDto
    {
        public int CompanyID { get; set; }
        public string SubMerchantType { get; set; } = "LIMITED_OR_JOINT_STOCK_COMPANY"; // LIMITED_OR_JOINT_STOCK_COMPANY, PERSONAL_COMPANY, PRIVATE_COMPANY
        public string LegalCompanyTitle { get; set; } = string.Empty;
        public string IdentityNumber { get; set; } = string.Empty; // VKN (10 hane) veya TCKN (11 hane)
        public string TaxOffice { get; set; } = string.Empty;
        public string IBAN { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string ContactSurname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = "İstanbul";
    }

    public class CardPaymentInputDto
    {
        public string CardHolderName { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string ExpireMonth { get; set; } = string.Empty;
        public string ExpireYear { get; set; } = string.Empty;
        public string Cvc { get; set; } = string.Empty;
    }

    public class Iyzico3DCallbackInputDto
    {
        public string? status { get; set; }
        public string? paymentId { get; set; }
        public string? conversationData { get; set; }
        public string? conversationId { get; set; }
        public string? mdStatus { get; set; }
    }

    public class EscrowActionInputDto
    {
        public int OrderID { get; set; }
        public string? Reason { get; set; }
        public string? Note { get; set; }
    }

    public class DisputeCreateInputDto
    {
        public int OrderID { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? EvidenceUrl { get; set; }
    }

    public class IyzicoPaymentResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? HtmlContent { get; set; } // 3D Secure HTML formu
        public string? PaymentId { get; set; }
        public string? PaymentTransactionId { get; set; }
        public string? ConversationId { get; set; }
        public string? OrderNumber { get; set; }
        public int? OrderID { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal SupplierPayoutAmount { get; set; }
    }
}
