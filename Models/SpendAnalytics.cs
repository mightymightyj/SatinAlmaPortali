namespace satinalmaportali2.Models {
    /// <summary>Onay akışındaki bir talebin durumu.</summary>
    public enum ApprovalStatus {
        Approved,
        Pending,
        Rejected
    }

    /// <summary>Bir departmanın bütçesi ve fiilî harcaması.</summary>
    public class DepartmentSpend {
        public string Name { get; set; } = "";
        public decimal Budgeted { get; set; }
        public decimal Actual { get; set; }

        /// <summary>Pozitifse bütçe aşılmış demektir.</summary>
        public decimal Variance => Actual - Budgeted;

        public bool IsOverBudget => Actual > Budgeted;

        /// <summary>Bütçeye göre sapma yüzdesi. Bütçe sıfırsa null.</summary>
        public decimal? VariancePercent =>
            Budgeted <= 0 ? null : Variance / Budgeted * 100m;
    }

    /// <summary>Harcamanın kategori kırılımı — donut grafiğin verisi.</summary>
    public class CategorySpend {
        public string Name { get; set; } = "";
        public decimal Amount { get; set; }

        /// <summary>Toplam içindeki payı; controller tarafından doldurulur.</summary>
        public decimal SharePercent { get; set; }
    }

    /// <summary>Onay bekleyen ya da sonuçlanmış satın alma talebi.</summary>
    public class ApprovalRequest {
        public string RequestId { get; set; } = "";
        public string Vendor { get; set; } = "";
        public string Department { get; set; } = "";
        public decimal Amount { get; set; }
        public ApprovalStatus Status { get; set; }
        public DateOnly RequestedOn { get; set; }

        /// <summary>Beklemede olup süresi dolmak üzere olan talepler acil sayılır.</summary>
        public bool IsUrgent { get; set; }

        public string StatusText => Status switch {
            ApprovalStatus.Approved => "Onaylandı",
            ApprovalStatus.Pending => "Beklemede",
            _ => "Reddedildi"
        };

        /// <summary>CSS sınıf eki — durum rozetinin rengini seçer.</summary>
        public string StatusKey => Status switch {
            ApprovalStatus.Approved => "approved",
            ApprovalStatus.Pending => "pending",
            _ => "rejected"
        };
    }

    /// <summary>Tedarikçi performans karnesi.</summary>
    public class VendorScore {
        public string Name { get; set; } = "";
        public string Sector { get; set; } = "";

        /// <summary>Zamanında teslim oranı (%).</summary>
        public double OnTimeDeliveryPercent { get; set; }

        /// <summary>Kalite puanı, 5 üzerinden.</summary>
        public double QualityScore { get; set; }

        public int OrderCount { get; set; }

        /// <summary>Ölçüt: %95 ve üzeri iyi, %85 altı kritik.</summary>
        public string PerformanceKey =>
            OnTimeDeliveryPercent >= 95 ? "good"
            : OnTimeDeliveryPercent >= 85 ? "watch"
            : "critical";

        public string PerformanceText =>
            OnTimeDeliveryPercent >= 95 ? "Hedefte"
            : OnTimeDeliveryPercent >= 85 ? "İzlemede"
            : "Riskli";
    }
}
