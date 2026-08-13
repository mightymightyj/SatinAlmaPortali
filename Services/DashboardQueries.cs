using Dapper;
using Microsoft.Data.SqlClient;
using satinalmaportali2.Models;

namespace satinalmaportali2.Services {

    /// <summary>Panelin ihtiyaç duyduğu okumalar. Widget başına bir metot.</summary>
    public interface IDashboardQueries {
        /// <summary>Talep verisi bulunan yıllar — filtre şeridini besler.</summary>
        Task<IReadOnlyList<int>> GetAvailableYearsAsync();

        Task<SummaryCards> GetSummaryAsync(DashboardFilter filter);
        Task<IReadOnlyList<RequestTrendRow>> GetRequestTrendAsync(DashboardFilter filter);
        Task<IReadOnlyList<RequestTypeRow>> GetRequestTypesAsync(DashboardFilter filter);
        Task<IReadOnlyList<CategoryRow>> GetCategoryBreakdownAsync();
        Task<IReadOnlyList<VendorScorecardRow>> GetVendorScorecardAsync(DashboardFilter filter);
        Task<IReadOnlyList<CompanyMixRow>> GetCompanyMixAsync();
        Task<IReadOnlyList<RegionRow>> GetRegionsAsync();
        Task<IReadOnlyList<FlowRow>> GetFlowsAsync(DashboardFilter filter);
        Task<IReadOnlyList<FormTopicRow>> GetFormTopicsAsync(DashboardFilter filter);
        Task<IReadOnlyList<BrandRow>> GetBrandsAsync();
    }

    /// <summary>
    /// Mevcut şemadan SALT OKUNUR veri çeker. Bu sınıfta yalnızca SELECT vardır;
    /// hiçbir INSERT / UPDATE / DELETE / DDL ifadesi bulunmaz ve veri üretilmez.
    /// Şemaya yeni tablo veya sütun eklenmediği için sorgular yalnızca
    /// diyagramdaki sütunlara dayanır.
    /// </summary>
    public sealed class SqlDashboardQueries : IDashboardQueries {

        /// <summary>Düşük stok eşiği. Şemada böyle bir sütun yok — bu bir uygulama ayarıdır.</summary>
        public const int LowStockThreshold = 10;

        private readonly string _connectionString;

        public SqlDashboardQueries(string connectionString) {
            _connectionString = connectionString;
        }

        private SqlConnection Open() => new SqlConnection(_connectionString);

        // ---------------- Filtre şeridi ----------------

        private const string AvailableYearsSql = @"
SELECT DISTINCT YEAR(r.RequestDate) AS [Year]
FROM Request r
WHERE r.RequestDate IS NOT NULL
ORDER BY [Year] DESC;";

        public async Task<IReadOnlyList<int>> GetAvailableYearsAsync() {
            using var connection = Open();
            var rows = await connection.QueryAsync<int>(AvailableYearsSql);
            return rows.ToList();
        }

        // ---------------- K1–K4 ----------------

        private const string SummarySql = @"
SELECT
    (SELECT COUNT(*) FROM Request r
      WHERE r.RequestDate >= @From AND r.RequestDate < @To)                    AS CurrentRequests,
    (SELECT COUNT(*) FROM Request r
      WHERE r.RequestDate >= DATEADD(DAY, -DATEDIFF(DAY, @From, @To), @From)
        AND r.RequestDate <  @From)                                            AS PreviousRequests,

    (SELECT CAST(AVG(CAST(rv.ReviewScore AS decimal(9,4))) AS decimal(4,2))
       FROM Reviews rv)                                                        AS AvgScore,
    (SELECT COUNT(*) FROM Reviews)                                             AS ReviewCount,

    (SELECT SUM(CAST(i.ItemPrice AS decimal(18,2)) * i.Quantity) FROM Items i) AS StockValue,
    (SELECT COUNT(*) FROM Items)                                               AS ItemCount,
    (SELECT AVG(CAST(i.ItemPrice AS decimal(18,2))) FROM Items i)              AS AvgUnitPrice,

    (SELECT COUNT(*) FROM Companies)                                           AS CompanyCount;";

        private const string TopMembershipSql = @"
SELECT TOP (1)
    mt.MembershipTypeName AS TopMembershipName,
    COUNT(*)              AS TopMembershipCount
FROM Companies c
INNER JOIN MembershipTypes mt ON mt.MembershipTypeID = c.MembershipType
GROUP BY mt.MembershipTypeName
ORDER BY COUNT(*) DESC;";

        public async Task<SummaryCards> GetSummaryAsync(DashboardFilter filter) {
            using var connection = Open();
            var cards = await connection.QuerySingleAsync<SummaryCards>(
                SummarySql, new { filter.From, filter.To });

            var top = await connection.QueryFirstOrDefaultAsync<SummaryCards>(TopMembershipSql);
            if (top is not null) {
                cards.TopMembershipName = top.TopMembershipName;
                cards.TopMembershipCount = top.TopMembershipCount;
            }
            return cards;
        }

        // ---------------- W1 ----------------

        private const string RequestTrendSql = @"
SELECT
    DATEFROMPARTS(YEAR(r.RequestDate), MONTH(r.RequestDate), 1) AS Period,
    r.RequestType                                                AS RequestType,
    COUNT(*)                                                     AS RequestCount
FROM Request r
WHERE r.RequestDate >= @From AND r.RequestDate < @To
GROUP BY DATEFROMPARTS(YEAR(r.RequestDate), MONTH(r.RequestDate), 1), r.RequestType
ORDER BY Period, r.RequestType;";

        public async Task<IReadOnlyList<RequestTrendRow>> GetRequestTrendAsync(DashboardFilter filter) {
            using var connection = Open();
            var rows = await connection.QueryAsync<RequestTrendRow>(
                RequestTrendSql, new { filter.From, filter.To });
            return rows.ToList();
        }

        // ---------------- W2 ----------------

        private const string RequestTypesSql = @"
SELECT
    r.RequestType                                                  AS RequestType,
    COUNT(*)                                                       AS RequestCount,
    CAST(100.0 * COUNT(*) / SUM(COUNT(*)) OVER () AS decimal(5,2)) AS SharePercent
FROM Request r
WHERE r.RequestDate >= @From AND r.RequestDate < @To
GROUP BY r.RequestType
ORDER BY RequestCount DESC;";

        public async Task<IReadOnlyList<RequestTypeRow>> GetRequestTypesAsync(DashboardFilter filter) {
            using var connection = Open();
            var rows = await connection.QueryAsync<RequestTypeRow>(
                RequestTypesSql, new { filter.From, filter.To });
            return rows.ToList();
        }

        // ---------------- W3 ----------------

        private const string CategorySql = @"
SELECT
    c1.Category1Name                                     AS CategoryName,
    COUNT(*)                                             AS ItemCount,
    SUM(CAST(i.Quantity AS bigint))                      AS TotalQuantity,
    MIN(CAST(i.ItemPrice AS decimal(18,2)))              AS MinPrice,
    AVG(CAST(i.ItemPrice AS decimal(18,2)))              AS AvgPrice,
    MAX(CAST(i.ItemPrice AS decimal(18,2)))              AS MaxPrice,
    SUM(CAST(i.ItemPrice AS decimal(18,2)) * i.Quantity) AS StockValue
FROM Items i
INNER JOIN Category1 c1 ON c1.Category1ID = i.Category1
GROUP BY c1.Category1Name
ORDER BY StockValue DESC;";

        public async Task<IReadOnlyList<CategoryRow>> GetCategoryBreakdownAsync() {
            using var connection = Open();
            var rows = await connection.QueryAsync<CategoryRow>(CategorySql);
            return rows.ToList();
        }

        // ---------------- W5 ----------------
        // Request ve Reviews ayrı CTE'lerde toplanır. Doğrudan join edilseydi
        // 10 talebi ve 5 puanı olan firma 50 satır üretir, hem sayım hem
        // ortalama bozulurdu (kartezyen şişme).

        private const string VendorScorecardSql = @"
WITH ReqAgg AS (
    SELECT r.RecieverID AS CompanyID, COUNT(*) AS ReceivedRequests
    FROM Request r
    WHERE r.RequestDate >= @From AND r.RequestDate < @To
    GROUP BY r.RecieverID
),
RevAgg AS (
    SELECT rv.CompanyID,
           AVG(CAST(rv.ReviewScore AS decimal(9,4))) AS AvgScore,
           COUNT(*)                                  AS ReviewCount
    FROM Reviews rv
    GROUP BY rv.CompanyID
)
SELECT
    c.CompanyID                              AS CompanyID,
    c.CompanyName                            AS CompanyName,
    ct.CompanyTypeName                       AS CompanyTypeName,
    COALESCE(q.ReceivedRequests, 0)          AS ReceivedRequests,
    CAST(v.AvgScore AS decimal(4,2))         AS AvgScore,
    COALESCE(v.ReviewCount, 0)               AS ReviewCount
FROM Companies c
LEFT JOIN CompanyTypes ct ON ct.CompanyTypeID = c.CompanyType
LEFT JOIN ReqAgg       q  ON q.CompanyID      = c.CompanyID
LEFT JOIN RevAgg       v  ON v.CompanyID      = c.CompanyID
WHERE COALESCE(q.ReceivedRequests, 0) > 0 OR v.ReviewCount IS NOT NULL
ORDER BY COALESCE(v.AvgScore, -1) DESC, ReceivedRequests DESC;";

        public async Task<IReadOnlyList<VendorScorecardRow>> GetVendorScorecardAsync(DashboardFilter filter) {
            using var connection = Open();
            var rows = await connection.QueryAsync<VendorScorecardRow>(
                VendorScorecardSql, new { filter.From, filter.To });
            return rows.ToList();
        }

        // ---------------- W6 ----------------

        private const string CompanyMixSql = @"
SELECT
    ct.CompanyTypeName    AS CompanyTypeName,
    mt.MembershipTypeName AS MembershipTypeName,
    COUNT(*)              AS CompanyCount
FROM Companies c
INNER JOIN CompanyTypes    ct ON ct.CompanyTypeID    = c.CompanyType
INNER JOIN MembershipTypes mt ON mt.MembershipTypeID = c.MembershipType
GROUP BY ct.CompanyTypeName, mt.MembershipTypeName
ORDER BY ct.CompanyTypeName, CompanyCount DESC;";

        public async Task<IReadOnlyList<CompanyMixRow>> GetCompanyMixAsync() {
            using var connection = Open();
            var rows = await connection.QueryAsync<CompanyMixRow>(CompanyMixSql);
            return rows.ToList();
        }

        // ---------------- W7 ----------------
        // COUNT(DISTINCT) şart: bir firmanın birden fazla adresi olabilir.

        private const string RegionsSql = @"
SELECT
    co.CountryName              AS CountryName,
    t.TownName                  AS TownName,
    COUNT(DISTINCT a.CompanyID) AS CompanyCount
FROM Addresses a
INNER JOIN District  d  ON d.DistrictID = a.DistrictID
INNER JOIN Towns     t  ON t.TownID     = d.TownID
INNER JOIN Countries co ON co.CountryID = t.CountryID
WHERE a.CompanyID IS NOT NULL
GROUP BY co.CountryName, t.TownName
ORDER BY CompanyCount DESC;";

        public async Task<IReadOnlyList<RegionRow>> GetRegionsAsync() {
            using var connection = Open();
            var rows = await connection.QueryAsync<RegionRow>(RegionsSql);
            return rows.ToList();
        }

        // ---------------- W8 ----------------

        private const string FlowsSql = @"
SELECT TOP (15)
    s.CompanyName  AS SenderName,
    rc.CompanyName AS ReceiverName,
    COUNT(*)       AS RequestCount
FROM Request r
INNER JOIN Companies s  ON s.CompanyID  = r.SenderID
INNER JOIN Companies rc ON rc.CompanyID = r.RecieverID
WHERE r.RequestDate >= @From AND r.RequestDate < @To
GROUP BY s.CompanyName, rc.CompanyName
ORDER BY RequestCount DESC;";

        public async Task<IReadOnlyList<FlowRow>> GetFlowsAsync(DashboardFilter filter) {
            using var connection = Open();
            var rows = await connection.QueryAsync<FlowRow>(FlowsSql, new { filter.From, filter.To });
            return rows.ToList();
        }

        // ---------------- W9 ----------------

        private const string FormTopicsSql = @"
SELECT
    f.Topic         AS Topic,
    COUNT(*)        AS FormCount,
    MAX(f.FormDate) AS LastReceived
FROM Forms f
WHERE f.FormDate >= @From AND f.FormDate < @To
GROUP BY f.Topic
ORDER BY FormCount DESC;";

        public async Task<IReadOnlyList<FormTopicRow>> GetFormTopicsAsync(DashboardFilter filter) {
            using var connection = Open();
            var rows = await connection.QueryAsync<FormTopicRow>(
                FormTopicsSql, new { filter.From, filter.To });
            return rows.ToList();
        }

        // ---------------- W10 ----------------

        private const string BrandsSql = @"
SELECT TOP (10)
    i.Brand                                              AS Brand,
    COUNT(*)                                             AS ItemCount,
    SUM(CAST(i.ItemPrice AS decimal(18,2)) * i.Quantity) AS StockValue
FROM Items i
WHERE i.Brand IS NOT NULL AND LTRIM(RTRIM(i.Brand)) <> ''
GROUP BY i.Brand
ORDER BY ItemCount DESC;";

        public async Task<IReadOnlyList<BrandRow>> GetBrandsAsync() {
            using var connection = Open();
            var rows = await connection.QueryAsync<BrandRow>(BrandsSql);
            return rows.ToList();
        }
    }
}
