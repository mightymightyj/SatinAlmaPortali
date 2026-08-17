using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace satinalmaportali2.Models
{
    public class Reviews
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReviewID { get; set; }
        public int? RequestID { get; set; }
        public double? ReviewScore { get; set; }
        public int? CompanyID { get; set; }
        public string? ReviewText { get; set; }
    }
}