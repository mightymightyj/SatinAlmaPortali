using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace satinalmaportali2.Models
{
    public class ItemsModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ItemID { get; set; }
        public string? ItemName { get; set; }
        public double? ItemPrice { get; set; }
        public int? Quantity { get; set; }
        public int? CategoryID { get; set; }
        public string? Properties { get; set; }
        public string? ItemDescryption { get; set; }
        public string? Brand { get; set; }

        public virtual ICollection<ItemImage> Images { get; set; } = new List<ItemImage>();
    }
}