using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace satinalmaportali2.Models
{
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderID { get; set; }

        [Required]
        [MaxLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        public int? BuyerCompanyID { get; set; }
        public string BuyerCompanyName { get; set; } = string.Empty;
        public string? BuyerTaxNumber { get; set; }
        public string? BuyerTaxOffice { get; set; }
        public string? BuyerEmail { get; set; }
        public string? BuyerPhone { get; set; }

        public int? SupplierCompanyID { get; set; }
        public string? SupplierCompanyName { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime? DeliveryDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; }

        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "Kredi Kartı"; // Kredi Kartı, Havale / EFT, Cari Hesap / Vadeli, DBS

        [MaxLength(50)]
        public string PaymentStatus { get; set; } = "Ödendi"; // Ödendi, Beklemede, Vadeli

        [MaxLength(50)]
        public string OrderStatus { get; set; } = "Onaylandı"; // Onaylandı, Hazırlanıyor, Kargoya Verildi, Teslim Edildi, İptal

        public string? DeliveryCity { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }

        public string? TrackingNumber { get; set; }
        public string? ShippingCarrier { get; set; } // Yurtiçi Kargo, MNG, Aras, Borusan Lojistik, Ambar
        public string? Notes { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
