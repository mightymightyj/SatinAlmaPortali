using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace satinalmaportali2.Models
{
    public class PaymentTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TransactionID { get; set; }

        public int? OrderID { get; set; }

        [MaxLength(100)]
        public string ConversationId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? PaymentId { get; set; }

        [MaxLength(100)]
        public string? PaymentTransactionId { get; set; }

        [MaxLength(100)]
        public string? SubMerchantKey { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MerchantPayoutAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PlatformCommission { get; set; }

        [MaxLength(50)]
        public string TransactionType { get; set; } = "Initialize"; // Initialize, Auth_3D, Escrow_Blocked, Escrow_Released, Refund, Disapproval

        [MaxLength(50)]
        public string Status { get; set; } = "Success"; // Success, Failure, Pending

        public string? ErrorCode { get; set; }

        public string? ErrorMessage { get; set; }

        public string? RawResponse { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("OrderID")]
        public virtual Order? Order { get; set; }
    }
}
