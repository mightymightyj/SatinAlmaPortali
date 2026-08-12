using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace satinalmaportali2.Models
{
    public class Addresses
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AddressID { get; set; }
        public string? AddressText { get; set; }
        public int? CompanyID { get; set; }
        public int? RequestID { get; set; }
        public int? DistrictID { get; set; }
    }
}