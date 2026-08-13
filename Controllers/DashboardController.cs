using Microsoft.AspNetCore.Mvc;
using satinalmaportali2.Models;
using satinalmaportali2.Services;

namespace satinalmaportali2.Controllers {

    /// <summary>
    /// Satın alma gösterge paneli. Tüm veriler mevcut veritabanından SALT OKUNUR
    /// çekilir; veritabanına ulaşılamadığında sahte sayı gösterilmez, durum bildirilir.
    /// </summary>
    public class DashboardController : Controller {

        /// <summary>appsettings.json &gt; ConnectionStrings altındaki anahtar.</summary>
        public const string ConnectionName = "TedarikDb";

        /// <summary>Trend grafiğinde ayrı çizgi olarak gösterilecek en fazla tür sayısı.</summary>
        private const int MaxTrendSeries = 4;

        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<DashboardController> logger) {
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        /// <param name="yil">Gösterilecek yıl; verilmezse veri bulunan en güncel yıl seçilir.</param>
        public async Task<IActionResult> Index(int? yil) {
            var connectionString = _configuration.GetConnectionString(ConnectionName);

            if (string.IsNullOrWhiteSpace(connectionString)) {
                return View(new DashboardViewModel {
                    State = DataState.NotConfigured,
                    StateMessage =
                        $"Veritabanı bağlantısı tanımlı değil. appsettings.json içine " +
                        $"\"ConnectionStrings\": {{ \"{ConnectionName}\": \"...\" }} eklendiğinde " +
                        "panel mevcut veriyi okumaya başlar."
                });
            }

            var queries = new SqlDashboardQueries(connectionString);

            try {
                return View(await BuildModelAsync(queries, yil));
            }
            catch (Exception ex) {
                // Bağlantı kurulamadı ya da bir sütun/tablo beklenenden farklı.
                // Bu durumda tahmini veri üretmek yerine ne olduğunu söylüyoruz.
                _logger.LogError(ex, "Gösterge paneli verisi okunamadı.");
                return View(new DashboardViewModel {
                    State = DataState.Failed,
                    StateMessage =
                        "Veritabanına ulaşılamadı ya da sorgu çalıştırılamadı. " +
                        "Bağlantı dizesi, sunucu erişimi ve tablo adları kontrol edilmeli.",
                    StateDetail = _environment.IsDevelopment() ? ex.Message : null
                });
            }
        }

        private async Task<DashboardViewModel> BuildModelAsync(IDashboardQueries queries, int? requestedYear) {
            var years = await queries.GetAvailableYearsAsync();

            // Hiç talep kaydı yoksa içinde bulunulan yıla düşüyoruz; sorgular boş döner.
            var selectedYear = requestedYear is int wanted && years.Contains(wanted)
                ? wanted
                : years.Count > 0 ? years[0] : DateTime.Today.Year;

            var from = new DateTime(selectedYear, 1, 1);
            var to = from.AddYears(1);
            var filter = new DashboardFilter(from, to);

            var model = new DashboardViewModel {
                SelectedYear = selectedYear,
                AvailableYears = years,
                From = from,
                To = to,
                Summary = await queries.GetSummaryAsync(filter),
                RequestTypes = await queries.GetRequestTypesAsync(filter),
                Categories = await queries.GetCategoryBreakdownAsync(),
                Vendors = await queries.GetVendorScorecardAsync(filter),
                CompanyMix = await queries.GetCompanyMixAsync(),
                Regions = await queries.GetRegionsAsync(),
                Flows = await queries.GetFlowsAsync(filter),
                FormTopics = await queries.GetFormTopicsAsync(filter),
                Brands = await queries.GetBrandsAsync()
            };

            BuildTrend(model, await queries.GetRequestTrendAsync(filter), from, to);
            return model;
        }

        /// <summary>
        /// Trend satırlarını çizgi grafiğe uygun hâle getirir.
        /// Kritik nokta: hiç talep olmayan ay sorgudan satır olarak GELMEZ.
        /// Doldurulmazsa çizgi o ayı atlar ve yanlış bir eğim çizer.
        /// </summary>
        private static void BuildTrend(
            DashboardViewModel model, IReadOnlyList<RequestTrendRow> rows, DateTime from, DateTime to) {

            // Dönem ekseni: aralıktaki her ay, veri olsun olmasın.
            var periods = new List<DateTime>();
            for (var cursor = new DateTime(from.Year, from.Month, 1); cursor < to; cursor = cursor.AddMonths(1)) {
                periods.Add(cursor);
            }
            model.TrendPeriods = periods;

            if (rows.Count == 0) {
                model.TrendSeriesList = Array.Empty<TrendSeries>();
                return;
            }

            // Tür sayısı fazlaysa kuyruğu "Diğer" altında topluyoruz — yeni renk üretmiyoruz.
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
            foreach (var typeName in namedTypes) {
                series.Add(new TrendSeries { Name = typeName, Values = Fill(typeName, false) });
            }
            if (hasOther) {
                series.Add(new TrendSeries { Name = "Diğer", Values = Fill(null, true) });
            }

            model.TrendSeriesList = series;

            int[] Fill(string? typeName, bool isOther) {
                var values = new int[periods.Count];
                foreach (var row in rows) {
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
