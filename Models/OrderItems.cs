using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace satinalmaportali2.Models
{
    public class OrderItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderItemID { get; set; }

        public int OrderID { get; set; }

        [ForeignKey("OrderID")]
        public virtual Order? Order { get; set; }

        public int? ItemID { get; set; }

        [ForeignKey("ItemID")]
        public virtual ItemsModel? Item { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty;

        public string? Sku { get; set; }

        public string? CategoryName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }

        public string? SupplierCompanyName { get; set; }
        public int? SupplierCompanyID { get; set; }
    }
}
