namespace satinalmaportali2.Models {
    /// <summary>Bir ayın alış / satış toplamı.</summary>
    public class MonthlyTotals {
        public int Month { get; set; }
        public string Label { get; set; } = "";
        public decimal Purchase { get; set; }
        public decimal Sale { get; set; }
    }

    /// <summary>Bir yılın alış / satış toplamı.</summary>
    public class YearlyTotals {
        public int Year { get; set; }
        public decimal Purchase { get; set; }
        public decimal Sale { get; set; }
    }

    /// <summary>Bir firmayla yapılan işlemlerin özeti.</summary>
    public class PartnerSummary {
        public Partner Partner { get; set; } = new();
        public decimal Purchase { get; set; }
        public decimal Sale { get; set; }
        public int TransactionCount { get; set; }
        public DateOnly? LastTransactionDate { get; set; }

        public decimal Volume => Purchase + Sale;
    }

    /// <summary>Şirket sahibinin panelinde gösterilen tüm veriler.</summary>
    public class DashboardViewModel {
        public int SelectedYear { get; set; }
        public IReadOnlyList<int> AvailableYears { get; set; } = Array.Empty<int>();

        /// <summary>Seçili yılda yapılan toplam alış (harcanan para).</summary>
        public decimal PurchaseTotal { get; set; }

        /// <summary>Seçili yılda yapılan toplam satış.</summary>
        public decimal SaleTotal { get; set; }

        /// <summary>
        /// Önceki yılın alış toplamı, <b>aynı ay aralığına</b> kısıtlanmış hâli.
        /// Kısmi bir yılı geçen yılın tamamıyla karşılaştırmak sahte bir düşüş üretir.
        /// </summary>
        public decimal PreviousPurchaseTotal { get; set; }

        /// <summary>Önceki yılın satış toplamı, aynı ay aralığında.</summary>
        public decimal PreviousSaleTotal { get; set; }

        /// <summary>Seçili yılda veri bulunan son ay (1-12).</summary>
        public int LastMonthWithData { get; set; }

        /// <summary>Seçili yıl henüz tamamlanmadıysa true.</summary>
        public bool IsPartialYear => LastMonthWithData is > 0 and < 12;

        /// <summary>Karşılaştırmanın hangi aralığa dayandığını anlatan etiket.</summary>
        public string ComparisonNote { get; set; } = "";

        /// <summary>"Oca–Ağu 2026" gibi dönem etiketi.</summary>
        public string PeriodLabel { get; set; } = "";

        public int ActivePartnerCount { get; set; }
        public int PastPartnerCount { get; set; }
        public int TransactionCount { get; set; }

        // ---- Üst şerit göstergeleri ----

        /// <summary>Onay bekleyen talep sayısı.</summary>
        public int PendingApprovalCount { get; set; }

        /// <summary>Bekleyenlerin içinde süresi dolmak üzere olanlar.</summary>
        public int UrgentApprovalCount { get; set; }

        /// <summary>Açık RFQ / ihale sayısı.</summary>
        public int ActiveRfqCount { get; set; }

        /// <summary>Bu hafta kapanacak RFQ sayısı.</summary>
        public int RfqClosingSoonCount { get; set; }

        /// <summary>Bütçesinin altında kalan departmanların topladığı tasarruf.</summary>
        public decimal EstimatedSavings { get; set; }

        /// <summary>Tasarrufun toplam bütçeye oranı (%).</summary>
        public decimal SavingsRatePercent { get; set; }

        // ---- Grafik ve liste verileri ----

        /// <summary>Bütçe / gerçekleşen karşılaştırması — en çok harcayan 6 departman.</summary>
        public IReadOnlyList<DepartmentSpend> Departments { get; set; } = Array.Empty<DepartmentSpend>();

        /// <summary>Kategori kırılımı — donut grafiği.</summary>
        public IReadOnlyList<CategorySpend> Categories { get; set; } = Array.Empty<CategorySpend>();

        /// <summary>Son onay talepleri listesi.</summary>
        public IReadOnlyList<ApprovalRequest> RecentApprovals { get; set; } = Array.Empty<ApprovalRequest>();

        /// <summary>Zamanında teslim oranına göre ilk 5 tedarikçi.</summary>
        public IReadOnlyList<VendorScore> TopVendors { get; set; } = Array.Empty<VendorScore>();

        /// <summary>Kategori toplamı — donut dilim açılarını hesaplarken kullanılır.</summary>
        public decimal CategoryTotal { get; set; }

        public IReadOnlyList<MonthlyTotals> Monthly { get; set; } = Array.Empty<MonthlyTotals>();
        public IReadOnlyList<YearlyTotals> Yearly { get; set; } = Array.Empty<YearlyTotals>();

        /// <summary>Seçili yılda en çok hacim yapılan firmalar.</summary>
        public IReadOnlyList<PartnerSummary> TopPartners { get; set; } = Array.Empty<PartnerSummary>();

        /// <summary>Tüm firmalar — aktif olanlar önce, sonra geçmiştekiler.</summary>
        public IReadOnlyList<PartnerSummary> AllPartners { get; set; } = Array.Empty<PartnerSummary>();

        /// <summary>Satış eksi alış. Pozitifse şirket kazanmış demektir.</summary>
        public decimal NetBalance => SaleTotal - PurchaseTotal;

        /// <summary>Alışın önceki yıla göre değişimi (%). Önceki yıl sıfırsa null.</summary>
        public decimal? PurchaseChangePercent => Change(PurchaseTotal, PreviousPurchaseTotal);

        /// <summary>Satışın önceki yıla göre değişimi (%). Önceki yıl sıfırsa null.</summary>
        public decimal? SaleChangePercent => Change(SaleTotal, PreviousSaleTotal);

        private static decimal? Change(decimal current, decimal previous) {
            if (previous <= 0) return null;
            return (current - previous) / previous * 100m;
        }
    }
}
