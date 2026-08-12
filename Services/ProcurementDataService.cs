using satinalmaportali2.Models;

namespace satinalmaportali2.Services {
    public interface IProcurementDataService {
        IReadOnlyList<Partner> GetPartners();
        IReadOnlyList<Transaction> GetTransactions();

        /// <summary>Verilen yılın harcamasını departmanlara dağıtır ve bütçeyle eşler.</summary>
        IReadOnlyList<DepartmentSpend> GetDepartments(int year);

        /// <summary>Verilen yılın harcamasının kategori kırılımı.</summary>
        IReadOnlyList<CategorySpend> GetCategories(int year);

        /// <summary>Onay akışındaki talepler, en yeniden eskiye.</summary>
        IReadOnlyList<ApprovalRequest> GetApprovals();

        /// <summary>Aktif tedarikçilerin performans karneleri.</summary>
        IReadOnlyList<VendorScore> GetVendorScores();

        /// <summary>Açık RFQ / ihale sayısı ve bu hafta kapanacaklar.</summary>
        (int Active, int ClosingSoon) GetRfqCounts();
    }

    /// <summary>
    /// Panelin verisini bellekte üretir. Veritabanı bağlanana kadar geçici kaynak:
    /// tohum sabit olduğu için üretilen rakamlar her açılışta aynı çıkar.
    /// Gerçek veriye geçerken sadece bu sınıfın yerine EF Core tabanlı bir
    /// uygulama yazmak yeterli; panel arayüzü değişmez.
    /// </summary>
    public class ProcurementDataService : IProcurementDataService {
        private const int Seed = 20260812;

        /// <summary>
        /// Paylaşılan tek örnek. Panel Program.cs'e kayıt gerektirmesin diye burada
        /// duruyor; ileride DI'ya taşınırsa bu alan kaldırılabilir.
        /// </summary>
        private static readonly Lazy<ProcurementDataService> Shared =
            new(() => new ProcurementDataService(), isThreadSafe: true);

        public static ProcurementDataService Instance => Shared.Value;

        private readonly List<Partner> _partners;
        private readonly List<Transaction> _transactions;
        private readonly List<ApprovalRequest> _approvals;
        private readonly List<VendorScore> _vendorScores;
        private readonly int _activeRfqCount;
        private readonly int _rfqClosingSoonCount;

        public ProcurementDataService() {
            _partners = BuildPartners();
            _transactions = BuildTransactions(_partners);
            _approvals = BuildApprovals(_partners);
            _vendorScores = BuildVendorScores(_partners, _transactions);

            // RFQ modülü henüz yok; sayılar aktif tedarikçi sayısından türetiliyor.
            var activeCount = _partners.Count(p => p.Status == PartnershipStatus.Active);
            _activeRfqCount = activeCount * 2 + 3;
            _rfqClosingSoonCount = Math.Max(1, activeCount / 3);
        }

        public IReadOnlyList<Partner> GetPartners() => _partners;

        public IReadOnlyList<Transaction> GetTransactions() => _transactions;

        // Departman ve kategori payları sabittir; yılın gerçek alış toplamı bu
        // paylara bölünür, böylece üstteki "Toplam Harcama" kartı ile grafikler
        // birbirini tutar.
        private static readonly (string Name, double Share, double ActualOverBudget)[] DepartmentShares = {
            ("Üretim",               0.30, 1.06),
            ("Bilgi Teknolojileri",  0.20, 0.92),
            ("Lojistik",             0.17, 1.03),
            ("Bakım ve Onarım",      0.13, 0.88),
            ("İdari İşler",          0.11, 0.95),
            ("Pazarlama",            0.09, 1.09)
        };

        private static readonly (string Name, double Share)[] CategoryShares = {
            ("BT Donanım",      0.28),
            ("Lojistik",        0.31),
            ("Tesis Yönetimi",  0.27),
            ("Ofis Sarf",       0.14)
        };

        public IReadOnlyList<DepartmentSpend> GetDepartments(int year) {
            var yearTotal = _transactions
                .Where(t => t.Date.Year == year && t.Type == TransactionType.Purchase)
                .Sum(t => t.Amount);

            return DepartmentShares.Select(d => {
                var actual = Round(yearTotal * (decimal)d.Share);
                return new DepartmentSpend {
                    Name = d.Name,
                    Actual = actual,
                    Budgeted = Round(actual / (decimal)d.ActualOverBudget)
                };
            }).ToList();
        }

        public IReadOnlyList<CategorySpend> GetCategories(int year) {
            var yearTotal = _transactions
                .Where(t => t.Date.Year == year && t.Type == TransactionType.Purchase)
                .Sum(t => t.Amount);

            return CategoryShares
                .Select(c => new CategorySpend { Name = c.Name, Amount = Round(yearTotal * (decimal)c.Share) })
                .OrderByDescending(c => c.Amount)
                .ToList();
        }

        public IReadOnlyList<ApprovalRequest> GetApprovals() => _approvals;

        public IReadOnlyList<VendorScore> GetVendorScores() => _vendorScores;

        public (int Active, int ClosingSoon) GetRfqCounts() => (_activeRfqCount, _rfqClosingSoonCount);

        private static decimal Round(decimal value) => Math.Round(value / 100m) * 100m;

        private record PartnerSeed(
            string Name, string Sector, string City,
            int StartYear, int StartMonth,
            int? EndYear, int? EndMonth,
            double Scale, double SaleRatio);

        private static readonly PartnerSeed[] Seeds = {
            new("Anadolu Metal Sanayi A.Ş.",        "Metal",    "Kocaeli",   2021, 2,  null, null, 1.9, 0.35),
            new("Ege Plastik ve Ambalaj Ltd. Şti.", "Ambalaj",  "İzmir",     2021, 6,  null, null, 1.2, 0.55),
            new("Marmara Lojistik A.Ş.",            "Lojistik", "İstanbul",  2022, 1,  null, null, 0.9, 0.15),
            new("Trakya Kimya San. Tic. A.Ş.",      "Kimya",    "Tekirdağ",  2021, 1,  2023, 8,    1.4, 0.30),
            new("Konya Makine ve Otomasyon A.Ş.",   "Makine",   "Konya",     2023, 3,  null, null, 1.6, 0.45),
            new("Bursa Otomotiv Yan Sanayi Ltd.",   "Otomotiv", "Bursa",     2022, 5,  null, null, 1.3, 0.70),
            new("Antalya Tarım Ürünleri A.Ş.",      "Tarım",    "Antalya",   2021, 4,  2022, 11,   0.8, 0.60),
            new("Gaziantep Tekstil San. A.Ş.",      "Tekstil",  "Gaziantep", 2024, 2,  null, null, 1.1, 0.65),
            new("Ankara Elektrik Malzemeleri Ltd.", "Elektrik", "Ankara",    2021, 9,  2024, 5,    1.0, 0.25),
            new("Samsun Depolama ve Antrepo A.Ş.",  "Depolama", "Samsun",    2025, 1,  null, null, 0.7, 0.20)
        };

        private static List<Partner> BuildPartners() {
            var partners = new List<Partner>();
            for (var i = 0; i < Seeds.Length; i++) {
                var s = Seeds[i];
                partners.Add(new Partner {
                    Id = i + 1,
                    Name = s.Name,
                    Sector = s.Sector,
                    City = s.City,
                    StartDate = new DateOnly(s.StartYear, s.StartMonth, 1),
                    EndDate = s.EndYear is int ey && s.EndMonth is int em
                        ? new DateOnly(ey, em, DateTime.DaysInMonth(ey, em))
                        : null
                });
            }
            return partners;
        }

        private static List<Transaction> BuildTransactions(List<Partner> partners) {
            var random = new Random(Seed);
            var today = DateOnly.FromDateTime(DateTime.Today);
            var transactions = new List<Transaction>();
            var id = 1;

            for (var i = 0; i < partners.Count; i++) {
                var partner = partners[i];
                var seed = Seeds[i];
                var lastDay = partner.EndDate ?? today;
                if (lastDay > today) lastDay = today;

                var month = new DateOnly(partner.StartDate.Year, partner.StartDate.Month, 1);
                while (month <= lastDay) {
                    // Yıllara göre büyüme + mevsimsel dalgalanma, grafiğin düz çizgi olmaması için.
                    var growth = 1.0 + 0.12 * (month.Year - 2021);
                    var season = 1.0 + 0.25 * Math.Sin((month.Month - 1) / 12.0 * 2 * Math.PI);
                    var monthlyBase = 260_000.0 * seed.Scale * growth * season;

                    var window = new MonthWindow(partner, month, lastDay);
                    id = AddMonthlyTransactions(transactions, random, window, id,
                        TransactionType.Purchase, random.Next(1, 4), monthlyBase);
                    id = AddMonthlyTransactions(transactions, random, window, id,
                        TransactionType.Sale, random.Next(0, 3), monthlyBase * seed.SaleRatio);

                    month = month.AddMonths(1);
                }
            }

            return transactions.OrderBy(t => t.Date).ThenBy(t => t.Id).ToList();
        }

        /// <summary>Tek bir firmanın tek bir ayı — işlem tarihleri bu aralığa sıkışır.</summary>
        private record MonthWindow(Partner Partner, DateOnly Month, DateOnly LastDay);

        /// <summary>Verilen ay için rastgele işlem üretir ve bir sonraki kimliği döndürür.</summary>
        private static int AddMonthlyTransactions(
            List<Transaction> target, Random random, MonthWindow window, int nextId,
            TransactionType type, int count, double amountBase) {

            var daysInMonth = DateTime.DaysInMonth(window.Month.Year, window.Month.Month);
            for (var n = 0; n < count; n++) {
                var date = new DateOnly(window.Month.Year, window.Month.Month, random.Next(1, daysInMonth + 1));
                if (date < window.Partner.StartDate || date > window.LastDay) continue;

                var amount = amountBase * (0.55 + random.NextDouble() * 0.95);
                target.Add(new Transaction {
                    Id = nextId++,
                    PartnerId = window.Partner.Id,
                    Date = date,
                    Type = type,
                    Amount = Math.Round((decimal)amount / 100m) * 100m
                });
            }
            return nextId;
        }

        /// <summary>Onay akışındaki talepler. En yeniler beklemede, eskiler sonuçlanmış.</summary>
        private static List<ApprovalRequest> BuildApprovals(List<Partner> partners) {
            var random = new Random(Seed + 7);
            var today = DateOnly.FromDateTime(DateTime.Today);
            var active = partners.Where(p => p.Status == PartnershipStatus.Active).ToList();
            var departments = DepartmentShares.Select(d => d.Name).ToArray();

            var result = new List<ApprovalRequest>();
            for (var i = 0; i < 9; i++) {
                var partner = active[random.Next(active.Count)];
                var daysAgo = i * 2 + random.Next(0, 2);
                var requestedOn = today.AddDays(-daysAgo);

                // İlk dört talep henüz karara bağlanmadı; sonrakiler sonuçlanmış.
                var status = i < 4
                    ? ApprovalStatus.Pending
                    : (random.NextDouble() < 0.78 ? ApprovalStatus.Approved : ApprovalStatus.Rejected);

                result.Add(new ApprovalRequest {
                    RequestId = $"PR-{requestedOn.Year}-{(1200 + i * 37 + random.Next(0, 20)):0000}",
                    Vendor = partner.Name,
                    Department = departments[random.Next(departments.Length)],
                    Amount = Math.Round((decimal)(random.NextDouble() * 1_450_000 + 85_000) / 100m) * 100m,
                    Status = status,
                    RequestedOn = requestedOn,
                    // Beş günden uzun süredir bekleyen talepler acil sayılır.
                    IsUrgent = status == ApprovalStatus.Pending && daysAgo >= 5
                });
            }

            return result.OrderByDescending(r => r.RequestedOn).ToList();
        }

        /// <summary>Aktif tedarikçilerin teslim ve kalite karnesi.</summary>
        private static List<VendorScore> BuildVendorScores(
            List<Partner> partners, List<Transaction> transactions) {

            var random = new Random(Seed + 13);
            var orderCounts = transactions
                .Where(t => t.Type == TransactionType.Purchase)
                .GroupBy(t => t.PartnerId)
                .ToDictionary(g => g.Key, g => g.Count());

            return partners
                .Where(p => p.Status == PartnershipStatus.Active)
                .Select(p => new VendorScore {
                    Name = p.Name,
                    Sector = p.Sector,
                    OnTimeDeliveryPercent = Math.Round(78 + random.NextDouble() * 21, 1),
                    QualityScore = Math.Round(3.4 + random.NextDouble() * 1.6, 1),
                    OrderCount = orderCounts.TryGetValue(p.Id, out var count) ? count : 0
                })
                .OrderByDescending(v => v.OnTimeDeliveryPercent)
                .Take(5)
                .ToList();
        }
    }
}
