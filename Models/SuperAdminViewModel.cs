using System.ComponentModel.DataAnnotations;

namespace satinalmaportali2.Models
{
    public class SuperAdminDashboardViewModel
    {
        public SuperAdminStatsDto Stats { get; set; } = new();
        public List<CategoryAdminDto> Categories { get; set; } = new();
        public List<CategoryDropdownDto> AvailableParentCategories { get; set; } = new();
        public List<CompanyAdminDto> Companies { get; set; } = new();
        public List<CompanyTypes> AvailableCompanyTypes { get; set; } = new();
        public List<MembershipTypes> AvailableMembershipTypes { get; set; } = new();
        public List<RequestSummaryDto> OpenRequests { get; set; } = new();
        public List<ItemSummaryDto> RecentListings { get; set; } = new();
        public List<ReviewAdminDto> Reviews { get; set; } = new();
        public List<OrderSummaryDto> AvailableOrders { get; set; } = new();

        public string ActiveTab { get; set; } = "overview"; // overview, categories, companies, reviews, simulation, settings
        public string? SearchQuery { get; set; }
        public int? FilterCompanyType { get; set; }
    }

    public class SuperAdminStatsDto
    {
        public int TotalCompanies { get; set; }
        public int CorporateCompaniesCount { get; set; }
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

        public int TotalListings { get; set; }
        public decimal TotalListingValue { get; set; }

        public int TotalReviews { get; set; }
        public double AveragePlatformScore { get; set; } = 10.0;
        public int BuyerToSellerReviewsCount { get; set; }
        public int SellerToBuyerReviewsCount { get; set; }
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
        public List<CategoryAdminDto> SubCategories { get; set; } = new();
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

        public string? ProductName { get; set; }

        public int Quantity { get; set; } = 1;

        public string? RequestType { get; set; } = "Satın Alma";

        public int? CategoryID { get; set; }

        public decimal? EstimatedBudget { get; set; }

        public DateTime? TargetDeliveryDate { get; set; }

        public string? Notes { get; set; }

        public List<SimulateRequestItemDto>? Items { get; set; } = new();
    }

    public class SimulateRequestItemDto
    {
        public string ItemName { get; set; } = string.Empty;
        public decimal Quantity { get; set; } = 1;
        public string Unit { get; set; } = "Adet";
        public decimal? EstimatedUnitPrice { get; set; }
        public string? ItemDescription { get; set; }
    }

    public class SimulateOfferInputModel
    {
        [Required(ErrorMessage = "Lütfen teklif verilecek talebi seçin.")]
        public int RequestID { get; set; }

        [Required(ErrorMessage = "Lütfen teklif veren tedarikçi firmayı seçin.")]
        public int SupplierCompanyID { get; set; }

        public decimal OfferedPrice { get; set; }

        public int? DeliveryDays { get; set; } = 7;

        public int? ValidityDays { get; set; } = 30;

        public string? SupplierNotes { get; set; }

        public List<SimulateOfferItemDto>? Items { get; set; } = new();
    }

    public class SimulateOfferItemDto
    {
        public int RequestItemID { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Notes { get; set; }
    }

    public class CategoryDropdownDto
    {
        public int CategoryID { get; set; }
        public string FormattedName { get; set; } = string.Empty;
        public int Level { get; set; }
    }

    public class SimulateListingInputModel
    {
        [Required(ErrorMessage = "Lütfen ürünü listeleyecek tedarikçi firmayı seçin.")]
        public int SupplierCompanyID { get; set; }

        [Required(ErrorMessage = "Ürün adı zorunludur.")]
        public string ItemName { get; set; } = string.Empty;

        public int? CategoryID { get; set; }

        [Required(ErrorMessage = "Birim satış fiyatı zorunludur.")]
        [Range(0.01, 100000000, ErrorMessage = "Geçerli bir birim fiyat giriniz.")]
        public decimal ItemPrice { get; set; }

        [Required(ErrorMessage = "Stok adedi zorunludur.")]
        [Range(1, 1000000, ErrorMessage = "Stok adedi en az 1 olmalıdır.")]
        public int Quantity { get; set; } = 100;

        public string? Brand { get; set; }
        public string? Properties { get; set; }
        public string? ItemDescryption { get; set; }
        public string? ImageBase64 { get; set; }
    }

    public class ItemSummaryDto
    {
        public int ItemID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string? Properties { get; set; }
        public string? ItemImage { get; set; }
    }

    public class ReviewAdminDto
    {
        public int ReviewID { get; set; }
        public int? CompanyID { get; set; }
        public string TargetCompanyName { get; set; } = string.Empty;
        public int? ReviewerCompanyID { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public string ReviewType { get; set; } = "BuyerToSeller"; // BuyerToSeller / SellerToBuyer
        public double ReviewScore { get; set; } = 10.0;
        public double? DeliveryScore { get; set; }
        public double? QualityScore { get; set; }
        public double? CommunicationScore { get; set; }
        public string? ReviewText { get; set; }
        public DateTime ReviewDate { get; set; }
        public string? OrderNumber { get; set; }
        public int? OrderID { get; set; }
        public bool IsVerifiedPurchase { get; set; } = true;
    }

    public class OrderSummaryDto
    {
        public int OrderID { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string BuyerCompanyName { get; set; } = string.Empty;
        public string SupplierCompanyName { get; set; } = string.Empty;
        public decimal GrandTotal { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
    }

    public class SimulateReviewInputModel
    {
        [Required(ErrorMessage = "Lütfen değerlendirilecek hedef firmayı seçin.")]
        public int TargetCompanyID { get; set; }

        public int? ReviewerCompanyID { get; set; }

        public string? ReviewerName { get; set; }

        public string ReviewType { get; set; } = "BuyerToSeller"; // "BuyerToSeller" or "SellerToBuyer"

        public int? OrderID { get; set; }

        [Range(1.0, 10.0, ErrorMessage = "Puan 1 ile 10 arasında olmalıdır.")]
        public double ReviewScore { get; set; } = 10.0;

        public double? DeliveryScore { get; set; } = 10.0;
        public double? QualityScore { get; set; } = 10.0;
        public double? CommunicationScore { get; set; } = 10.0;

        public string? ReviewText { get; set; }
        public bool IsVerifiedPurchase { get; set; } = true;
    }
}
