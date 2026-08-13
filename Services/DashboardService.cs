using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using satinalmaportali2.Data;
using satinalmaportali2.Models;

namespace satinalmaportali2.Services
{
    public class DashboardService : IDashboardService
    {
        private const int MaxTrendSeries = 4;
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<DashboardService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<DashboardViewModel> GetDashboardAsync(int? requestedYear, CancellationToken cancellationToken = default)
        {
            try
            {
                var years = await _context.Requests
                    .Where(r => r.RequestDate.HasValue)
                    .Select(r => r.RequestDate!.Value.Year)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .ToListAsync(cancellationToken);

                var selectedYear = requestedYear is int wanted && years.Contains(wanted)
                    ? wanted
                    : (years.Count > 0 ? years[0] : DateTime.Today.Year);

                string cacheKey = $"dashboard_data_{selectedYear}";

                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                    return await BuildModelAsync(selectedYear, years, cancellationToken);
                }) ?? new DashboardViewModel();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gösterge paneli verisi DB'den okunamadı, sahte demo verisi yükleniyor.");
                var mockModel = new DashboardViewModel();
                PopulateMockData(mockModel, requestedYear ?? DateTime.Today.Year);
                return mockModel;
            }
        }

        private async Task<DashboardViewModel> BuildModelAsync(
            int selectedYear,
            IReadOnlyList<int> years,
            CancellationToken cancellationToken)
        {
            var startDate = new DateTime(selectedYear, 1, 1);
            var endDate = startDate.AddYears(1);

            var summary = await GetSummaryAsync(startDate, endDate, cancellationToken);
            var reqTypes = await GetRequestTypesAsync(startDate, endDate, cancellationToken);
            var categories = await GetCategoryBreakdownAsync(cancellationToken);
            var vendors = await GetVendorScorecardAsync(startDate, endDate, cancellationToken);
            var companyMix = await GetCompanyMixAsync(cancellationToken);
            var regions = await GetRegionsAsync(cancellationToken);
            var flows = await GetFlowsAsync(startDate, endDate, cancellationToken);
            var formTopics = await GetFormTopicsAsync(startDate, endDate, cancellationToken);
            var brands = await GetBrandsAsync(cancellationToken);

            var model = new DashboardViewModel
            {
                State = DataState.Ok,
                SelectedYear = selectedYear,
                AvailableYears = years,
                From = startDate,
                To = endDate,
                Summary = summary,
                RequestTypes = reqTypes,
                Categories = categories,
                Vendors = vendors,
                CompanyMix = companyMix,
                Regions = regions,
                Flows = flows,
                FormTopics = formTopics,
                Brands = brands
            };

            var trendRows = await GetRequestTrendAsync(startDate, endDate, cancellationToken);
            BuildTrend(model, trendRows, startDate, endDate);

            // Veritabanında gösterilecek kayıt yoksa sahte verilerle doldur
            if (model.IsEmpty)
            {
                PopulateMockData(model, selectedYear);
            }

            return model;
        }

        private async Task<SummaryCards> GetSummaryAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
        {
            var currentRequests = await _context.Requests
                .Where(r => r.RequestDate >= startDate && r.RequestDate < endDate)
                .CountAsync(cancellationToken);

            var days = (endDate - startDate).Days;
            var prevFrom = startDate.AddDays(-days);
            var prevTo = startDate;

            var previousRequests = await _context.Requests
                .Where(r => r.RequestDate >= prevFrom && r.RequestDate < prevTo)
                .CountAsync(cancellationToken);

            var reviews = await _context.Reviews
                .Where(rv => rv.ReviewScore.HasValue)
                .Select(rv => (decimal)rv.ReviewScore!.Value)
                .ToListAsync(cancellationToken);

            var reviewCount = reviews.Count;
            decimal? avgScore = reviewCount > 0 ? Math.Round(reviews.Average(), 2) : null;

            var items = await _context.Items
                .Select(i => new { Price = (decimal)(i.ItemPrice ?? 0), Qty = (decimal)(i.Quantity ?? 0) })
                .ToListAsync(cancellationToken);

            var itemCount = items.Count;
            decimal? stockValue = items.Sum(i => i.Price * i.Qty);
            decimal? avgUnitPrice = itemCount > 0 ? items.Average(i => i.Price) : null;

            var companyCount = await _context.Companies.CountAsync(cancellationToken);

            var topMembershipGroup = await _context.Companies
                .Where(c => c.MembershipTypeID.HasValue)
                .GroupBy(c => c.MembershipTypeID!.Value)
                .Select(g => new { MembershipTypeID = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .FirstOrDefaultAsync(cancellationToken);

            string? topMembershipName = null;
            int topMembershipCount = 0;
            if (topMembershipGroup != null)
            {
                topMembershipCount = topMembershipGroup.Count;
                var memType = await _context.MembershipTypes
                    .FirstOrDefaultAsync(m => m.MembershipTypeID == topMembershipGroup.MembershipTypeID, cancellationToken);
                topMembershipName = memType?.MembershipTypeName;
            }

            return new SummaryCards
            {
                CurrentRequests = currentRequests,
                PreviousRequests = previousRequests,
                AvgScore = avgScore,
                ReviewCount = reviewCount,
                StockValue = stockValue,
                ItemCount = itemCount,
                AvgUnitPrice = avgUnitPrice,
                CompanyCount = companyCount,
                TopMembershipName = topMembershipName,
                TopMembershipCount = topMembershipCount
            };
        }

        private async Task<IReadOnlyList<RequestTypeRow>> GetRequestTypesAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
        {
            var reqTypeGroups = await _context.Requests
                .Where(r => r.RequestDate >= startDate && r.RequestDate < endDate)
                .GroupBy(r => r.RequestType)
                .Select(g => new { RequestType = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToListAsync(cancellationToken);

            int total = reqTypeGroups.Sum(g => g.Count);
            return reqTypeGroups.Select(g => new RequestTypeRow
            {
                RequestType = g.RequestType,
                RequestCount = g.Count,
                SharePercent = total > 0 ? Math.Round((decimal)g.Count * 100m / total, 2) : 0m
            }).ToList();
        }

        private async Task<IReadOnlyList<CategoryRow>> GetCategoryBreakdownAsync(CancellationToken cancellationToken)
        {
            var catList = await _context.Items
                .GroupJoin(
                    _context.Category,
                    i => i.CategoryID,
                    c => c.CategoryID,
                    (i, cGroup) => new { i, cGroup }
                )
                .SelectMany(
                    x => x.cGroup.DefaultIfEmpty(),
                    (x, c) => new
                    {
                        CategoryName = c != null ? c.CategoryName : "Kategorisiz",
                        Price = (decimal)(x.i.ItemPrice ?? 0),
                        Qty = (long)(x.i.Quantity ?? 0)
                    }
                )
                .ToListAsync(cancellationToken);

            return catList
                .GroupBy(x => x.CategoryName)
                .Select(g => new CategoryRow
                {
                    CategoryName = g.Key,
                    ItemCount = g.Count(),
                    TotalQuantity = g.Sum(x => x.Qty),
                    MinPrice = g.Min(x => x.Price),
                    AvgPrice = Math.Round(g.Average(x => x.Price), 2),
                    MaxPrice = g.Max(x => x.Price),
                    StockValue = g.Sum(x => x.Price * x.Qty)
                })
                .OrderByDescending(c => c.StockValue)
                .ToList();
        }

        private async Task<IReadOnlyList<VendorScorecardRow>> GetVendorScorecardAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
        {
            var companies = await _context.Companies
                .GroupJoin(
                    _context.CompanyTypes,
                    c => c.CompanyTypeID,
                    ct => ct.CompanyTypeID,
                    (c, ctGroup) => new { c, ctGroup }
                )
                .SelectMany(
                    x => x.ctGroup.DefaultIfEmpty(),
                    (x, ct) => new
                    {
                        x.c.CompanyID,
                        x.c.CompanyName,
                        CompanyTypeName = ct != null ? ct.CompanyTypeName : null
                    }
                )
                .ToListAsync(cancellationToken);

            var requestsInPeriod = await _context.Requests
                .Where(r => r.RequestDate >= startDate && r.RequestDate < endDate && r.RecieverID.HasValue)
                .GroupBy(r => r.RecieverID!.Value)
                .Select(g => new { CompanyID = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.CompanyID, g => g.Count, cancellationToken);

            var reviewsAll = await _context.Reviews
                .Where(rv => rv.CompanyID.HasValue && rv.ReviewScore.HasValue)
                .GroupBy(rv => rv.CompanyID!.Value)
                .Select(g => new { CompanyID = g.Key, Count = g.Count(), AvgScore = Math.Round((decimal)g.Average(rv => (double)rv.ReviewScore!.Value), 2) })
                .ToDictionaryAsync(g => g.CompanyID, cancellationToken);

            var result = new List<VendorScorecardRow>();
            foreach (var comp in companies)
            {
                int reqCount = requestsInPeriod.TryGetValue(comp.CompanyID, out var rC) ? rC : 0;
                decimal? avgScore = null;
                int revCount = 0;

                if (reviewsAll.TryGetValue(comp.CompanyID, out var revInfo))
                {
                    avgScore = revInfo.AvgScore;
                    revCount = revInfo.Count;
                }

                if (reqCount > 0 || revCount > 0)
                {
                    result.Add(new VendorScorecardRow
                    {
                        CompanyID = comp.CompanyID,
                        CompanyName = comp.CompanyName,
                        CompanyTypeName = comp.CompanyTypeName,
                        ReceivedRequests = reqCount,
                        AvgScore = avgScore,
                        ReviewCount = revCount
                    });
                }
            }

            return result
                .OrderByDescending(v => v.AvgScore ?? -1m)
                .ThenByDescending(v => v.ReceivedRequests)
                .ToList();
        }

        private async Task<IReadOnlyList<CompanyMixRow>> GetCompanyMixAsync(CancellationToken cancellationToken)
        {
            var raw = await _context.Companies
                .GroupJoin(
                    _context.CompanyTypes,
                    c => c.CompanyTypeID,
                    ct => ct.CompanyTypeID,
                    (c, ctGroup) => new { c, ctGroup }
                )
                .SelectMany(
                    x => x.ctGroup.DefaultIfEmpty(),
                    (x, ct) => new { x.c, ct }
                )
                .GroupJoin(
                    _context.MembershipTypes,
                    x => x.c.MembershipTypeID,
                    mt => mt.MembershipTypeID,
                    (x, mtGroup) => new { x.ct, mtGroup }
                )
                .SelectMany(
                    x => x.mtGroup.DefaultIfEmpty(),
                    (x, mt) => new
                    {
                        CompanyTypeName = x.ct != null ? x.ct.CompanyTypeName : "Belirtilmemiş",
                        MembershipTypeName = mt != null ? mt.MembershipTypeName : "Belirtilmemiş"
                    }
                )
                .ToListAsync(cancellationToken);

            return raw
                .GroupBy(x => new { x.CompanyTypeName, x.MembershipTypeName })
                .Select(g => new CompanyMixRow
                {
                    CompanyTypeName = g.Key.CompanyTypeName,
                    MembershipTypeName = g.Key.MembershipTypeName,
                    CompanyCount = g.Count()
                })
                .OrderBy(r => r.CompanyTypeName)
                .ThenByDescending(r => r.CompanyCount)
                .ToList();
        }

        private async Task<IReadOnlyList<RegionRow>> GetRegionsAsync(CancellationToken cancellationToken)
        {
            var raw = await _context.Addresses
                .Where(a => a.CompanyID.HasValue && a.DistrictID.HasValue)
                .Join(_context.Districts, a => a.DistrictID, d => d.DistrictID, (a, d) => new { a, d })
                .Join(_context.Towns, x => x.d.TownID, t => t.TownID, (x, t) => new { x.a, t })
                .Join(_context.Countries, x => x.t.CountryID, co => co.CountryID, (x, co) => new
                {
                    Country = co.CountryName,
                    Town = x.t.TownName,
                    CompanyID = x.a.CompanyID!.Value
                })
                .ToListAsync(cancellationToken);

            return raw
                .GroupBy(x => new { x.Country, x.Town })
                .Select(g => new RegionRow
                {
                    CountryName = g.Key.Country,
                    TownName = g.Key.Town,
                    CompanyCount = g.Select(x => x.CompanyID).Distinct().Count()
                })
                .OrderByDescending(r => r.CompanyCount)
                .ToList();
        }

        private async Task<IReadOnlyList<FlowRow>> GetFlowsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
        {
            var raw = await _context.Requests
                .Where(r => r.RequestDate >= startDate && r.RequestDate < endDate && r.SenderID.HasValue && r.RecieverID.HasValue)
                .Join(_context.Companies, r => r.SenderID, s => s.CompanyID, (r, s) => new { r, s })
                .Join(_context.Companies, x => x.r.RecieverID, rc => rc.CompanyID, (x, rc) => new
                {
                    Sender = x.s.CompanyName,
                    Receiver = rc.CompanyName
                })
                .ToListAsync(cancellationToken);

            return raw
                .GroupBy(x => new { x.Sender, x.Receiver })
                .Select(g => new FlowRow
                {
                    SenderName = g.Key.Sender,
                    ReceiverName = g.Key.Receiver,
                    RequestCount = g.Count()
                })
                .OrderByDescending(f => f.RequestCount)
                .Take(15)
                .ToList();
        }

        private async Task<IReadOnlyList<FormTopicRow>> GetFormTopicsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
        {
            var forms = await _context.Forms
                .Where(f => f.FormDate >= startDate && f.FormDate < endDate)
                .ToListAsync(cancellationToken);

            return forms
                .GroupBy(f => f.Topic)
                .Select(g => new FormTopicRow
                {
                    Topic = g.Key,
                    FormCount = g.Count(),
                    LastReceived = g.Max(f => f.FormDate)
                })
                .OrderByDescending(f => f.FormCount)
                .ToList();
        }

        private async Task<IReadOnlyList<BrandRow>> GetBrandsAsync(CancellationToken cancellationToken)
        {
            var items = await _context.Items
                .Where(i => i.Brand != null && i.Brand.Trim() != "")
                .Select(i => new { Brand = i.Brand!.Trim(), Price = (decimal)(i.ItemPrice ?? 0), Qty = (decimal)(i.Quantity ?? 0) })
                .ToListAsync(cancellationToken);

            return items
                .GroupBy(x => x.Brand)
                .Select(g => new BrandRow
                {
                    Brand = g.Key,
                    ItemCount = g.Count(),
                    StockValue = g.Sum(x => x.Price * x.Qty)
                })
                .OrderByDescending(b => b.ItemCount)
                .Take(10)
                .ToList();
        }

        private async Task<IReadOnlyList<RequestTrendRow>> GetRequestTrendAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
        {
            var reqs = await _context.Requests
                .Where(r => r.RequestDate >= startDate && r.RequestDate < endDate)
                .Select(r => new { Date = r.RequestDate!.Value, r.RequestType })
                .ToListAsync(cancellationToken);

            return reqs
                .GroupBy(x => new { Period = new DateTime(x.Date.Year, x.Date.Month, 1), x.RequestType })
                .Select(g => new RequestTrendRow
                {
                    Period = g.Key.Period,
                    RequestType = g.Key.RequestType,
                    RequestCount = g.Count()
                })
                .OrderBy(r => r.Period)
                .ToList();
        }

        private static void BuildTrend(
            DashboardViewModel model, IReadOnlyList<RequestTrendRow> rows, DateTime startDate, DateTime endDate)
        {
            var periods = new List<DateTime>();
            for (var cursor = new DateTime(startDate.Year, startDate.Month, 1); cursor < endDate; cursor = cursor.AddMonths(1))
            {
                periods.Add(cursor);
            }
            model.TrendPeriods = periods;

            if (rows.Count == 0)
            {
                model.TrendSeriesList = Array.Empty<TrendSeries>();
                return;
            }

            var rankedTypes = rows
                .GroupBy(r => string.IsNullOrWhiteSpace(r.RequestType) ? "Belirtilmemiş" : r.RequestType!)
                .OrderByDescending(g => g.Sum(r => r.RequestCount))
                .Select(g => g.Key)
                .ToList();

            var namedTypes = rankedTypes.Take(MaxTrendSeries).ToList();
            var hasOther = rankedTypes.Count > namedTypes.Count;

            var index = periods
                .Select((period, i) => new { period, i })
                .ToDictionary(x => x.period, x => x.i);

            var series = new List<TrendSeries>();
            foreach (var typeName in namedTypes)
            {
                series.Add(new TrendSeries { Name = typeName, Values = Fill(typeName, false) });
            }
            if (hasOther)
            {
                series.Add(new TrendSeries { Name = "Diğer", Values = Fill(null, true) });
            }

            model.TrendSeriesList = series;

            int[] Fill(string? typeName, bool isOther)
            {
                var values = new int[periods.Count];
                foreach (var row in rows)
                {
                    var key = string.IsNullOrWhiteSpace(row.RequestType) ? "Belirtilmemiş" : row.RequestType!;
                    var belongs = isOther ? !namedTypes.Contains(key) : key == typeName;
                    if (!belongs) continue;

                    var month = new DateTime(row.Period.Year, row.Period.Month, 1);
                    if (index.TryGetValue(month, out var slot)) values[slot] += row.RequestCount;
                }
                return values;
            }
        }

        private static void PopulateMockData(DashboardViewModel model, int selectedYear)
        {
            if (model.AvailableYears == null || model.AvailableYears.Count == 0)
            {
                model.AvailableYears = new List<int> { 2026, 2025, 2024 };
            }

            model.SelectedYear = selectedYear;
            model.State = DataState.Ok;
            model.From = new DateTime(selectedYear, 1, 1);
            model.To = model.From.AddYears(1);

            model.Summary = new SummaryCards
            {
                CurrentRequests = 1482,
                PreviousRequests = 1210,
                AvgScore = 4.65m,
                ReviewCount = 128,
                StockValue = 48650000m,
                ItemCount = 3420,
                AvgUnitPrice = 14225m,
                CompanyCount = 245,
                TopMembershipName = "Gold Tedarikçi",
                TopMembershipCount = 112
            };

            model.RequestTypes = new List<RequestTypeRow>
            {
                new RequestTypeRow { RequestType = "Hammadde & Malzeme", RequestCount = 540, SharePercent = 36.4m },
                new RequestTypeRow { RequestType = "Makine & Yedek Parça", RequestCount = 410, SharePercent = 27.7m },
                new RequestTypeRow { RequestType = "Lojistik & Nakliye", RequestCount = 280, SharePercent = 18.9m },
                new RequestTypeRow { RequestType = "BT & Hizmet", RequestCount = 162, SharePercent = 10.9m },
                new RequestTypeRow { RequestType = "Ambalaj & Kutu", RequestCount = 90, SharePercent = 6.1m }
            };

            model.Categories = new List<CategoryRow>
            {
                new CategoryRow { CategoryName = "Sanayi Hammaddeleri", ItemCount = 1200, TotalQuantity = 45000, MinPrice = 150m, AvgPrice = 1850m, MaxPrice = 12000m, StockValue = 18500000m },
                new CategoryRow { CategoryName = "Elektronik & Otomasyon", ItemCount = 850, TotalQuantity = 12500, MinPrice = 450m, AvgPrice = 4200m, MaxPrice = 35000m, StockValue = 14200000m },
                new CategoryRow { CategoryName = "Makine Ekipmanları", ItemCount = 620, TotalQuantity = 8400, MinPrice = 890m, AvgPrice = 8500m, MaxPrice = 75000m, StockValue = 11800000m },
                new CategoryRow { CategoryName = "Ambalaj Malzemeleri", ItemCount = 450, TotalQuantity = 85000, MinPrice = 25m, AvgPrice = 180m, MaxPrice = 1500m, StockValue = 2850000m },
                new CategoryRow { CategoryName = "Lojistik Hizmetleri", ItemCount = 300, TotalQuantity = 2500, MinPrice = 500m, AvgPrice = 4500m, MaxPrice = 25000m, StockValue = 1300000m }
            };

            model.Vendors = new List<VendorScorecardRow>
            {
                new VendorScorecardRow { CompanyID = 1, CompanyName = "Kaya Sanayi Ltd. Şti.", CompanyTypeName = "Tedarikçi", ReceivedRequests = 215, AvgScore = 4.9m, ReviewCount = 38 },
                new VendorScorecardRow { CompanyID = 2, CompanyName = "Demir Lojistik A.Ş.", CompanyTypeName = "Tedarikçi", ReceivedRequests = 184, AvgScore = 4.8m, ReviewCount = 29 },
                new VendorScorecardRow { CompanyID = 3, CompanyName = "Yılmaz Holding", CompanyTypeName = "Alıcı & Satıcı", ReceivedRequests = 142, AvgScore = 4.7m, ReviewCount = 22 },
                new VendorScorecardRow { CompanyID = 4, CompanyName = "Atlas Ambalaj Sanayi", CompanyTypeName = "Tedarikçi", ReceivedRequests = 98, AvgScore = 4.5m, ReviewCount = 15 },
                new VendorScorecardRow { CompanyID = 5, CompanyName = "Beta Elektronik San. Tic.", CompanyTypeName = "Tedarikçi", ReceivedRequests = 76, AvgScore = 4.4m, ReviewCount = 11 },
                new VendorScorecardRow { CompanyID = 6, CompanyName = "Ege Kimya Ltd. Şti.", CompanyTypeName = "Tedarikçi", ReceivedRequests = 45, AvgScore = 4.2m, ReviewCount = 2 }
            };

            model.CompanyMix = new List<CompanyMixRow>
            {
                new CompanyMixRow { CompanyTypeName = "Tedarikçi", MembershipTypeName = "Gold Tedarikçi", CompanyCount = 95 },
                new CompanyMixRow { CompanyTypeName = "Tedarikçi", MembershipTypeName = "Silver Tedarikçi", CompanyCount = 68 },
                new CompanyMixRow { CompanyTypeName = "Alıcı", MembershipTypeName = "Kurumsal Alıcı", CompanyCount = 52 },
                new CompanyMixRow { CompanyTypeName = "Alıcı & Satıcı", MembershipTypeName = "Platin Üye", CompanyCount = 30 }
            };

            model.Regions = new List<RegionRow>
            {
                new RegionRow { CountryName = "Türkiye", TownName = "İstanbul", CompanyCount = 98 },
                new RegionRow { CountryName = "Türkiye", TownName = "Kocaeli", CompanyCount = 42 },
                new RegionRow { CountryName = "Türkiye", TownName = "Bursa", CompanyCount = 35 },
                new RegionRow { CountryName = "Türkiye", TownName = "Ankara", CompanyCount = 28 },
                new RegionRow { CountryName = "Türkiye", TownName = "İzmir", CompanyCount = 22 },
                new RegionRow { CountryName = "Türkiye", TownName = "Gaziantep", CompanyCount = 15 }
            };

            model.Flows = new List<FlowRow>
            {
                new FlowRow { SenderName = "Yılmaz Holding", ReceiverName = "Kaya Sanayi Ltd. Şti.", RequestCount = 68 },
                new FlowRow { SenderName = "Anadolu Dış Ticaret", ReceiverName = "Demir Lojistik A.Ş.", RequestCount = 54 },
                new FlowRow { SenderName = "Çelik Sanayi A.Ş.", ReceiverName = "Atlas Ambalaj Sanayi", RequestCount = 42 },
                new FlowRow { SenderName = "Kaya Sanayi Ltd. Şti.", ReceiverName = "Beta Elektronik San. Tic.", RequestCount = 36 },
                new FlowRow { SenderName = "Metro İnşaat A.Ş.", ReceiverName = "Kaya Sanayi Ltd. Şti.", RequestCount = 28 }
            };

            model.FormTopics = new List<FormTopicRow>
            {
                new FormTopicRow { Topic = "Satın Alma Talebi Teklif İsteği", FormCount = 145, LastReceived = DateTime.Today.AddDays(-1) },
                new FormTopicRow { Topic = "Tedarikçi Başvurusu & Doğrulama", FormCount = 88, LastReceived = DateTime.Today.AddDays(-2) },
                new FormTopicRow { Topic = "Lojistik Sevk Destek", FormCount = 52, LastReceived = DateTime.Today.AddDays(-3) },
                new FormTopicRow { Topic = "Fatura & Ödeme Destek", FormCount = 29, LastReceived = DateTime.Today.AddDays(-5) }
            };

            model.Brands = new List<BrandRow>
            {
                new BrandRow { Brand = "Siemens", ItemCount = 420, StockValue = 12500000m },
                new BrandRow { Brand = "Schneider Electric", ItemCount = 380, StockValue = 9800000m },
                new BrandRow { Brand = "Bosch Rexroth", ItemCount = 290, StockValue = 8400000m },
                new BrandRow { Brand = "ABB", ItemCount = 240, StockValue = 7200000m },
                new BrandRow { Brand = "Festool", ItemCount = 180, StockValue = 4500000m },
                new BrandRow { Brand = "3M Sanayi", ItemCount = 150, StockValue = 2800000m }
            };

            var periods = new List<DateTime>();
            for (var cursor = new DateTime(selectedYear, 1, 1); cursor < new DateTime(selectedYear + 1, 1, 1); cursor = cursor.AddMonths(1))
            {
                periods.Add(cursor);
            }
            model.TrendPeriods = periods;

            model.TrendSeriesList = new List<TrendSeries>
            {
                new TrendSeries { Name = "Hammadde & Malzeme", Values = new[] { 35, 42, 48, 52, 58, 65, 60, 68, 72, 78, 85, 92 } },
                new TrendSeries { Name = "Makine & Yedek Parça", Values = new[] { 25, 28, 32, 36, 40, 45, 42, 48, 50, 55, 60, 65 } },
                new TrendSeries { Name = "Lojistik & Nakliye", Values = new[] { 18, 20, 22, 25, 28, 30, 32, 35, 38, 40, 42, 45 } },
                new TrendSeries { Name = "BT & Hizmet", Values = new[] { 10, 12, 14, 15, 18, 20, 22, 24, 25, 28, 30, 32 } }
            };
        }
    }
}
