using System.ComponentModel.DataAnnotations;

namespace satinalmaportali2.Models
{
    public class ItemsModel
    {
        [Key]
        public int? ItemID { get; set; }
        public string? ItemDescryption { get; set; }
        public float? ItemPrice { get; set; }
        public string? Properties { get; set; }
        public string? ItemName { get; set; }
        public int? Quantity { get; set; }
        public string? Brand { get; set; }
        public int? Category1ID { get; set; }
        public int? Category2ID { get; set; }
    }
}