using System.Collections.Generic;

namespace satinalmaportali2.Models
{
    public class ProfileViewModel
    {
        // Company Info
        public int CompanyID { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyOfficial { get; set; }
        public string? CompanyMail { get; set; }
        public string? CompanyPhone { get; set; }
        public string? TCNO { get; set; }
        public string? TaxOffice { get; set; }
        public string? CompanyDetailText { get; set; }
        public string? CompanyLogo { get; set; }

        // Company Type
        public int? CompanyTypeID { get; set; }
        public string? CompanyTypeName { get; set; }

        // Membership Type
        public int? MembershipTypeID { get; set; }
        public string? MembershipTypeName { get; set; }

        // Address Info
        public int? AddressID { get; set; }
        public string? AddressText { get; set; }

        // Location Hierarchy
        public int? DistrictID { get; set; }
        public string? DistrictName { get; set; }

        public int? TownID { get; set; }
        public string? TownName { get; set; }

        public int? CountryID { get; set; }
        public string? CountryName { get; set; }

        // Dropdown Lists
        public List<CompanyTypes>? AvailableCompanyTypes { get; set; }

        // 10 Yıldızlı Firma Değerlendirmeleri & Karne Bilgisi
        public double BuyerScore { get; set; } = 10.0;
        public int BuyerReviewCount { get; set; } = 0;
        public double OverallScore { get; set; } = 10.0;
        public List<ReviewDetailItemDto> ReceivedReviews { get; set; } = new();

        // Siparişlerim & Satışlarım Bölümü
        public string ActiveTab { get; set; } = "profile"; // "profile", "orders", "sales", "purchased-items", "reviews"
        public List<Order> MyOrders { get; set; } = new(); // Verilen Alış Siparişleri
        public List<Order> MySalesOrders { get; set; } = new(); // Gelen Satış Siparişleri (Tedarikçi Olarak)
        public List<PurchasedProductDto> PurchasedProducts { get; set; } = new();
    }

    public class PurchasedProductDto
    {
        public int? ItemID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? CategoryName { get; set; }
        public decimal UnitPrice { get; set; }
        public int TotalQuantityPurchased { get; set; }
        public DateTime LastPurchaseDate { get; set; }
        public string? SupplierCompanyName { get; set; }
        public int? SupplierCompanyID { get; set; }
        public int LastOrderID { get; set; }
        public string? LastOrderNumber { get; set; }
    }
}
