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

        public string ActiveTab { get; set; } = "overview"; // overview, categories, companies, settings
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
}
