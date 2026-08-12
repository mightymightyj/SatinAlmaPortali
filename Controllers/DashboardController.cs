using Microsoft.AspNetCore.Mvc;
using satinalmaportali2.Helpers;
using satinalmaportali2.Models;
using satinalmaportali2.Services;

namespace satinalmaportali2.Controllers {
    /// <summary>Şirket sahibinin alış, satış ve iş ortağı özetini gördüğü panel.</summary>
    public class DashboardController : Controller {
        // Program.cs'de servis kaydı gerekmesin diye veri doğrudan buradan alınıyor.
        // DI'ya geçirmek istersen: Program.cs'e AddSingleton ekleyip bunu
        // constructor parametresine çevirmen yeterli.
        private readonly IProcurementDataService _data = ProcurementDataService.Instance;

        /// <param name="yil">Gösterilecek yıl; verilmezse en güncel yıl seçilir.</param>
        public IActionResult Index(int? yil) {
            var partners = _data.GetPartners();
            var transactions = _data.GetTransactions();

            var years = transactions.Select(t => t.Date.Year).Distinct().OrderBy(y => y).ToList();
            if (years.Count == 0) return View("Empty");

            var selectedYear = yil is int requested && years.Contains(requested) ? requested : years[^1];
            var inYear = transactions.Where(t => t.Date.Year == selectedYear).ToList();

            // Seçili yıl henüz bitmediyse önceki yılı da aynı ay aralığına kısıtla.
            // Aksi hâlde 8 aylık veri, 12 aylık veriyle kıyaslanır ve gerçekte
            // olmayan bir düşüş görünür.
            var lastMonth = inYear.Count == 0 ? 12 : inYear.Max(t => t.Date.Month);
            var inPreviousYear = transactions
                .Where(t => t.Date.Year == selectedYear - 1 && t.Date.Month <= lastMonth)
                .ToList();

            var periodLabel = lastMonth >= 12
                ? $"{selectedYear} tam yıl"
                : $"{ChartHelper.MonthLabel(1)}–{ChartHelper.MonthLabel(lastMonth)} {selectedYear}";

            var model = new DashboardViewModel {
                SelectedYear = selectedYear,
                AvailableYears = years,
                LastMonthWithData = lastMonth,
                PeriodLabel = periodLabel,
                ComparisonNote = lastMonth >= 12
                    ? $"{selectedYear - 1} yılına göre"
                    : $"{selectedYear - 1} yılının aynı dönemine göre",
                PurchaseTotal = SumOf(inYear, TransactionType.Purchase),
                SaleTotal = SumOf(inYear, TransactionType.Sale),
                PreviousPurchaseTotal = SumOf(inPreviousYear, TransactionType.Purchase),
                PreviousSaleTotal = SumOf(inPreviousYear, TransactionType.Sale),
                TransactionCount = inYear.Count,
                ActivePartnerCount = partners.Count(p => p.Status == PartnershipStatus.Active),
                PastPartnerCount = partners.Count(p => p.Status == PartnershipStatus.Past),
                Monthly = BuildMonthly(inYear),
                Yearly = BuildYearly(transactions, years),
                AllPartners = BuildPartnerSummaries(partners, transactions)
            };

            model.TopPartners = BuildPartnerSummaries(partners, inYear)
                .Where(s => s.Volume > 0)
                .OrderByDescending(s => s.Volume)
                .Take(6)
                .ToList();

            FillSpendAnalytics(model, selectedYear);
            return View(model);
        }

        /// <summary>Üst şerit göstergelerini, departman/kategori kırılımlarını ve listeleri doldurur.</summary>
        private void FillSpendAnalytics(DashboardViewModel model, int year) {
            var departments = _data.GetDepartments(year);
            var categories = _data.GetCategories(year);
            var approvals = _data.GetApprovals();
            var (activeRfq, closingSoon) = _data.GetRfqCounts();

            // Pay yüzdeleri donut dilim açılarıyla aynı toplamdan hesaplansın.
            var categoryTotal = categories.Sum(c => c.Amount);
            foreach (var category in categories) {
                category.SharePercent = categoryTotal <= 0 ? 0 : category.Amount / categoryTotal * 100m;
            }

            // Tasarruf = yalnızca bütçesinin altında kalan departmanların farkı.
            var savings = departments.Where(d => !d.IsOverBudget).Sum(d => -d.Variance);
            var totalBudget = departments.Sum(d => d.Budgeted);

            model.Departments = departments;
            model.Categories = categories;
            model.CategoryTotal = categoryTotal;
            model.RecentApprovals = approvals;
            model.TopVendors = _data.GetVendorScores();
            model.PendingApprovalCount = approvals.Count(a => a.Status == ApprovalStatus.Pending);
            model.UrgentApprovalCount = approvals.Count(a => a.IsUrgent);
            model.ActiveRfqCount = activeRfq;
            model.RfqClosingSoonCount = closingSoon;
            model.EstimatedSavings = savings;
            model.SavingsRatePercent = totalBudget <= 0 ? 0 : savings / totalBudget * 100m;
        }

        private static decimal SumOf(IEnumerable<Transaction> source, TransactionType type) =>
            source.Where(t => t.Type == type).Sum(t => t.Amount);

        /// <summary>12 ayın hepsini üretir — işlem olmayan ay sıfır olarak çizilir.</summary>
        private static List<MonthlyTotals> BuildMonthly(IReadOnlyList<Transaction> yearTransactions) {
            var byMonth = yearTransactions.ToLookup(t => t.Date.Month);
            return Enumerable.Range(1, 12).Select(month => new MonthlyTotals {
                Month = month,
                Label = ChartHelper.MonthLabel(month),
                Purchase = SumOf(byMonth[month], TransactionType.Purchase),
                Sale = SumOf(byMonth[month], TransactionType.Sale)
            }).ToList();
        }

        private static List<YearlyTotals> BuildYearly(IReadOnlyList<Transaction> all, IReadOnlyList<int> years) {
            var byYear = all.ToLookup(t => t.Date.Year);
            return years.Select(year => new YearlyTotals {
                Year = year,
                Purchase = SumOf(byYear[year], TransactionType.Purchase),
                Sale = SumOf(byYear[year], TransactionType.Sale)
            }).ToList();
        }

        /// <summary>Aktif firmalar üstte, her grup içinde hacme göre büyükten küçüğe.</summary>
        private static List<PartnerSummary> BuildPartnerSummaries(
            IReadOnlyList<Partner> partners, IReadOnlyList<Transaction> scope) {

            var byPartner = scope.ToLookup(t => t.PartnerId);
            return partners.Select(partner => {
                var rows = byPartner[partner.Id].ToList();
                return new PartnerSummary {
                    Partner = partner,
                    Purchase = SumOf(rows, TransactionType.Purchase),
                    Sale = SumOf(rows, TransactionType.Sale),
                    TransactionCount = rows.Count,
                    LastTransactionDate = rows.Count == 0 ? null : rows.Max(t => t.Date)
                };
            })
            .OrderBy(s => s.Partner.Status)
            .ThenByDescending(s => s.Volume)
            .ToList();
        }
    }
}
