using System.Collections.Generic;

namespace satinalmaportali2.Models
{
    public class ProfileViewModel
    {
        // Company Info
        public int CompanyID { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyOfficial { get; set; }
        public string? CompanyMail { get; set; }
        public string? CompanyPhone { get; set; }
        public string? TCNO { get; set; }
        public string? TaxOffice { get; set; }
        public string? CompanyDetailText { get; set; }
        public string? CompanyLogo { get; set; }

        // Company Type
        public int? CompanyTypeID { get; set; }
        public string? CompanyTypeName { get; set; }

        // Membership Type
        public int? MembershipTypeID { get; set; }
        public string? MembershipTypeName { get; set; }

        // Address Info
        public int? AddressID { get; set; }
        public string? AddressText { get; set; }

        // Location Hierarchy
        public int? DistrictID { get; set; }
        public string? DistrictName { get; set; }

        public int? TownID { get; set; }
        public string? TownName { get; set; }

        public int? CountryID { get; set; }
        public string? CountryName { get; set; }

        // Dropdown Lists
        public List<CompanyTypes>? AvailableCompanyTypes { get; set; }
    }
}
