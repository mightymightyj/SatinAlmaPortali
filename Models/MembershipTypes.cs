using System.ComponentModel.DataAnnotations;

namespace satinalmaportali2.Models
{
    public class MembershipTypes
    {
        [Key]
        public int? MembershipTypeID { get; set; }
        public string? MembershipTypeName { get; set; }
    }
}