namespace satinalmaportali2.Models {

    /// <summary>
    /// Panelin tüm sorgularına aynı anda uygulanan filtre. Tek bir filtre şeridi
    /// bunu doldurur; hiçbir widget kendi başına farklı bir dönem gösteremez.
    /// </summary>
    public sealed record DashboardFilter(DateTime From, DateTime To);

    /// <summary>Veri kaynağının o anki durumu — sahte sayı göstermek yerine bunu söyleriz.</summary>
    public enum DataState {
        /// <summary>Veritabanından okundu.</summary>
        Ok,
        /// <summary>appsettings.json içinde bağlantı dizesi tanımlı değil.</summary>
        NotConfigured,
        /// <summary>Bağlantı dizesi var ama veritabanına ulaşılamadı ya da sorgu hata verdi.</summary>
        Failed
    }

    // ---------------------------------------------------------------
    // Aşağıdaki tipler doğrudan SQL sonuç kümelerinin karşılığıdır.
    // Sayısal alanlar nullable: şemada sütun tipleri belirtilmemiş ve
    // boş tabloda toplama fonksiyonları NULL döner.
    // ---------------------------------------------------------------

    /// <summary>K1–K4 gösterge kartları, tek sorguda.</summary>
    public class SummaryCards {
        public int CurrentRequests { get; set; }
        public int PreviousRequests { get; set; }

        public decimal? AvgScore { get; set; }
        public int ReviewCount { get; set; }

        public decimal? StockValue { get; set; }
        public int ItemCount { get; set; }
        public decimal? AvgUnitPrice { get; set; }

        public int CompanyCount { get; set; }
        public string? TopMembershipName { get; set; }
        public int TopMembershipCount { get; set; }

        /// <summary>Talep adedinin önceki eşit uzunluktaki döneme göre değişimi (%).</summary>
        public decimal? RequestChangePercent =>
            PreviousRequests <= 0 ? null : (CurrentRequests - PreviousRequests) / (decimal)PreviousRequests * 100m;
    }

    /// <summary>W1 — dönem × talep türü × adet.</summary>
    public class RequestTrendRow {
        public DateTime Period { get; set; }
        public string? RequestType { get; set; }
        public int RequestCount { get; set; }
    }

    /// <summary>W2 — talep türü dağılımı.</summary>
    public class RequestTypeRow {
        public string? RequestType { get; set; }
        public int RequestCount { get; set; }
        public decimal SharePercent { get; set; }
    }

    /// <summary>W3 — kategori kırılımı ve fiyat istatistikleri.</summary>
    public class CategoryRow {
        public string? CategoryName { get; set; }
        public int ItemCount { get; set; }
        public long? TotalQuantity { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? AvgPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public decimal? StockValue { get; set; }
    }

    /// <summary>W5 — tedarikçi karnesi.</summary>
    public class VendorScorecardRow {
        public int CompanyID { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyTypeName { get; set; }
        public int ReceivedRequests { get; set; }
        public decimal? AvgScore { get; set; }
        public int ReviewCount { get; set; }

        /// <summary>Puan ortalamasının anlamlı sayılması için gereken en az değerlendirme.</summary>
        public const int MinReviewsForRanking = 3;

        /// <summary>Yeterli değerlendirmesi yoksa sıralamada güvenilir kabul edilmez.</summary>
        public bool HasReliableScore => AvgScore.HasValue && ReviewCount >= MinReviewsForRanking;
    }

    /// <summary>W6 — şirket tipi × üyelik tipi matrisi.</summary>
    public class CompanyMixRow {
        public string? CompanyTypeName { get; set; }
        public string? MembershipTypeName { get; set; }
        public int CompanyCount { get; set; }
    }

    /// <summary>W7 — ülke / şehir kırılımı.</summary>
    public class RegionRow {
        public string? CountryName { get; set; }
        public string? TownName { get; set; }
        public int CompanyCount { get; set; }
    }

    /// <summary>W8 — alıcı → satıcı talep akışı.</summary>
    public class FlowRow {
        public string? SenderName { get; set; }
        public string? ReceiverName { get; set; }
        public int RequestCount { get; set; }
    }

    /// <summary>W9 — iletişim formu konuları.</summary>
    public class FormTopicRow {
        public string? Topic { get; set; }
        public int FormCount { get; set; }
        public DateTime? LastReceived { get; set; }
    }

    /// <summary>W10 — marka dağılımı.</summary>
    public class BrandRow {
        public string? Brand { get; set; }
        public int ItemCount { get; set; }
        public decimal? StockValue { get; set; }
    }

    /// <summary>Kategori hiyerarşisi (Ana ve Alt Kategoriler).</summary>
    public class CategoryTreeItemDto {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = "";
        public int? ParentCategoryID { get; set; }
        public string? ParentCategoryName { get; set; }
        public List<CategoryTreeItemDto> SubCategories { get; set; } = new();
        public int ItemCount { get; set; }
    }

    // =========================================================================
    // ALICI (BUYER) DASHBOARD DTO VE VIEW MODELLERİ
    // =========================================================================

    public class BuyerDashboardViewModel {
        public DataState State { get; set; } = DataState.Ok;
        public int SelectedYear { get; set; }
        public IReadOnlyList<int> AvailableYears { get; set; } = Array.Empty<int>();
        public BuyerSummaryDto Summary { get; set; } = new();
        public BuyerSpendTrendDto SpendTrend { get; set; } = new();
        public IReadOnlyList<BuyerRequestItemDto> RecentRequests { get; set; } = Array.Empty<BuyerRequestItemDto>();
        public IReadOnlyList<BuyerPurchasedItemDto> PurchasedProducts { get; set; } = Array.Empty<BuyerPurchasedItemDto>();
        public int TotalPurchasedProductsCount { get; set; }
        public decimal TotalPurchasedProductsSpend { get; set; }
        public IReadOnlyList<BuyerBudgetItemDto> BudgetCategories { get; set; } = Array.Empty<BuyerBudgetItemDto>();
        public IReadOnlyList<BuyerSupplierScoreDto> TopSuppliers { get; set; } = Array.Empty<BuyerSupplierScoreDto>();
        public bool IsEmpty => RecentRequests.Count == 0 && PurchasedProducts.Count == 0 && Summary.TotalActiveRequests == 0;
    }

    public class BuyerSummaryDto {
        public int TotalActiveRequests { get; set; }
        public int RequestsThisWeek { get; set; }
        public decimal? RequestChangePercent { get; set; }
        public int PendingApprovals { get; set; }
        public int UrgentApprovals { get; set; }
        public decimal CurrentPeriodSpend { get; set; }
        public decimal? SpendChangePercent { get; set; }
        public decimal SupplierPerformanceScore { get; set; }
        public int TotalReviews { get; set; }
        public decimal OnTimeDeliveryRate { get; set; }
    }

    public class BuyerSpendTrendDto {
        public IReadOnlyList<string> Months { get; set; } = Array.Empty<string>();
        public IReadOnlyList<decimal> MonthlySpend { get; set; } = Array.Empty<decimal>();
        public IReadOnlyList<decimal> MonthlyBudget { get; set; } = Array.Empty<decimal>();
        public decimal MaxSpendValue { get; set; }
    }

    public class BuyerRequestItemDto {
        public string RequestCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public int ReceivedBidsCount { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? Deadline { get; set; }
        public decimal EstimatedBudget { get; set; }
        public string Status { get; set; } = "Açık"; // Açık, Beklemede, Onaylandı, Kazanıldı
        public string Priority { get; set; } = "Normal";
        public string? BuyerNotes { get; set; }
    }

    public class BuyerBudgetItemDto {
        public string CategoryName { get; set; } = "";
        public decimal AllocatedBudget { get; set; }
        public decimal SpentAmount { get; set; }
        public decimal SpentPercent => AllocatedBudget > 0 ? Math.Round(SpentAmount * 100m / AllocatedBudget, 1) : 0;
    }

    public class BuyerSupplierScoreDto {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = "";
        public string Category { get; set; } = "";
        public decimal Score { get; set; }
        public int ReviewCount { get; set; }
        public decimal OnTimeRate { get; set; }
        public int CompletedOrders { get; set; }
    }

    // =========================================================================
    // TEDARİKÇİ (SUPPLIER) DASHBOARD DTO VE VIEW MODELLERİ
    // =========================================================================

    public class SupplierDashboardViewModel {
        public DataState State { get; set; } = DataState.Ok;
        public int? CompanyID { get; set; }
        public int SelectedYear { get; set; }
        public IReadOnlyList<int> AvailableYears { get; set; } = Array.Empty<int>();
        public SupplierSummaryDto Summary { get; set; } = new();
        public IReadOnlyList<SupplierRfqItemDto> OpenRfqs { get; set; } = Array.Empty<SupplierRfqItemDto>();
        public SupplierEarningsTrendDto EarningsTrend { get; set; } = new();
        public IReadOnlyList<SupplierActionItemDto> ActionItems { get; set; } = Array.Empty<SupplierActionItemDto>();
        public IReadOnlyList<SupplierCatalogItemDto> FeaturedProducts { get; set; } = Array.Empty<SupplierCatalogItemDto>();
        public bool IsEmpty => OpenRfqs.Count == 0 && Summary.NewIncomingRfqs == 0;
    }

    public class SupplierSummaryDto {
        public int NewIncomingRfqs { get; set; }
        public int NewRfqsThisWeek { get; set; }
        public int RfqsAwaitingResponse { get; set; }
        public int ActiveBids { get; set; }
        public decimal ActiveBidsValue { get; set; }
        public int WonAuctions { get; set; }
        public decimal WinRatePercent { get; set; }
        public decimal WonTotalRevenue { get; set; }
        public decimal CatalogStockValue { get; set; }
        public int ActiveSkuCount { get; set; }
        public int LowStockAlertCount { get; set; }
    }

    public class SupplierRfqItemDto {
        public int RequestID { get; set; }
        public int? SenderID { get; set; }
        public bool IsOwner { get; set; }
        public string RfqCode { get; set; } = "";
        public string BuyerCompany { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string Category { get; set; } = "";
        public int Quantity { get; set; }
        public string Unit { get; set; } = "Adet";
        public DateTime Deadline { get; set; }
        public int DaysLeft { get; set; }
        public decimal BudgetEstimate { get; set; }
        public string Status { get; set; } = "Cevap Bekliyor"; // Cevap Bekliyor, Teklif Verildi, İncelemede
        public bool HasMyBid { get; set; }
        public decimal? MyBidAmount { get; set; }
        public string? MyBidStatus { get; set; }
        public string? MyBidRejectionReason { get; set; }
    }

    public class SupplierEarningsTrendDto {
        public IReadOnlyList<string> Months { get; set; } = Array.Empty<string>();
        public IReadOnlyList<decimal> MonthlyEarnings { get; set; } = Array.Empty<decimal>();
        public IReadOnlyList<decimal> MonthlyPendingPipeline { get; set; } = Array.Empty<decimal>();
        public decimal MaxEarningsValue { get; set; }
    }

    public class SupplierActionItemDto {
        public string Id { get; set; } = "";
        public string Type { get; set; } = ""; // pricing, stock, urgent_rfq, review
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string BadgeText { get; set; } = "";
        public string BadgeType { get; set; } = "amber"; // amber, danger, blue, green
        public string ActionUrl { get; set; } = "#";
        public string ActionText { get; set; } = "İncele";
    }

    public class SupplierCatalogItemDto {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = "";
        public string Sku { get; set; } = "";
        public string Category { get; set; } = "";
        public int Stock { get; set; }
        public decimal UnitPrice { get; set; }
        public string Status { get; set; } = "Stokta";
    }

    public class SupplierListingsViewModel {
        public IReadOnlyList<SupplierListingDetailDto> Listings { get; set; } = Array.Empty<SupplierListingDetailDto>();
        public int TotalCount { get; set; }
        public decimal TotalCatalogValue { get; set; }
        public int ActiveCount { get; set; }
        public int LowStockCount { get; set; }
        public IReadOnlyList<string> AvailableCategories { get; set; } = Array.Empty<string>();
        public string? SelectedCategory { get; set; }
        public string? SearchQuery { get; set; }
        public string? SelectedStatus { get; set; }
        public string CompanyName { get; set; } = "";
        public bool IsIyzicoApproved { get; set; }
        public string? IyzicoSubMerchantKey { get; set; }
    }

    public class SupplierListingDetailDto {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = "";
        public string Sku { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public int? CategoryId { get; set; }
        public int Stock { get; set; }
        public decimal UnitPrice { get; set; }
        public string Brand { get; set; } = "";
        public string? Description { get; set; }
        public string? Properties { get; set; }
        public string Status { get; set; } = "Stokta";
        public bool IsLiveOnVitrin { get; set; } = true;
    }

    public class BuyerOrdersViewModel {
        public IReadOnlyList<Order> Orders { get; set; } = Array.Empty<Order>();
        public int TotalOrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public int ActiveShipmentCount { get; set; }
        public int CompletedCount { get; set; }
        public string? SearchQuery { get; set; }
        public string? SelectedStatus { get; set; }
        public string CompanyName { get; set; } = "";
    }

    public class SupplierOrdersViewModel {
        public IReadOnlyList<Order> Orders { get; set; } = Array.Empty<Order>();
        public int TotalOrderCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingShipmentCount { get; set; }
        public int CompletedCount { get; set; }
        public string? SearchQuery { get; set; }
        public string? SelectedStatus { get; set; }
        public string CompanyName { get; set; } = "";
    }

    /// <summary>
    /// Alıcı firmanın Dashboard'da listelenen satın aldığı ürün dökümü DTO modeli.
    /// </summary>
    public class BuyerPurchasedItemDto
    {
        public int? ItemID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? CategoryName { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "Adet";
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public string? SupplierCompanyName { get; set; }
        public int? SupplierCompanyID { get; set; }
        public int OrderID { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; } = "Onaylandı";
        public string PaymentStatus { get; set; } = "Ödendi";
        public string? TrackingNumber { get; set; }
        public string? ShippingCarrier { get; set; }
    }

    /// <summary>
    /// Satın alma ve checkout işlemlerinde veritabanından çekilen kurumsal profil bilgileri.
    /// </summary>
    public class CheckoutProfileDto
    {
        public bool IsAuthenticated { get; set; }
        public int CompanyID { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyOfficial { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public string TaxOffice { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DeliveryCity { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
    }

    /// <summary>
    /// Bağımsız Ürün Detay Sayfası (Product Detail Page) ViewModel
    /// </summary>
    public class ProductDetailPageViewModel
    {
        public int ItemID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; } = 0.20m;
        public decimal TaxAmount => Math.Round(UnitPrice * TaxRate, 2);
        public decimal GrandTotal => UnitPrice + TaxAmount;
        public int StockQuantity { get; set; }
        public bool IsOutOfStock => StockQuantity <= 0;
        public string CategoryName { get; set; } = "Endüstriyel Ürünler";
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = "/images/default-product.svg";
        public Dictionary<string, string> Specifications { get; set; } = new();

        // Tedarikçi Firma Bilgileri
        public int SupplierCompanyID { get; set; }
        public string SupplierCompanyName { get; set; } = "Onaylı Tedarikçi";
        public string SupplierOfficial { get; set; } = string.Empty;
        public string SupplierLocation { get; set; } = "Türkiye";
        public double SupplierScore { get; set; } = 9.8;
        public int SupplierReviewCount { get; set; } = 0;
        public bool IsOwner { get; set; } // Giriş yapan firma ürünün sahibi mi?

        // Benzer / İlgili Ürünler
        public List<RelatedProductCardDto> RelatedProducts { get; set; } = new();
    }

    public class RelatedProductCardDto
    {
        public int ItemID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public string ImageUrl { get; set; } = "/images/default-product.svg";
        public int StockQuantity { get; set; }
        public bool IsOutOfStock => StockQuantity <= 0;
    }
}
