using System;
using System.ComponentModel.DataAnnotations;

namespace satinalmaportali2.Models
{
    public class Requests
    {
        [Key]
        public int? RequestID { get; set; }
        public DateTime? RequestDate { get; set; }
        public int? SenderID { get; set; }
        public int? RecieverID { get; set; }
        public string? RequestType { get; set; }
        public string? BuyerInfo { get; set; }
        public string? ProductName { get; set; }
        public int? Quantity { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
    }
}