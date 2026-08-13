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
}
