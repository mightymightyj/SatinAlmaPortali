namespace satinalmaportali2.Models {
    /// <summary>İş birliği yapılan firmanın durumu.</summary>
    public enum PartnershipStatus {
        /// <summary>Anlaşma hâlen sürüyor.</summary>
        Active,
        /// <summary>Geçmişte çalışılmış, anlaşma sonlanmış.</summary>
        Past
    }

    /// <summary>Şirketin alış veya satış yaptığı karşı firma.</summary>
    public class Partner {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Sector { get; set; } = "";
        public string City { get; set; } = "";

        /// <summary>İş birliğinin başladığı tarih.</summary>
        public DateOnly StartDate { get; set; }

        /// <summary>İş birliğinin bittiği tarih; hâlen sürüyorsa null.</summary>
        public DateOnly? EndDate { get; set; }

        public PartnershipStatus Status => EndDate is null ? PartnershipStatus.Active : PartnershipStatus.Past;

        public string StatusText => Status == PartnershipStatus.Active ? "Aktif" : "Geçmiş";
    }
}
