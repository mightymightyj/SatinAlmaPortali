using System;
using System.Collections.Generic;

namespace satinalmaportali2.Models
{
    /// <summary>
    /// Alıcının satın aldığı sipariş için satıcıyı 10 yıldız üzerinden değerlendirme girdi modeli.
    /// </summary>
    public class SubmitBuyerReviewInputModel
    {
        public int OrderID { get; set; }

        /// <summary>
        /// 1.0 ile 10.0 arasında genel yıldız puanı
        /// </summary>
        public double ReviewScore { get; set; }

        public double? DeliveryScore { get; set; }
        public double? QualityScore { get; set; }
        public double? CommunicationScore { get; set; }

        public string? ReviewText { get; set; }
    }

    /// <summary>
    /// Satıcının sipariş veren alıcıyı 10 yıldız üzerinden değerlendirme girdi modeli.
    /// </summary>
    public class SubmitSellerReviewInputModel
    {
        public int OrderID { get; set; }

        /// <summary>
        /// 1.0 ile 10.0 arasında genel yıldız puanı
        /// </summary>
        public double ReviewScore { get; set; }

        public double? PaymentPunctualityScore { get; set; }
        public double? CommunicationScore { get; set; }

        public string? ReviewText { get; set; }
    }

    /// <summary>
    /// Satıcı Profili ve 10 Yıldızlı Değerlendirme Sayfası Görünüm Modeli
    /// </summary>
    public class SellerProfileViewModel
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyOfficial { get; set; }
        public string? CompanyLogo { get; set; }
        public string? CompanyMail { get; set; }
        public string? CompanyPhone { get; set; }
        public string? CityName { get; set; }
        public string? DistrictName { get; set; }
        public string? AddressText { get; set; }
        public string? CompanyDetailText { get; set; }
        public string MembershipTypeName { get; set; } = "Kurumsal Onaylı Tedarikçi";
        public string MemberSince { get; set; } = "2024";

        // 10 Yıldız İstatistikleri
        public double OverallScore { get; set; } = 9.8;
        public int TotalReviews { get; set; }
        public double DeliveryScoreAvg { get; set; } = 9.7;
        public double QualityScoreAvg { get; set; } = 9.9;
        public double CommunicationScoreAvg { get; set; } = 9.8;

        // 10'dan 1'e yıldız dağılımı (Anahtar: Yıldız 10-1, Değer: Adet)
        public Dictionary<int, int> StarCounts { get; set; } = new();
        public Dictionary<int, double> StarPercentages { get; set; } = new();

        public List<ReviewDetailItemDto> Reviews { get; set; } = new();
        public List<SellerProductItemDto> Products { get; set; } = new();

        // Mevcut oturum açan kullanıcı bu satıcıdan ürün satın almış mı?
        public bool HasUserPurchasedFromSeller { get; set; }
        public int? EligibleOrderID { get; set; }
    }

    public class ReviewDetailItemDto
    {
        public int ReviewID { get; set; }
        public double ReviewScore { get; set; }
        public double? DeliveryScore { get; set; }
        public double? QualityScore { get; set; }
        public double? CommunicationScore { get; set; }
        public string? ReviewText { get; set; }
        public DateTime ReviewDate { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public string ReviewerInitials { get; set; } = "AL";
        public bool IsVerifiedPurchase { get; set; } = true;
        public string? ProductName { get; set; }
        public string? OrderNumber { get; set; }
        public string ReviewType { get; set; } = "BuyerToSeller";
    }

    public class SellerProductItemDto
    {
        public int ItemID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string CategoryName { get; set; } = "Endüstriyel";
        public decimal UnitPrice { get; set; }
        public int Stock { get; set; }
        public string? ImageUrl { get; set; }
        public string? Properties { get; set; }
    }

    /// <summary>
    /// Alıcının satıcılardan aldığı değerlendirmeleri tutan özet DTO
    /// </summary>
    public class BuyerReviewSummaryDto
    {
        public int BuyerCompanyID { get; set; }
        public string BuyerCompanyName { get; set; } = string.Empty;
        public double AverageScore { get; set; } = 10.0;
        public int TotalReviews { get; set; }
        public double PaymentScoreAvg { get; set; } = 10.0;
        public double CommunicationScoreAvg { get; set; } = 10.0;
        public List<ReviewDetailItemDto> Reviews { get; set; } = new();
    }
}
