using System;
using System.Collections.Generic;

namespace satinalmaportali2.Models
{
    public class PricingPageViewModel
    {
        public bool IsAuthenticated { get; set; }
        public int CurrentCompanyId { get; set; }
        public string CurrentCompanyName { get; set; } = string.Empty;
        public string CurrentCompanyEmail { get; set; } = string.Empty;
        public int CurrentMembershipTypeId { get; set; } = 1;
        public string CurrentMembershipTypeName { get; set; } = "Bedava";
        public List<MembershipPlanDto> Plans { get; set; } = new();
    }

    public class MembershipPlanDto
    {
        public int TierId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Badge { get; set; } = string.Empty;
        public decimal PriceMonthly { get; set; }
        public string Description { get; set; } = string.Empty;
        public int MaxRequests { get; set; }
        public int MaxProducts { get; set; }
        public int MaxImagesPerProduct { get; set; }
        public List<string> Highlights { get; set; } = new();
        public bool IsCurrentPlan { get; set; }
    }

    public class PurchaseMembershipInputModel
    {
        public int TargetMembershipTypeId { get; set; } // 2 (Pro) veya 3 (Kurumsal)
        public CardPaymentInputDto? CardInfo { get; set; }
    }
}
