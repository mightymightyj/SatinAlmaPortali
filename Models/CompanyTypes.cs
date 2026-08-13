using System.ComponentModel.DataAnnotations;

namespace satinalmaportali2.Models
{
    public class CompanyTypes
    {
        [Key]
        public int? CompanyTypeID { get; set; }
        public string? CompanyTypeName { get; set; }
    }
}