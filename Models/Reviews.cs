using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace satinalmaportali2.Models
{
    public class Reviews
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReviewID { get; set; }

        public int? RequestID { get; set; }

        public int? OrderID { get; set; }

        public int? OrderItemID { get; set; }

        /// <summary>
        /// Değerlendirilen hedef firma (Satıcı değerlendiriliyorsa Satıcı Firma ID, Alıcı değerlendiriliyorsa Alıcı Firma ID)
        /// </summary>
        public int? CompanyID { get; set; }

        /// <summary>
        /// Değerlendirmeyi yapan firma ID
        /// </summary>
        public int? ReviewerCompanyID { get; set; }

        /// <summary>
        /// Değerlendiren firma veya yetkilinin adı / ünvanı
        /// </summary>
        [MaxLength(255)]
        public string? ReviewerName { get; set; }

        /// <summary>
        /// Değerlendirme türü: "BuyerToSeller" (Alıcı -> Satıcı) veya "SellerToBuyer" (Satıcı -> Alıcı)
        /// </summary>
        [MaxLength(50)]
        public string ReviewType { get; set; } = "BuyerToSeller";

        /// <summary>
        /// 10 Yıldız üzerinden genel puan (1.0 - 10.0)
        /// </summary>
        public double? ReviewScore { get; set; }

        /// <summary>
        /// Teslimat Hızı Puanı (1.0 - 10.0)
        /// </summary>
        public double? DeliveryScore { get; set; }

        /// <summary>
        /// Ürün / Hizmet Kalitesi Puanı (1.0 - 10.0)
        /// </summary>
        public double? QualityScore { get; set; }

        /// <summary>
        /// İletişim ve Süreç Puanı (1.0 - 10.0)
        /// </summary>
        public double? CommunicationScore { get; set; }

        public string? ReviewText { get; set; }

        public DateTime ReviewDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Doğrulanmış satın alma şartı bayrağı (Sipariş doğrulandıktan sonra verilir)
        /// </summary>
        public bool IsVerifiedPurchase { get; set; } = true;

        [ForeignKey("OrderID")]
        public virtual Order? Order { get; set; }

        [ForeignKey("CompanyID")]
        public virtual Company? TargetCompany { get; set; }

        [ForeignKey("ReviewerCompanyID")]
        public virtual Company? ReviewerCompany { get; set; }
    }
}