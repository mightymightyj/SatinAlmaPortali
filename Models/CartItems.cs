using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace satinalmaportali2.Models
{
    public class CartItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CartItemID { get; set; }

        public int CompanyID { get; set; }

        [ForeignKey("CompanyID")]
        public virtual Company? Company { get; set; }

        public int ProductID { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Sku { get; set; }

        public string? Category { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int Quantity { get; set; } = 1;

        public string? Seller { get; set; }

        public string? Image { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class UpdateCartQtyInputModel
    {
        public int ProductID { get; set; }
        public int Quantity { get; set; }
    }

    public class RemoveCartItemInputModel
    {
        public int ProductID { get; set; }
    }
}
