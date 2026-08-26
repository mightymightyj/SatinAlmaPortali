using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace satinalmaportali2.Models
{
    public class RequestOffers
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OfferID { get; set; }

        public int RequestID { get; set; }

        public int SupplierID { get; set; }

        public decimal OfferedPrice { get; set; }

        public int DeliveryDays { get; set; } = 7;

        public int ValidityDays { get; set; } = 30;

        public DateTime OfferDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Durum: 'Beklemede', 'Kabul Edildi', 'Reddedildi'
        /// </summary>
        public string Status { get; set; } = "Beklemede";

        public string? SupplierNotes { get; set; }

        // Navigation Properties
        [ForeignKey("RequestID")]
        public virtual Requests? Request { get; set; }

        [ForeignKey("SupplierID")]
        public virtual Company? Supplier { get; set; }

        public virtual ICollection<RequestOfferItem> OfferItems { get; set; } = new List<RequestOfferItem>();
    }
}
