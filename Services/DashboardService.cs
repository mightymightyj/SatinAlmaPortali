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
            var (categoryTree, allCategories) = await GetCategoryTreeAsync(cancellationToken);

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
                Brands = brands,
                CategoryTree = categoryTree,
                AllCategories = allCategories
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

        private async Task<(IReadOnlyList<CategoryTreeItemDto> Tree, IReadOnlyList<CategoryTreeItemDto> All)> GetCategoryTreeAsync(CancellationToken cancellationToken)
        {
            var categories = await _context.Category.AsNoTracking().ToListAsync(cancellationToken);
            
            var dict = categories.ToDictionary(c => c.CategoryID, c => c.CategoryName ?? "");
            var existingIds = new HashSet<int>(categories.Select(c => c.CategoryID));

            var allDtos = categories.Select(c => new CategoryTreeItemDto
            {
                CategoryID = c.CategoryID,
                CategoryName = c.CategoryName ?? "Kategorisiz",
                ParentCategoryID = c.ParentCategoryID,
                ParentCategoryName = c.ParentCategoryID.HasValue && dict.TryGetValue(c.ParentCategoryID.Value, out var pName) ? pName : null
            }).ToList();

            var lookup = allDtos.ToLookup(c => c.ParentCategoryID);

            void PopulateChildrenRecursive(CategoryTreeItemDto item)
            {
                var children = lookup[item.CategoryID].ToList();
                item.SubCategories = children;
                foreach (var child in children)
                {
                    PopulateChildrenRecursive(child);
                }
            }

            // Kök kategoriler: ParentCategoryID null, 0 veya veritabanında karşılığı olmayan üst kategoriler
            var roots = allDtos.Where(c => !c.ParentCategoryID.HasValue || c.ParentCategoryID == 0 || !existingIds.Contains(c.ParentCategoryID.Value)).ToList();

            foreach (var root in roots)
            {
                PopulateChildrenRecursive(root);
            }

            return (roots, allDtos);
        }

        public async Task<Category> CreateCategoryAsync(string categoryName, int? parentCategoryId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                throw new ArgumentException("Kategori adı boş olamaz.", nameof(categoryName));
            }

            if (parentCategoryId.HasValue && parentCategoryId.Value <= 0)
            {
                parentCategoryId = null;
            }

            var newCategory = new Category
            {
                CategoryName = categoryName.Trim(),
                ParentCategoryID = parentCategoryId
            };

            _context.Category.Add(newCategory);
            await _context.SaveChangesAsync(cancellationToken);

            // Yeni kategori eklendiğinde dashboard önbelleğini temizle (Anında listede gözükmesi için)
            for (int y = 2020; y <= DateTime.Today.Year + 5; y++)
            {
                _cache.Remove($"dashboard_data_{y}");
                _cache.Remove($"buyer_dashboard_data_{y}");
                _cache.Remove($"supplier_dashboard_data_{y}");
            }

            return newCategory;
        }

        // =========================================================================
        // ALICI (BUYER) DASHBOARD SERVİS METODU
        // =========================================================================
        public async Task<BuyerDashboardViewModel> GetBuyerDashboardAsync(int? requestedYear, CancellationToken cancellationToken = default)
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

                if (!years.Contains(selectedYear))
                {
                    years = new List<int> { selectedYear, selectedYear - 1 };
                }

                // Gerçek DB talepleri
                var dbRequests = await _context.Requests
                    .AsNoTracking()
                    .Where(r => !r.RequestDate.HasValue || r.RequestDate.Value.Year == selectedYear)
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync(cancellationToken);

                var recentRequests = new List<BuyerRequestItemDto>();
                int reqIndex = 1;
                foreach (var req in dbRequests)
                {
                    var status = !string.IsNullOrWhiteSpace(req.Status) ? req.Status : "Açık";
                    recentRequests.Add(new BuyerRequestItemDto
                    {
                        RequestCode = $"REQ-{selectedYear}-{(req.RequestID > 0 ? req.RequestID.ToString("D3") : reqIndex.ToString("D3"))}",
                        ProductName = !string.IsNullOrWhiteSpace(req.ProductName) ? req.ProductName : (!string.IsNullOrWhiteSpace(req.RequestType) ? req.RequestType : "Genel Endüstriyel Malzeme"),
                        CategoryName = !string.IsNullOrWhiteSpace(req.RequestType) ? req.RequestType : "Satın Alma",
                        ReceivedBidsCount = (req.RequestID > 0 ? req.RequestID : 1) % 4 + 1,
                        RequestDate = req.RequestDate ?? DateTime.Today.AddDays(-reqIndex * 2),
                        Deadline = (req.RequestDate ?? DateTime.Today).AddDays(14),
                        EstimatedBudget = ((req.Quantity ?? 10) * 1250m) + (reqIndex * 8500m),
                        Status = status,
                        Priority = (reqIndex % 3 == 0) ? "Yüksek" : "Normal",
                        BuyerNotes = req.Notes
                    });
                    reqIndex++;
                }

                // Eğer DB'de yeterli kayıt yoksa zengin örnek taleplerle tamamla
                if (recentRequests.Count < 5)
                {
                    var sampleBuyerRequests = new List<BuyerRequestItemDto>
                    {
                        new BuyerRequestItemDto
                        {
                            RequestCode = $"REQ-{selectedYear}-089",
                            ProductName = "Endüstriyel Rulman & Lineer Mil Seti",
                            CategoryName = "Makine & Yedek Parça",
                            ReceivedBidsCount = 4,
                            RequestDate = DateTime.Today.AddDays(-2),
                            Deadline = DateTime.Today.AddDays(5),
                            EstimatedBudget = 85000m,
                            Status = "Açık",
                            Priority = "Yüksek",
                            BuyerNotes = "ISO 9001 sertifikalı tedarikçiler tercih edilecektir."
                        },
                        new BuyerRequestItemDto
                        {
                            RequestCode = $"REQ-{selectedYear}-084",
                            ProductName = "Paslanmaz Çelik Flanş & Küresel Vana Grubu",
                            CategoryName = "Boru & Tesisat",
                            ReceivedBidsCount = 3,
                            RequestDate = DateTime.Today.AddDays(-4),
                            Deadline = DateTime.Today.AddDays(7),
                            EstimatedBudget = 142000m,
                            Status = "Beklemede",
                            Priority = "Normal",
                            BuyerNotes = "316L kalite paslanmaz malzeme test sertifikası gereklidir."
                        },
                        new BuyerRequestItemDto
                        {
                            RequestCode = $"REQ-{selectedYear}-079",
                            ProductName = "Pnömatik Yön Kontrol Valfleri ve Silindirler",
                            CategoryName = "Otomasyon & Pnömatik",
                            ReceivedBidsCount = 5,
                            RequestDate = DateTime.Today.AddDays(-8),
                            Deadline = DateTime.Today.AddDays(2),
                            EstimatedBudget = 64500m,
                            Status = "Kazanıldı",
                            Priority = "Yüksek",
                            BuyerNotes = "Teklif onaylandı, sözleşme aşamasında."
                        },
                        new BuyerRequestItemDto
                        {
                            RequestCode = $"REQ-{selectedYear}-073",
                            ProductName = "ESD Korumalı Elektronik Montaj Masaları",
                            CategoryName = "Fabrika & Atölye Donanımı",
                            ReceivedBidsCount = 2,
                            RequestDate = DateTime.Today.AddDays(-12),
                            Deadline = DateTime.Today.AddDays(-1),
                            EstimatedBudget = 118000m,
                            Status = "Onaylandı",
                            Priority = "Normal",
                            BuyerNotes = "Yönetim kurulu bütçe onayı verildi."
                        },
                        new BuyerRequestItemDto
                        {
                            RequestCode = $"REQ-{selectedYear}-068",
                            ProductName = "Endüstriyel PLC & Dokunmatik Operatör Paneli",
                            CategoryName = "Elektrik & Elektronik",
                            ReceivedBidsCount = 6,
                            RequestDate = DateTime.Today.AddDays(-15),
                            Deadline = DateTime.Today.AddDays(-3),
                            EstimatedBudget = 210000m,
                            Status = "Kazanıldı",
                            Priority = "Yüksek",
                            BuyerNotes = "Siemens S7-1500 uyumlu modeller seçildi."
                        }
                    };

                    foreach (var s in sampleBuyerRequests)
                    {
                        if (!recentRequests.Any(r => r.RequestCode == s.RequestCode))
                        {
                            recentRequests.Add(s);
                        }
                    }
                }

                // Harcama Trendi
                var spendTrend = new BuyerSpendTrendDto
                {
                    Months = new[] { "Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara" },
                    MonthlySpend = new[] { 85000m, 112000m, 94000m, 138000m, 115000m, 168000m, 185000m, 215000m, 142000m, 130000m, 155000m, 190000m },
                    MonthlyBudget = new[] { 120000m, 120000m, 120000m, 150000m, 150000m, 180000m, 180000m, 200000m, 180000m, 180000m, 180000m, 200000m },
                    MaxSpendValue = 250000m
                };

                // Kategori Bütçeleri
                var budgetCategories = new List<BuyerBudgetItemDto>
                {
                    new BuyerBudgetItemDto { CategoryName = "Makine & Yedek Parça", AllocatedBudget = 650000m, SpentAmount = 485000m },
                    new BuyerBudgetItemDto { CategoryName = "Hammadde & Kimyasallar", AllocatedBudget = 500000m, SpentAmount = 390000m },
                    new BuyerBudgetItemDto { CategoryName = "Otomasyon & Elektrik", AllocatedBudget = 400000m, SpentAmount = 275000m },
                    new BuyerBudgetItemDto { CategoryName = "Fabrika & Donanım", AllocatedBudget = 300000m, SpentAmount = 180000m },
                    new BuyerBudgetItemDto { CategoryName = "Lojistik & Sarf Malzeme", AllocatedBudget = 200000m, SpentAmount = 90500m }
                };

                // Tedarikçi Karnesi
                var topSuppliers = new List<BuyerSupplierScoreDto>
                {
                    new BuyerSupplierScoreDto { CompanyId = 1, CompanyName = "Özkan Endüstri Makina A.Ş.", Category = "Mekanik & Talaşlı İmalat", Score = 4.9m, ReviewCount = 28, OnTimeRate = 98.5m, CompletedOrders = 34 },
                    new BuyerSupplierScoreDto { CompanyId = 2, CompanyName = "Atlas Pnömatik & Otomasyon Ltd.", Category = "Otomasyon Sistemleri", Score = 4.8m, ReviewCount = 19, OnTimeRate = 96.0m, CompletedOrders = 22 },
                    new BuyerSupplierScoreDto { CompanyId = 3, CompanyName = "Metalsan Paslanmaz Çelik Sanayi", Category = "Boru, Profil & Flanş", Score = 4.7m, ReviewCount = 15, OnTimeRate = 94.2m, CompletedOrders = 18 },
                    new BuyerSupplierScoreDto { CompanyId = 4, CompanyName = "Teknolink Elektronik Sistemler", Category = "Sensör & Kontrol", Score = 4.6m, ReviewCount = 12, OnTimeRate = 91.8m, CompletedOrders = 14 }
                };

                // KPI Özeti
                var summary = new BuyerSummaryDto
                {
                    TotalActiveRequests = recentRequests.Count(r => r.Status == "Açık" || r.Status == "Beklemede"),
                    RequestsThisWeek = 5,
                    RequestChangePercent = 18.2m,
                    PendingApprovals = recentRequests.Count(r => r.Status == "Beklemede"),
                    UrgentApprovals = 2,
                    CurrentPeriodSpend = spendTrend.MonthlySpend.Sum(),
                    SpendChangePercent = 12.4m,
                    SupplierPerformanceScore = 4.8m,
                    TotalReviews = 48,
                    OnTimeDeliveryRate = 96.4m
                };

                return new BuyerDashboardViewModel
                {
                    State = DataState.Ok,
                    SelectedYear = selectedYear,
                    AvailableYears = years,
                    Summary = summary,
                    SpendTrend = spendTrend,
                    RecentRequests = recentRequests,
                    BudgetCategories = budgetCategories,
                    TopSuppliers = topSuppliers
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Alıcı dashboard verisi yüklenirken hata oluştu.");
                return new BuyerDashboardViewModel { State = DataState.Failed };
            }
        }

        // =========================================================================
        // TEDARİKÇİ (SUPPLIER) DASHBOARD SERVİS METODU
        // =========================================================================
        public async Task<SupplierDashboardViewModel> GetSupplierDashboardAsync(int? requestedYear, CancellationToken cancellationToken = default)
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

                if (!years.Contains(selectedYear))
                {
                    years = new List<int> { selectedYear, selectedYear - 1 };
                }

                // Açık RFQ Listesi
                var openRfqs = new List<SupplierRfqItemDto>();

                // Gerçek DB Talepleri (Açık RFQ'lar)
                var dbRequests = await _context.Requests
                    .AsNoTracking()
                    .Where(r => !r.RequestDate.HasValue || r.RequestDate.Value.Year == selectedYear)
                    .OrderByDescending(r => r.RequestDate)
                    .Take(10)
                    .ToListAsync(cancellationToken);

                int rfqIdx = 1;
                foreach (var req in dbRequests)
                {
                    var rfqStatus = !string.IsNullOrWhiteSpace(req.Status) ? req.Status : "Cevap Bekliyor";
                    var daysLeft = Math.Max(1, (int)((req.RequestDate ?? DateTime.Today).AddDays(14) - DateTime.Today).TotalDays);
                    openRfqs.Add(new SupplierRfqItemDto
                    {
                        RfqCode = $"RFQ-{selectedYear}-{(req.RequestID > 0 ? req.RequestID.ToString("D3") : rfqIdx.ToString("D3"))}",
                        BuyerCompany = !string.IsNullOrWhiteSpace(req.BuyerInfo) ? req.BuyerInfo : "Kurumsal Alıcı A.Ş.",
                        ProductName = !string.IsNullOrWhiteSpace(req.ProductName) ? req.ProductName : (!string.IsNullOrWhiteSpace(req.RequestType) ? req.RequestType : "Endüstriyel Ürün Talebi"),
                        Category = !string.IsNullOrWhiteSpace(req.RequestType) ? req.RequestType : "Genel Tedarik",
                        Quantity = req.Quantity ?? 10,
                        Unit = "Adet",
                        Deadline = (req.RequestDate ?? DateTime.Today).AddDays(14),
                        DaysLeft = daysLeft,
                        BudgetEstimate = ((req.Quantity ?? 10) * 1500m) + (rfqIdx * 12000m),
                        Status = rfqStatus,
                        HasMyBid = rfqIdx % 2 == 0,
                        MyBidAmount = rfqIdx % 2 == 0 ? ((req.Quantity ?? 10) * 1400m) : null
                    });
                    rfqIdx++;
                }

                // Yeterli RFQ yoksa örnek verilerle zenginleştir
                if (openRfqs.Count < 6)
                {
                    var sampleRfqs = new List<SupplierRfqItemDto>
                    {
                        new SupplierRfqItemDto
                        {
                            RfqCode = $"RFQ-{selectedYear}-104",
                            BuyerCompany = "Aselsan Tedarik & Savunma A.Ş.",
                            ProductName = "CNC Freze Takım Seti & Karbür Freze Uçları",
                            Category = "Kesici Takımlar & Uçlar",
                            Quantity = 50,
                            Unit = "Set",
                            Deadline = DateTime.Today.AddDays(2),
                            DaysLeft = 2,
                            BudgetEstimate = 125000m,
                            Status = "Cevap Bekliyor",
                            HasMyBid = false,
                            MyBidAmount = null
                        },
                        new SupplierRfqItemDto
                        {
                            RfqCode = $"RFQ-{selectedYear}-098",
                            BuyerCompany = "Borusan Mannesmann Boru Yatırımları",
                            ProductName = "Paslanmaz Çelik Flanş & Küresel Vana Grubu",
                            Category = "Boru & Vana Ekipmanı",
                            Quantity = 120,
                            Unit = "Adet",
                            Deadline = DateTime.Today.AddDays(4),
                            DaysLeft = 4,
                            BudgetEstimate = 145000m,
                            Status = "Teklif Verildi",
                            HasMyBid = true,
                            MyBidAmount = 138500m
                        },
                        new SupplierRfqItemDto
                        {
                            RfqCode = $"RFQ-{selectedYear}-092",
                            BuyerCompany = "Tofaş Türk Otomobil Fabrikası A.Ş.",
                            ProductName = "Pnömatik Yön Valfleri ve ISO Standart Silindirler",
                            Category = "Pnömatik & Otomasyon",
                            Quantity = 200,
                            Unit = "Adet",
                            Deadline = DateTime.Today.AddDays(1),
                            DaysLeft = 1,
                            BudgetEstimate = 98000m,
                            Status = "Cevap Bekliyor",
                            HasMyBid = false,
                            MyBidAmount = null
                        },
                        new SupplierRfqItemDto
                        {
                            RfqCode = $"RFQ-{selectedYear}-088",
                            BuyerCompany = "Vestel Elektronik Sanayi ve Ticaret A.Ş.",
                            ProductName = "ESD Korumalı Çalışma Tezgahları & Montaj Aksamı",
                            Category = "Endüstriyel Masalar",
                            Quantity = 35,
                            Unit = "Takım",
                            Deadline = DateTime.Today.AddDays(6),
                            DaysLeft = 6,
                            BudgetEstimate = 112000m,
                            Status = "İncelemede",
                            HasMyBid = true,
                            MyBidAmount = 104000m
                        },
                        new SupplierRfqItemDto
                        {
                            RfqCode = $"RFQ-{selectedYear}-081",
                            BuyerCompany = "Kalyon Güneş Teknolojileri Üretim A.Ş.",
                            ProductName = "Endüstriyel DC Konnektörler ve Kablo Kanalları",
                            Category = "Elektrik & Enerji",
                            Quantity = 500,
                            Unit = "Adet",
                            Deadline = DateTime.Today.AddDays(3),
                            DaysLeft = 3,
                            BudgetEstimate = 78000m,
                            Status = "Teklif Verildi",
                            HasMyBid = true,
                            MyBidAmount = 72500m
                        },
                        new SupplierRfqItemDto
                        {
                            RfqCode = $"RFQ-{selectedYear}-077",
                            BuyerCompany = "Ford Otosan A.Ş. Gölcük Fabrikası",
                            ProductName = "Ağır Yük Lineer Kızak ve Vidalı Mil Mekanizması",
                            Category = "Lineer Hareket Sistemleri",
                            Quantity = 24,
                            Unit = "Set",
                            Deadline = DateTime.Today.AddDays(5),
                            DaysLeft = 5,
                            BudgetEstimate = 160000m,
                            Status = "Cevap Bekliyor",
                            HasMyBid = false,
                            MyBidAmount = null
                        }
                    };

                    foreach (var s in sampleRfqs)
                    {
                        if (!openRfqs.Any(r => r.RfqCode == s.RfqCode))
                        {
                            openRfqs.Add(s);
                        }
                    }
                }

                // Kazanç Trendi
                var earningsTrend = new SupplierEarningsTrendDto
                {
                    Months = new[] { "Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara" },
                    MonthlyEarnings = new[] { 140000m, 165000m, 130000m, 195000m, 180000m, 240000m, 210000m, 290000m, 260000m, 220000m, 245000m, 280000m },
                    MonthlyPendingPipeline = new[] { 45000m, 60000m, 80000m, 75000m, 90000m, 110000m, 95000m, 130000m, 115000m, 105000m, 120000m, 140000m },
                    MaxEarningsValue = 350000m
                };

                // İlgilenmeniz Gerekenler (Action Items)
                var actionItems = new List<SupplierActionItemDto>
                {
                    new SupplierActionItemDto
                    {
                        Id = "act-1",
                        Type = "pricing",
                        Title = "Fiyatlandırılacak 2 Ürün",
                        Description = "Müşteri teklif sepetinde bekleyen 2 ürününüz için birim fiyat güncellemesi gerekiyor.",
                        BadgeText = "2 Ürün Bekliyor",
                        BadgeType = "amber",
                        ActionUrl = "#",
                        ActionText = "Fiyat Gir"
                    },
                    new SupplierActionItemDto
                    {
                        Id = "act-2",
                        Type = "stock",
                        Title = "1 Eksik Stok Bildirimi",
                        Description = "'Lineer Rulman 25mm' stoğu kritik seviyeye (2 adet) düştü. Sipariş kaybetmemek için stok ekleyin.",
                        BadgeText = "Kritik Stok",
                        BadgeType = "danger",
                        ActionUrl = "#",
                        ActionText = "Stok Güncelle"
                    },
                    new SupplierActionItemDto
                    {
                        Id = "act-3",
                        Type = "urgent_rfq",
                        Title = "3 Süresi Dolmak Üzere Olan RFQ",
                        Description = "Tofaş ve Aselsan taleplerine bugün 18:00'e kadar teklif sunmanız gerekmektedir.",
                        BadgeText = "Son 24 Saat",
                        BadgeType = "amber",
                        ActionUrl = "#",
                        ActionText = "Teklifleri İncele"
                    },
                    new SupplierActionItemDto
                    {
                        Id = "act-4",
                        Type = "review",
                        Title = "2 Yeni Müşteri Değerlendirmesi",
                        Description = "Son tamamlanan 'Borusan' ve 'Vestel' teslimatlarınız için 5 yıldızlı olumlu yorumlar eklendi.",
                        BadgeText = "+5.0 ★ Puan",
                        BadgeType = "green",
                        ActionUrl = "#",
                        ActionText = "Yorumları Gör"
                    }
                };

                // Örnek ve Gerçek Katalog
                var catalogItems = new List<SupplierCatalogItemDto>();

                // Gerçek DB Ürünleri
                var dbItems = await _context.Items
                    .AsNoTracking()
                    .Take(10)
                    .ToListAsync(cancellationToken);

                if (dbItems.Any())
                {
                    var catIds = dbItems.Where(i => i.CategoryID.HasValue).Select(i => i.CategoryID!.Value).Distinct().ToList();
                    var catMap = await _context.Category
                        .Where(c => catIds.Contains(c.CategoryID))
                        .ToDictionaryAsync(c => c.CategoryID, c => c.CategoryName ?? "Genel", cancellationToken);

                    foreach (var it in dbItems)
                    {
                        string catName = it.CategoryID.HasValue && catMap.TryGetValue(it.CategoryID.Value, out var cN) ? cN : "Endüstriyel Malzeme";
                        catalogItems.Add(new SupplierCatalogItemDto
                        {
                            ItemId = it.ItemID,
                            ItemName = it.ItemName ?? "Katalog Ürünü",
                            Sku = $"SKU-{it.ItemID:D4}",
                            Category = catName,
                            Stock = it.Quantity ?? 50,
                            UnitPrice = (decimal)(it.ItemPrice ?? 100),
                            Status = (it.Quantity ?? 50) <= 5 ? "Kritik" : "Stokta"
                        });
                    }
                }

                if (catalogItems.Count < 4)
                {
                    var sampleCatalog = new List<SupplierCatalogItemDto>
                    {
                        new SupplierCatalogItemDto { ItemId = 1, ItemName = "Endüstriyel Rulman 6204-2RS", Sku = "SKU-RLM-6204", Category = "Rulman & Yatak", Stock = 450, UnitPrice = 185m, Status = "Stokta" },
                        new SupplierCatalogItemDto { ItemId = 2, ItemName = "Lineer Kızak ve Araba Seti HGR25", Sku = "SKU-LIN-HGR25", Category = "Lineer Sistemler", Stock = 18, UnitPrice = 1450m, Status = "Stokta" },
                        new SupplierCatalogItemDto { ItemId = 3, ItemName = "Pnömatik 5/2 Yön Kontrol Valfi", Sku = "SKU-PNO-52V", Category = "Pnömatik", Stock = 85, UnitPrice = 640m, Status = "Stokta" },
                        new SupplierCatalogItemDto { ItemId = 4, ItemName = "Karbür Parmak Freze 10mm 4 Ağız", Sku = "SKU-FRZ-10C", Category = "Kesici Takımlar", Stock = 2, UnitPrice = 320m, Status = "Kritik" }
                    };

                    foreach (var cat in sampleCatalog)
                    {
                        if (!catalogItems.Any(c => c.ItemId == cat.ItemId))
                        {
                            catalogItems.Add(cat);
                        }
                    }
                }

                var summary = new SupplierSummaryDto
                {
                    NewIncomingRfqs = openRfqs.Count,
                    NewRfqsThisWeek = Math.Min(5, openRfqs.Count),
                    RfqsAwaitingResponse = openRfqs.Count(r => r.Status == "Cevap Bekliyor"),
                    ActiveBids = openRfqs.Count(r => r.HasMyBid),
                    ActiveBidsValue = openRfqs.Where(r => r.HasMyBid).Sum(r => r.MyBidAmount ?? 0),
                    WonAuctions = 34,
                    WinRatePercent = 68.4m,
                    WonTotalRevenue = 2190000m,
                    CatalogStockValue = catalogItems.Sum(c => c.UnitPrice * c.Stock),
                    ActiveSkuCount = catalogItems.Count > 0 ? catalogItems.Count * 35 : 142,
                    LowStockAlertCount = catalogItems.Count(c => c.Status == "Kritik")
                };

                return new SupplierDashboardViewModel
                {
                    State = DataState.Ok,
                    SelectedYear = selectedYear,
                    AvailableYears = years,
                    Summary = summary,
                    OpenRfqs = openRfqs,
                    EarningsTrend = earningsTrend,
                    ActionItems = actionItems,
                    FeaturedProducts = catalogItems
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tedarikçi dashboard verisi yüklenirken hata oluştu.");
                return new SupplierDashboardViewModel { State = DataState.Failed };
            }
        }
    }
}
