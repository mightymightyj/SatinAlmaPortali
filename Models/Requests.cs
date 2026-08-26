using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace satinalmaportali2.Models
{
    public class Requests
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestID { get; set; }
        public DateTime? RequestDate { get; set; }
        public int? SenderID { get; set; }
        public int? RecieverID { get; set; }
        public string? RequestType { get; set; }
        public string? ReceiverName { get; set; }
        public string? BuyerInfo { get; set; }
        public string? ProductName { get; set; }
        public int? Quantity { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }

        public decimal? EstimatedBudget { get; set; }
        public DateTime? TargetDeliveryDate { get; set; }
        public int? CategoryID { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? RejectionReason { get; set; }

        // Navigation Properties
        public virtual ICollection<RequestOffers> Offers { get; set; } = new List<RequestOffers>();
        public virtual ICollection<RequestItem> Items { get; set; } = new List<RequestItem>();
    }
}