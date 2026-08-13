using System.ComponentModel.DataAnnotations;

namespace satinalmaportali2.Models
{
    public class Category1
    {
        [Key]
        public int? Category1ID { get; set; }
        public string? Category1Name { get; set; }
    }
}