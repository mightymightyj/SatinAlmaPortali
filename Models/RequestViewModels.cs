using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace satinalmaportali2.Models
{
    public class CreateRequestItemInputModel
    {
        public int ItemOrder { get; set; } = 1;

        [Required(ErrorMessage = "Kalem / Malzeme adı zorunludur.")]
        [Display(Name = "Kalem / Malzeme Adı")]
        public string ItemName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Miktar zorunludur.")]
        [Range(0.01, 10000000, ErrorMessage = "Lütfen 0'dan büyük geçerli bir miktar giriniz.")]
        [Display(Name = "Miktar")]
        public decimal Quantity { get; set; } = 1;

        [Display(Name = "Birim")]
        public string Unit { get; set; } = "Adet";

        [Display(Name = "Tahmini Birim Fiyat (₺)")]
        [Range(0, 1000000000, ErrorMessage = "Geçerli bir birim fiyat giriniz.")]
        public decimal? EstimatedUnitPrice { get; set; }

        [Display(Name = "Kalem Açıklaması / Şartname")]
        public string? ItemDescription { get; set; }
    }

    public class CreateRequestInputModel
    {
        [Display(Name = "Alıcı Bilgileri")]
        public string? BuyerInfo { get; set; }

        [Display(Name = "Talep Başlığı / Konusu")]
        public string ProductName { get; set; } = string.Empty;

        [Display(Name = "Toplam Miktar")]
        public int Quantity { get; set; } = 1;

        [Display(Name = "Talep Tipi / Kategori")]
        public string RequestType { get; set; } = "Satın Alma";

        public int? CategoryID { get; set; }

        [Display(Name = "Tahmini Bütçe (₺)")]
        [Range(0, 1000000000, ErrorMessage = "Geçerli bir bütçe tutarı giriniz.")]
        public decimal? EstimatedBudget { get; set; }

        [Display(Name = "Hedef Teslim / Son Teklif Tarihi")]
        public DateTime? TargetDeliveryDate { get; set; }

        [Display(Name = "Teknik Şartname & Genel Notlar")]
        public string? Notes { get; set; }

        // Dinamik İstenilenler Listesi (Talep Kalemleri)
        public List<CreateRequestItemInputModel> Items { get; set; } = new();
    }

    public class SubmitOfferItemInputModel
    {
        public int RequestItemID { get; set; }
        public decimal UnitPrice { get; set; }
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

        // Kalem Bazlı Teklif Listesi
        public List<SubmitOfferItemInputModel> Items { get; set; } = new();
    }

    public class RequestItemDto
    {
        public int RequestItemID { get; set; }
        public int RequestID { get; set; }
        public int ItemOrder { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "Adet";
        public decimal? EstimatedUnitPrice { get; set; }
        public decimal? EstimatedTotalPrice => EstimatedUnitPrice.HasValue ? EstimatedUnitPrice.Value * Quantity : null;
        public string? ItemDescription { get; set; }
    }

    public class RequestOfferItemDto
    {
        public int OfferItemID { get; set; }
        public int OfferID { get; set; }
        public int RequestItemID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal Quantity { get; set; } = 1;
        public string Unit { get; set; } = "Adet";
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public string? Notes { get; set; }
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

        public List<RequestOfferItemDto> OfferItems { get; set; } = new();
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

        public int ItemCount { get; set; } = 1;
        public List<RequestItemDto> Items { get; set; } = new();

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

        public int ItemCount { get; set; } = 1;
        public List<RequestItemDto> Items { get; set; } = new();

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
        public int ItemCount { get; set; } = 1;
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

        public List<RequestOfferItemDto> OfferItems { get; set; } = new();
    }

    public class OfferComparisonViewModel
    {
        public Requests? SelectedRequest { get; set; }
        public List<RequestSelectorDto> AvailableRequests { get; set; } = new();
        public List<OfferComparisonItemDto> Offers { get; set; } = new();
        public List<RequestItemDto> Items { get; set; } = new();

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

    /// <summary>
    /// Tedarikçi olarak kurumsal ürün ilanı ve fiyat listeleme DTO modeli.
    /// </summary>
    public class CreateProductListingInputModel
    {
        [Required(ErrorMessage = "Ürün adı zorunludur.")]
        public string ProductName { get; set; } = string.Empty;

        public int? CategoryID { get; set; }
        public string? CategoryName { get; set; }

        [Required(ErrorMessage = "Birim fiyat zorunludur.")]
        public decimal UnitPrice { get; set; }

        public string? StockQuantity { get; set; }
        public string? ItemDescription { get; set; }
        public string? Brand { get; set; }
        public string? Properties { get; set; }
        public string? ImageUrl { get; set; }
    }

    /// <summary>
    /// B2B Vitrinden hızlı kurumsal teklif (RFQ) isteme DTO modeli.
    /// </summary>
    public class SubmitVitrinQuoteInputModel
    {
        public int? ProductID { get; set; }

        [Required(ErrorMessage = "Firma ünvanı zorunludur.")]
        public string BuyerCompany { get; set; } = string.Empty;

        public string? BuyerTax { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Talep edilen miktar en az 1 olmalıdır.")]
        public int Quantity { get; set; } = 1;

        public decimal? TargetPrice { get; set; }
        public string? PaymentTerms { get; set; }
        public string? DeliveryCity { get; set; }
        public string? Notes { get; set; }
        public bool SampleRequested { get; set; }
    }

    /// <summary>
    /// Tedarikçi panelinden katalog ürün fiyat ve stok güncelleme DTO modeli.
    /// </summary>
    public class UpdateProductPriceInputModel
    {
        public int ItemID { get; set; }
        public decimal NewUnitPrice { get; set; }
        public int? NewStockQuantity { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>
    /// Sepet kalemi DTO modeli.
    /// </summary>
    public class CartItemInputModel
    {
        public int ProductID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? Category { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; } = 1;
        public string? Seller { get; set; }
        public string? Image { get; set; }
    }

    /// <summary>
    /// Kurumsal doğrudan sipariş oluşturma DTO modeli.
    /// </summary>
    public class CreateOrderInputModel
    {
        [Required(ErrorMessage = "Firma ünvanı zorunludur.")]
        public string BuyerCompany { get; set; } = string.Empty;

        public string? BuyerTaxNumber { get; set; }
        public string? BuyerTaxOffice { get; set; }
        public string? BuyerEmail { get; set; }
        public string? BuyerPhone { get; set; }

        [Required(ErrorMessage = "Teslimat şehri zorunludur.")]
        public string DeliveryCity { get; set; } = string.Empty;

        [Required(ErrorMessage = "Teslimat adresi zorunludur.")]
        public string DeliveryAddress { get; set; } = string.Empty;

        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }

        public string PaymentMethod { get; set; } = "Kredi Kartı"; // Kredi Kartı, Havale / EFT, Cari Hesap / Vadeli, DBS
        public string? Notes { get; set; }

        public List<CartItemInputModel> Items { get; set; } = new List<CartItemInputModel>();
    }

    /// <summary>
    /// Sipariş durum ve kargo güncelleme DTO modeli.
    /// </summary>
    public class UpdateOrderStatusInputModel
    {
        public int OrderID { get; set; }
        public string OrderStatus { get; set; } = "Kargoya Verildi"; // Hazırlanıyor, Kargoya Verildi, Teslim Edildi, İptal Edildi
        public string? TrackingNumber { get; set; }
        public string? ShippingCarrier { get; set; }
        public string? Notes { get; set; }
        public string? CancellationReason { get; set; } // Satıcı olumsuz durumda siparişi iptal ettiğinde girilen gerekçe
    }

    /// <summary>
    /// Alıcının siparişi gerekçe belirterek iptal etme modeli.
    /// </summary>
    public class CancelOrderByBuyerInputModel
    {
        public int OrderID { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
