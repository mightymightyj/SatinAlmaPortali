using System.ComponentModel.DataAnnotations;

namespace satinalmaportali2.Models
{
    public class SuperAdminDashboardViewModel
    {
        public SuperAdminStatsDto Stats { get; set; } = new();
        public List<CategoryAdminDto> Categories { get; set; } = new();
        public List<Category> AvailableParentCategories { get; set; } = new();
        public List<CompanyAdminDto> Companies { get; set; } = new();
        public List<CompanyTypes> AvailableCompanyTypes { get; set; } = new();
        public List<MembershipTypes> AvailableMembershipTypes { get; set; } = new();
        public List<RequestSummaryDto> OpenRequests { get; set; } = new();

        public string ActiveTab { get; set; } = "overview"; // overview, categories, companies, simulation, settings
        public string? SearchQuery { get; set; }
        public int? FilterCompanyType { get; set; }
    }

    public class SuperAdminStatsDto
    {
        public int TotalCompanies { get; set; }
        public int SupplierCount { get; set; }
        public int BuyerCount { get; set; }
        public int BothCount { get; set; }
        public int SuperAdminCount { get; set; }

        public int TotalCategories { get; set; }
        public int RootCategoriesCount { get; set; }
        public int SubCategoriesCount { get; set; }

        public int TotalRequests { get; set; }
        public int TotalOffers { get; set; }
        public decimal TotalEstimatedBudget { get; set; }
        public decimal TotalOfferedVolume { get; set; }
    }

    public class CategoryAdminDto
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int? ParentCategoryID { get; set; }
        public string? ParentCategoryName { get; set; }
        public int SubCategoryCount { get; set; }
        public int LinkedRequestsCount { get; set; }
        public bool IsRoot => !ParentCategoryID.HasValue || ParentCategoryID.Value == 0;
    }

    public class CompanyAdminDto
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyOfficial { get; set; } = string.Empty;
        public string CompanyMail { get; set; } = string.Empty;
        public string? CompanyPhone { get; set; }
        public string? TCNO { get; set; }
        public string? TaxOffice { get; set; }
        public int? CompanyTypeID { get; set; }
        public string CompanyTypeName { get; set; } = "Belirtilmemiş";
        public int? MembershipTypeID { get; set; }
        public string MembershipTypeName { get; set; } = "Standart";
        public string? CompanyLogo { get; set; }
        public string? CompanyDetailText { get; set; }
        public string? AddressText { get; set; }
        public int RequestCount { get; set; }
        public int OfferCount { get; set; }
        public bool IsSuperAdmin => CompanyTypeID == 4;
    }

    public class RequestSummaryDto
    {
        public int RequestID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string BuyerInfo { get; set; } = string.Empty;
        public int? SenderID { get; set; }
        public string Status { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal? EstimatedBudget { get; set; }
        public int OfferCount { get; set; }
        public DateTime? RequestDate { get; set; }
    }

    public class EditCategoryInputModel
    {
        public int CategoryID { get; set; }
        [Required(ErrorMessage = "Kategori adı zorunludur.")]
        public string CategoryName { get; set; } = string.Empty;
        public int? ParentCategoryID { get; set; }
    }

    public class EditCompanyInputModel
    {
        public int CompanyID { get; set; }
        [Required(ErrorMessage = "Firma adı zorunludur.")]
        public string CompanyName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Yetkili adı zorunludur.")]
        public string CompanyOfficial { get; set; } = string.Empty;
        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string CompanyMail { get; set; } = string.Empty;
        public string? CompanyPhone { get; set; }
        public string? TCNO { get; set; }
        public string? TaxOffice { get; set; }
        public int? CompanyTypeID { get; set; }
        public int? MembershipTypeID { get; set; }
        public string? CompanyDetailText { get; set; }
        public string? AddressText { get; set; }
    }

    public class ResetPasswordInputModel
    {
        public int CompanyID { get; set; }
        [Required(ErrorMessage = "Yeni şifre zorunludur.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class SimulateRequestInputModel
    {
        [Required(ErrorMessage = "Lütfen talebi oluşturan firmayı seçin.")]
        public int SenderCompanyID { get; set; }

        [Required(ErrorMessage = "Ürün veya hizmet adı zorunludur.")]
        public string ProductName { get; set; } = string.Empty;

        [Range(1, 1000000, ErrorMessage = "Adet en az 1 olmalıdır.")]
        public int Quantity { get; set; } = 1;

        public string? RequestType { get; set; } = "Satın Alma";

        public int? CategoryID { get; set; }

        public decimal? EstimatedBudget { get; set; }

        public DateTime? TargetDeliveryDate { get; set; }

        public string? Notes { get; set; }
    }

    public class SimulateOfferInputModel
    {
        [Required(ErrorMessage = "Lütfen teklif verilecek talebi seçin.")]
        public int RequestID { get; set; }

        [Required(ErrorMessage = "Lütfen teklif veren tedarikçi firmayı seçin.")]
        public int SupplierCompanyID { get; set; }

        [Required(ErrorMessage = "Teklif tutarı zorunludur.")]
        [Range(0.01, 100000000, ErrorMessage = "Lütfen geçerli bir teklif tutarı giriniz.")]
        public decimal OfferedPrice { get; set; }

        public int? DeliveryDays { get; set; } = 7;

        public int? ValidityDays { get; set; } = 30;

        public string? SupplierNotes { get; set; }
    }
}
