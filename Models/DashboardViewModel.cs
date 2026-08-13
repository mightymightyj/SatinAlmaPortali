namespace satinalmaportali2.Models {

    /// <summary>Trend grafiğinde tek bir çizgi — değerler dönem listesiyle hizalıdır.</summary>
    public class TrendSeries {
        public string Name { get; set; } = "";
        public IReadOnlyList<int> Values { get; set; } = Array.Empty<int>();
    }

    /// <summary>
    /// Panelin tüm verisi. Her alan mevcut şemadaki bir sorgudan gelir;
    /// hiçbir alan uygulama tarafında üretilmez.
    /// </summary>
    public class DashboardViewModel {

        // ---- Veri kaynağı durumu ----
        public DataState State { get; set; } = DataState.Ok;

        /// <summary>Durum Ok değilse kullanıcıya gösterilecek açıklama.</summary>
        public string? StateMessage { get; set; }

        /// <summary>Geliştiriciye yardımcı olacak teknik ayrıntı (yalnızca Development ortamında gösterilir).</summary>
        public string? StateDetail { get; set; }

        // ---- Filtre ----
        public int SelectedYear { get; set; }
        public IReadOnlyList<int> AvailableYears { get; set; } = Array.Empty<int>();
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        // ---- Widget verileri ----
        public SummaryCards Summary { get; set; } = new();

        /// <summary>W1 — trend grafiğinin x ekseni; boş aylar dâhil, sırayla.</summary>
        public IReadOnlyList<DateTime> TrendPeriods { get; set; } = Array.Empty<DateTime>();

        /// <summary>W1 — talep türü başına bir çizgi.</summary>
        public IReadOnlyList<TrendSeries> TrendSeriesList { get; set; } = Array.Empty<TrendSeries>();

        public IReadOnlyList<RequestTypeRow> RequestTypes { get; set; } = Array.Empty<RequestTypeRow>();
        public IReadOnlyList<CategoryRow> Categories { get; set; } = Array.Empty<CategoryRow>();
        public IReadOnlyList<VendorScorecardRow> Vendors { get; set; } = Array.Empty<VendorScorecardRow>();
        public IReadOnlyList<CompanyMixRow> CompanyMix { get; set; } = Array.Empty<CompanyMixRow>();
        public IReadOnlyList<RegionRow> Regions { get; set; } = Array.Empty<RegionRow>();
        public IReadOnlyList<FlowRow> Flows { get; set; } = Array.Empty<FlowRow>();
        public IReadOnlyList<FormTopicRow> FormTopics { get; set; } = Array.Empty<FormTopicRow>();
        public IReadOnlyList<BrandRow> Brands { get; set; } = Array.Empty<BrandRow>();
        public IReadOnlyList<CategoryTreeItemDto> CategoryTree { get; set; } = Array.Empty<CategoryTreeItemDto>();
        public IReadOnlyList<CategoryTreeItemDto> AllCategories { get; set; } = Array.Empty<CategoryTreeItemDto>();

        // ---- Görünüm yardımcıları ----

        /// <summary>W6 — matrisin satır başlıkları (şirket tipleri).</summary>
        public IReadOnlyList<string> CompanyTypeNames =>
            CompanyMix.Select(m => m.CompanyTypeName ?? "—").Distinct().ToList();

        /// <summary>W6 — matrisin seri adları (üyelik tipleri).</summary>
        public IReadOnlyList<string> MembershipNames =>
            CompanyMix.Select(m => m.MembershipTypeName ?? "—").Distinct().ToList();

        /// <summary>Panelde gösterilecek hiç veri yoksa true.</summary>
        public bool IsEmpty =>
            State == DataState.Ok
            && Summary.CurrentRequests == 0
            && Categories.Count == 0
            && Vendors.Count == 0;
    }
}
