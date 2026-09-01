using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace satinalmaportali2.Models
{
    public class Company
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CompanyID { get; set; }

        public int? CompanyTypeID { get; set; }

        public int? MembershipTypeID { get; set; }

        public string? CompanyName { get; set; }

        public string? CompanyOfficial { get; set; }

        public string? CompanyPhone { get; set; }

        public string? CompanyMail { get; set; }

        public string? TCNO { get; set; }

        public string? TaxOffice { get; set; }

        public string? CompanyDetailText { get; set; }

        public string? CompanyLogo { get; set; }

        public int? AddressID { get; set; }

        public string? CompanyPassword { get; set; }

        public string? ResetToken { get; set; }

        public DateTime? ResetTokenExpiry { get; set; }

        // iyzico Pazaryeri / Alt Üye İşyeri (Sub-Merchant) Alanları
        public string? IyzicoSubMerchantKey { get; set; }

        public string? SubMerchantType { get; set; } // LIMITED_OR_JOINT_STOCK_COMPANY, PERSONAL_COMPANY, PRIVATE_COMPANY

        public string? IBAN { get; set; }

        public string? LegalCompanyTitle { get; set; }

        public string? IdentityNumber { get; set; }

        public bool IsIyzicoApproved { get; set; } = false;
    }
}