using System.ComponentModel.DataAnnotations;

namespace satinalmaportali2.Models
{
    public class Category2
    {
        [Key]
        public int? Category2ID { get; set; }
        public string? Category2Name { get; set; }
    }
}