using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace satinalmaportali2.Models
{
    public class CreateRequestInputModel
    {
        [Required(ErrorMessage = "Alıcı bilgisi girilmesi zorunludur.")]
        [Display(Name = "Alıcı Bilgileri")]
        public string BuyerInfo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Aranan ürün adı girilmesi zorunludur.")]
        [Display(Name = "Aranan Ürün")]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Talep adedi girilmesi zorunludur.")]
        [Range(1, 1000000, ErrorMessage = "Lütfen 1 veya daha büyük geçerli bir adet giriniz.")]
        [Display(Name = "Talep Edilen Adet")]
        public int Quantity { get; set; } = 1;

        [Display(Name = "Talep Tipi")]
        public string RequestType { get; set; } = "Satın Alma";

        [Display(Name = "Açıklama / Notlar")]
        public string? Notes { get; set; }
    }

    public class RequestListItemViewModel
    {
        public int RequestID { get; set; }
        public DateTime RequestDate { get; set; }
        public string BuyerInfo { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string RequestType { get; set; } = "Satın Alma";
        public string Status { get; set; } = "Beklemede";
        public string? Notes { get; set; }
    }

    public class RequestListViewModel
    {
        public List<RequestListItemViewModel> Requests { get; set; } = new();
        public CreateRequestInputModel NewRequest { get; set; } = new();

        public int TotalRequestsCount => Requests.Count;
        public int PendingRequestsCount => Requests.Count(r => r.Status == "Beklemede");
        public int ApprovedRequestsCount => Requests.Count(r => r.Status == "Onaylandı" || r.Status == "Tamamlandı");
        public int TotalQuantityCount => Requests.Sum(r => r.Quantity);

        public string? SearchQuery { get; set; }
        public string? SelectedType { get; set; }
        public string? SelectedStatus { get; set; }
    }

    public class SubmitOfferInput
    {
        public int? RequestId { get; set; }
        public string? RfqCode { get; set; }
        public string? ProductName { get; set; }
        public string? ProductImageUrl { get; set; }
        public string? BuyerEmail { get; set; }
        public int? Quantity { get; set; } = 1;
        public string? Unit { get; set; } = "Adet";
        public decimal OfferAmount { get; set; }
        public int? DeliveryDays { get; set; } = 7;
        public int? ValidityDays { get; set; } = 30;
        public string? Notes { get; set; }
    }
}
