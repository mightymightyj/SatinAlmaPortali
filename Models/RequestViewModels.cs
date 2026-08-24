using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace satinalmaportali2.Models
{
    public class CreateRequestInputModel
    {
        [Display(Name = "Alıcı Bilgileri")]
        public string? BuyerInfo { get; set; }

        [Required(ErrorMessage = "Aranan ürün veya hizmet adı girilmesi zorunludur.")]
        [Display(Name = "Aranan Ürün / Hizmet")]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Talep adedi girilmesi zorunludur.")]
        [Range(1, 10000000, ErrorMessage = "Lütfen 1 veya daha büyük geçerli bir miktar giriniz.")]
        [Display(Name = "Talep Edilen Miktar")]
        public int Quantity { get; set; } = 1;

        [Display(Name = "Talep Tipi / Kategori")]
        public string RequestType { get; set; } = "Satın Alma";

        public int? CategoryID { get; set; }

        [Display(Name = "Tahmini Bütçe (₺)")]
        [Range(0, 1000000000, ErrorMessage = "Geçerli bir bütçe tutarı giriniz.")]
        public decimal? EstimatedBudget { get; set; }

        [Display(Name = "Hedef Teslim / Son Teklif Tarihi")]
        public DateTime? TargetDeliveryDate { get; set; }

        [Display(Name = "Teknik Şartname & Açıklama")]
        public string? Notes { get; set; }
    }

    public class SubmitOfferInputModel
    {
        [Required(ErrorMessage = "Talep numarası zorunludur.")]
        public int RequestID { get; set; }

        [Required(ErrorMessage = "Teklif fiyatı girilmesi zorunludur.")]
        [Range(0.01, 1000000000, ErrorMessage = "Lütfen geçerli bir teklif tutarı giriniz.")]
        [Display(Name = "Toplam Teklif Tutarı (₺)")]
        public decimal OfferedPrice { get; set; }

        [Required(ErrorMessage = "Teslimat süresi girilmesi zorunludur.")]
        [Range(1, 365, ErrorMessage = "Teslimat süresi 1-365 gün arasında olmalıdır.")]
        [Display(Name = "Teslimat Süresi (Gün)")]
        public int DeliveryDays { get; set; } = 7;

        [Display(Name = "Teklif Geçerlilik Süresi (Gün)")]
        public int ValidityDays { get; set; } = 30;

        [Display(Name = "Tedarikçi Notları / Şartlar")]
        public string? SupplierNotes { get; set; }
    }

    public class RequestOfferItemViewModel
    {
        public int OfferID { get; set; }
        public int RequestID { get; set; }
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierOfficial { get; set; } = string.Empty;
        public string? SupplierPhone { get; set; }
        public string? SupplierMail { get; set; }
        public decimal OfferedPrice { get; set; }
        public int DeliveryDays { get; set; }
        public int ValidityDays { get; set; }
        public DateTime OfferDate { get; set; }
        public string Status { get; set; } = "Beklemede";
        public string? SupplierNotes { get; set; }
        public string? RejectionReason { get; set; }
        public bool IsRevised { get; set; }
        public decimal? PreviousPrice { get; set; }
        public string? PreviousRejectionReason { get; set; }
        public bool IsAccepted => Status == "Kabul Edildi";
        public bool IsRejected => Status == "Reddedildi";
    }

    public class RequestListItemViewModel
    {
        public int RequestID { get; set; }
        public DateTime RequestDate { get; set; }
        public int? SenderID { get; set; }
        public int? RecieverID { get; set; }
        public string BuyerInfo { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string RequestType { get; set; } = "Satın Alma";
        public string Status { get; set; } = "Beklemede";
        public string? Notes { get; set; }

        public decimal? EstimatedBudget { get; set; }
        public DateTime? TargetDeliveryDate { get; set; }
        public int? CategoryID { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? RejectionReason { get; set; }

        public int OfferCount { get; set; }
        public decimal? BestOfferPrice { get; set; }
        public bool IsOwner { get; set; }
        public bool HasMyOffer { get; set; }
        public decimal? MyOfferPrice { get; set; }
        public int? MyOfferDeliveryDays { get; set; }
        public string? MyOfferNotes { get; set; }
        public string? MyOfferStatus { get; set; }
        public string? MyOfferRejectionReason { get; set; }
        public bool HasRevisedOffers { get; set; }
        public int RevisedOfferCount { get; set; }
    }

    public class RequestDetailViewModel
    {
        public int RequestID { get; set; }
        public DateTime RequestDate { get; set; }
        public int? SenderID { get; set; }
        public int? RecieverID { get; set; }
        public string BuyerInfo { get; set; } = string.Empty;
        public string? SenderOfficial { get; set; }
        public string? SenderMail { get; set; }
        public string? SenderPhone { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string RequestType { get; set; } = "Satın Alma";
        public string Status { get; set; } = "Beklemede";
        public string? Notes { get; set; }
        public decimal? EstimatedBudget { get; set; }
        public DateTime? TargetDeliveryDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? RejectionReason { get; set; }

        public bool IsOwner { get; set; }
        public bool IsSupplier { get; set; }
        public int CurrentCompanyID { get; set; }

        public List<RequestOfferItemViewModel> Offers { get; set; } = new();
        public SubmitOfferInputModel NewOffer { get; set; } = new();
    }

    public class RequestListViewModel
    {
        public List<RequestListItemViewModel> Requests { get; set; } = new();
        public CreateRequestInputModel NewRequest { get; set; } = new();
        public SubmitOfferInputModel NewOffer { get; set; } = new();

        // Sekme Bazlı Gerçek Sayılar (Tab filtresinden bağımsız doğru sayılar)
        public int AllRequestsCount { get; set; }
        public int MyRequestsCount { get; set; }
        public int IncomingOffersCount { get; set; }
        public int MyBidsCount { get; set; }

        // KPI Göstergeleri (Genel / Pazar Özeti)
        public int TotalRequestsCount { get; set; }
        public int PendingRequestsCount { get; set; }
        public int ApprovedRequestsCount { get; set; }
        public int TotalQuantityCount { get; set; }

        public string? SearchQuery { get; set; }
        public string? SelectedType { get; set; }
        public string? SelectedStatus { get; set; }
        public string ActiveTab { get; set; } = "all"; // all, my-requests, incoming-offers, my-bids

        public int CurrentCompanyID { get; set; }
        public string UserType { get; set; } = "User";
        public bool IsSupplier => UserType.Equals("Supplier", StringComparison.OrdinalIgnoreCase);
    }

    public class AcceptOfferInputModel
    {
        public int OfferId { get; set; }
    }

    public class RejectOfferInputModel
    {
        public int OfferId { get; set; }
        public string? Reason { get; set; }
    }

    public class RequestSelectorDto
    {
        public int RequestID { get; set; }
        public string Code => $"REQ-{RequestID:D4}";
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal? EstimatedBudget { get; set; }
        public int OfferCount { get; set; }
        public string Status { get; set; } = "Beklemede";
    }

    public class OfferComparisonItemDto
    {
        public int OfferID { get; set; }
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string? SupplierOfficial { get; set; }
        public string? SupplierPhone { get; set; }
        public string? SupplierMail { get; set; }
        public string? SupplierCity { get; set; }
        public decimal OfferedPrice { get; set; }
        public decimal UnitOfferPrice { get; set; }
        public int DeliveryDays { get; set; }
        public int ValidityDays { get; set; }
        public DateTime OfferDate { get; set; }
        public string Status { get; set; } = "Beklemede";
        public string? SupplierNotes { get; set; }
        public string? RejectionReason { get; set; }
        public bool IsRevised { get; set; }
        public decimal? PreviousPrice { get; set; }
        public string? PreviousRejectionReason { get; set; }
        public bool IsAccepted => Status == "Kabul Edildi";
        public bool IsRejected => Status == "Reddedildi";
        public bool IsLowestPrice { get; set; }
        public bool IsFastestDelivery { get; set; }
        public decimal? BudgetVariancePct { get; set; }
        public decimal? SavingsAmountComparedToMax { get; set; }
    }

    public class OfferComparisonViewModel
    {
        public Requests? SelectedRequest { get; set; }
        public List<RequestSelectorDto> AvailableRequests { get; set; } = new();
        public List<OfferComparisonItemDto> Offers { get; set; } = new();

        public int TotalOfferCount => Offers.Count;
        public decimal? LowestOfferPrice { get; set; }
        public string? LowestOfferSupplierName { get; set; }
        public int? FastestDeliveryDays { get; set; }
        public string? FastestSupplierName { get; set; }
        public decimal? AverageOfferPrice { get; set; }
        public decimal? MaxOfferPrice { get; set; }
        public decimal? EstimatedBudget => SelectedRequest?.EstimatedBudget;
        public decimal? PotentialSavingsComparedToBudget { get; set; }
        public decimal? PotentialSavingsComparedToMax { get; set; }

        public bool IsRequestApproved => SelectedRequest != null && 
            (SelectedRequest.Status == "Onaylandı" || SelectedRequest.Status == "Kabul Edildi" || SelectedRequest.Status == "Tamamlandı");
        public string? WinningSupplierName => SelectedRequest?.ReceiverName;
        public bool IsOwner { get; set; }
    }
}
