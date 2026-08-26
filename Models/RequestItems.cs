using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace satinalmaportali2.Models
{
    [Table("request_items")]
    public class RequestItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestItemID { get; set; }

        public int RequestID { get; set; }

        public int ItemOrder { get; set; } = 1;

        [Required]
        [MaxLength(255)]
        public string ItemName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; } = 1;

        [MaxLength(50)]
        public string Unit { get; set; } = "Adet";

        [Column(TypeName = "decimal(18,2)")]
        public decimal? EstimatedUnitPrice { get; set; }

        public string? ItemDescription { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property
        [ForeignKey("RequestID")]
        public virtual Requests? Request { get; set; }
    }
}
