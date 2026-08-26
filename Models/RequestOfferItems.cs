using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace satinalmaportali2.Models
{
    [Table("request_offer_items")]
    public class RequestOfferItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OfferItemID { get; set; }

        public int OfferID { get; set; }

        public int RequestItemID { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }

        public string? Notes { get; set; }

        // Navigation Properties
        [ForeignKey("OfferID")]
        public virtual RequestOffers? Offer { get; set; }

        [ForeignKey("RequestItemID")]
        public virtual RequestItem? RequestItem { get; set; }
    }
}
