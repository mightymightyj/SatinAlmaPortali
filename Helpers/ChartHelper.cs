using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace satinalmaportali2.Helpers {
    /// <summary>
    /// Panelin sayı biçimlendirmesi ve SVG geometrisi.
    /// Metinler her zaman tr-TR ile biçimlenir; SVG koordinatları ise
    /// InvariantCulture ile yazılır (virgüllü ondalık SVG'yi bozar).
    /// </summary>
    public static class ChartHelper {
        public static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

        private static readonly string[] MonthLabels = {
            "Oca", "Şub", "Mar", "Nis", "May", "Haz",
            "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara"
        };

        public static string MonthLabel(int month) => MonthLabels[month - 1];

        /// <summary>SVG koordinatı — nokta ondalık, en fazla 2 basamak.</summary>
        public static string N(double value) =>
            Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);

        /// <summary>Tam tutar: "1.284.500 ₺".</summary>
        public static string Money(decimal value) => value.ToString("N0", Tr) + " ₺";

        /// <summary>Kısa tutar: "24,7 Mn ₺" — kart ve eksen etiketleri için.</summary>
        public static string Compact(decimal value) {
            var abs = Math.Abs(value);
            if (abs >= 1_000_000_000m) return (value / 1_000_000_000m).ToString("0.#", Tr) + " Mr ₺";
            if (abs >= 1_000_000m) return (value / 1_000_000m).ToString("0.#", Tr) + " Mn ₺";
            if (abs >= 1_000m) return (value / 1_000m).ToString("0.#", Tr) + " B ₺";
            return value.ToString("0", Tr) + " ₺";
        }

        /// <summary>İşaretli yüzde: "+%12,4" / "−%8,1".</summary>
        public static string SignedPercent(decimal value) {
            var sign = value >= 0 ? "+" : "−";
            return sign + "%" + Math.Abs(value).ToString("0.#", Tr);
        }

        public static string ShortDate(DateOnly date) => date.ToString("d MMM yyyy", Tr);

        public static string MonthYear(DateOnly date) => date.ToString("MMMM yyyy", Tr);

        /// <summary>Değeri üstteki "yuvarlak" sayıya tamamlar — eksen tavanı için.</summary>
        public static double NiceCeiling(double value) {
            if (value <= 0) return 1;
            var exponent = Math.Floor(Math.Log10(value));
            var power = Math.Pow(10, exponent);
            var fraction = value / power;
            var nice = fraction <= 1 ? 1
                : fraction <= 2 ? 2
                : fraction <= 2.5 ? 2.5
                : fraction <= 5 ? 5
                : 10;
            return nice * power;
        }

        /// <summary>0'dan tavana kadar eşit aralıklı eksen değerleri.</summary>
        public static IReadOnlyList<double> Ticks(double max, int steps = 4) {
            var result = new List<double>(steps + 1);
            for (var i = 0; i <= steps; i++) result.Add(max / steps * i);
            return result;
        }

        /// <summary>Üstü yuvarlak, tabanı düz dikey sütun — "4px rounded data-end".</summary>
        public static string ColumnPath(double x, double y, double width, double height, double radius = 4) {
            if (height <= 0.5) height = 0.5;
            var r = Math.Min(radius, Math.Min(width / 2, height));
            var bottom = y + height;
            return $"M{N(x)},{N(bottom)} L{N(x)},{N(y + r)} Q{N(x)},{N(y)} {N(x + r)},{N(y)} " +
                   $"L{N(x + width - r)},{N(y)} Q{N(x + width)},{N(y)} {N(x + width)},{N(y + r)} " +
                   $"L{N(x + width)},{N(bottom)} Z";
        }

        /// <summary>Sağ ucu yuvarlak, solu düz yatay çubuk.</summary>
        public static string BarPath(double x, double y, double width, double height, double radius = 4) {
            if (width <= 0.5) width = 0.5;
            var r = Math.Min(radius, Math.Min(height / 2, width));
            var right = x + width;
            return $"M{N(x)},{N(y)} L{N(right - r)},{N(y)} Q{N(right)},{N(y)} {N(right)},{N(y + r)} " +
                   $"L{N(right)},{N(y + height - r)} Q{N(right)},{N(y + height)} {N(right - r)},{N(y + height)} " +
                   $"L{N(x)},{N(y + height)} Z";
        }

        /// <summary>Yüzde: "%94,5".</summary>
        public static string Percent(double value, int decimals = 1) =>
            "%" + value.ToString("N" + decimals, Tr);

        /// <summary>Ondalık puan: "4,3".</summary>
        public static string Score(double value) => value.ToString("0.0", Tr);

        /// <summary>
        /// Donut dilimi. Açılar saat 12'den başlar, saat yönünde ilerler.
        /// Diliminler arası ayrım, çevresine çizgi çekmek yerine açısal boşlukla
        /// yapılır — böylece işaretin etrafına veri olmayan mürekkep eklenmez.
        /// </summary>
        public static string DonutArc(
            double centerX, double centerY, double outerRadius, double innerRadius,
            double startAngle, double endAngle) {

            // Tam tur dilimi SVG yayıyla çizilemez; kapanmaması için hafifçe kısalt.
            if (endAngle - startAngle >= 360) endAngle = startAngle + 359.99;

            var (ox1, oy1) = PointOnCircle(centerX, centerY, outerRadius, startAngle);
            var (ox2, oy2) = PointOnCircle(centerX, centerY, outerRadius, endAngle);
            var (ix1, iy1) = PointOnCircle(centerX, centerY, innerRadius, startAngle);
            var (ix2, iy2) = PointOnCircle(centerX, centerY, innerRadius, endAngle);
            var largeArc = endAngle - startAngle > 180 ? 1 : 0;

            return $"M{N(ox1)},{N(oy1)} " +
                   $"A{N(outerRadius)},{N(outerRadius)} 0 {largeArc},1 {N(ox2)},{N(oy2)} " +
                   $"L{N(ix2)},{N(iy2)} " +
                   $"A{N(innerRadius)},{N(innerRadius)} 0 {largeArc},0 {N(ix1)},{N(iy1)} Z";
        }

        /// <summary>Saat 12'yi sıfır kabul eden açıdan kartezyen noktaya.</summary>
        public static (double X, double Y) PointOnCircle(
            double centerX, double centerY, double radius, double angleDegrees) {

            var radians = (angleDegrees - 90) * Math.PI / 180.0;
            return (centerX + radius * Math.Cos(radians), centerY + radius * Math.Sin(radians));
        }

        /// <summary>Sparkline için düz çizgi noktaları.</summary>
        public static string PolylinePoints(IReadOnlyList<(double X, double Y)> points) =>
            string.Join(" ", points.Select(p => $"{N(p.X)},{N(p.Y)}"));

        /// <summary>
        /// SVG metin etiketi. Razor "&lt;text&gt;" etiketini kendi özel etiketi
        /// olarak yorumladığı için grafik metinleri buradan üretilir.
        /// İçerik HTML olarak kaçırılır.
        /// </summary>
        public static IHtmlContent SvgText(double x, double y, string content, string cssClass, string anchor = "middle") {
            var encoded = HtmlEncoder.Default.Encode(content ?? string.Empty);
            return new HtmlString(
                $"<text class=\"{cssClass}\" x=\"{N(x)}\" y=\"{N(y)}\" text-anchor=\"{anchor}\">{encoded}</text>");
        }
    }
}
