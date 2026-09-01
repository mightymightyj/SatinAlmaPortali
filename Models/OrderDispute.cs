using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace satinalmaportali2.Models
{
    public class OrderDispute
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DisputeID { get; set; }

        public int OrderID { get; set; }

        public int BuyerCompanyID { get; set; }

        public int? SupplierCompanyID { get; set; }

        [Required]
        [MaxLength(100)]
        public string Reason { get; set; } = string.Empty; // Ayıplı Ürün, Yanlış Ürün, Eksik Teslimat, Teslim Edilmedi, Diğer

        [Required]
        public string Description { get; set; } = string.Empty;

        public string? EvidenceAttachmentUrl { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Open"; // Open, UnderReview, Approved_Refund, Rejected, Resolved

        public string? AdminNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ResolvedAt { get; set; }

        [ForeignKey("OrderID")]
        public virtual Order? Order { get; set; }

        [ForeignKey("BuyerCompanyID")]
        public virtual Company? BuyerCompany { get; set; }

        [ForeignKey("SupplierCompanyID")]
        public virtual Company? SupplierCompany { get; set; }
    }
}
