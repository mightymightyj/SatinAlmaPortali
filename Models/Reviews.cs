using System.ComponentModel.DataAnnotations;

namespace satinalmaportali2.Models
{
    public class Reviews
    {
        [Key]
        public int? ReviewID { get; set; }
        public int? RequestID { get; set; }
        public float? ReviewScore { get; set; }
        public int? CompanyID { get; set; }
        public string? ReviewText { get; set; }
    }
}