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
                _logger.LogError(ex, "Gösterge paneli verisi Entity Framework Core ile okunamadı.");
                return new DashboardViewModel
                {
                    State = DataState.Failed,
                    StateMessage = "Veritabanına ulaşılamadı ya da sorgu çalıştırılamadı. EF Core veritabanı bağlantısı kontrol edilmeli.",
                    StateDetail = ex.Message
                };
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
                    _context.Category1,
                    i => i.Category1ID,
                    c1 => c1.Category1ID,
                    (i, cGroup) => new { i, cGroup }
                )
                .SelectMany(
                    x => x.cGroup.DefaultIfEmpty(),
                    (x, c1) => new
                    {
                        CategoryName = c1 != null ? c1.Category1Name : "Kategorisiz",
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
    }
}
