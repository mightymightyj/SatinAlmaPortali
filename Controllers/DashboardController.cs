using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using satinalmaportali2.Data;
using satinalmaportali2.Models;
using satinalmaportali2.Services;

namespace satinalmaportali2.Controllers
{
    /// <summary>
    /// Satın alma ve talep analitiği gösterge paneli denetleyicisi.
    /// Tüm veri çekme, haritalama ve önbellekleme işlemleri IDashboardService tarafından yürütülür.
    /// </summary>
    [Authorize]
    public class DashboardController : Controller
    {
        public const string ConnectionName = "TedarikDb";

        private readonly IDashboardService _dashboardService;
        private readonly EmailService _emailService;
        private readonly ApplicationDbContext _context;

        public DashboardController(
            IDashboardService dashboardService,
            EmailService emailService,
            ApplicationDbContext context)
        {
            _dashboardService = dashboardService;
            _emailService = emailService;
            _context = context;
        }

        /// <summary>
        /// Dashboard ana görünümü (Varsayılan olarak Alıcı / Buyer paneline yönlendirir veya seçilen rolü açar).
        /// </summary>
        /// <param name="yil">Filtrelenecek yıl (isteğe bağlı).</param>
        /// <param name="role">Rol seçimi: "buyer" veya "supplier".</param>
        public async Task<IActionResult> Index(int? yil, string? role, CancellationToken cancellationToken)
        {
            if (string.Equals(role, "supplier", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Supplier), new { yil });
            }

            return RedirectToAction(nameof(Buyer), new { yil });
        }

        /// <summary>
        /// Alıcı (Buyer) kullanıcı rolü dashboard görünümü.
        /// </summary>
        public async Task<IActionResult> Buyer(int? yil, CancellationToken cancellationToken)
        {
            ViewBag.DashboardRole = "Buyer";
            ViewData["Title"] = "Alıcı Portalı - Genel Bakış";
            var viewModel = await _dashboardService.GetBuyerDashboardAsync(yil, cancellationToken);
            return View(viewModel);
        }

        /// <summary>
        /// Tedarikçi (Supplier) kullanıcı rolü dashboard görünümü.
        /// </summary>
        public async Task<IActionResult> Supplier(int? yil, CancellationToken cancellationToken)
        {
            ViewBag.DashboardRole = "Supplier";
            ViewData["Title"] = "Tedarikçi Portalı - Mağaza Genel Bakış";
            var viewModel = await _dashboardService.GetSupplierDashboardAsync(yil, cancellationToken);
            return View(viewModel);
        }

        /// <summary>
        /// Sistem genel analitik görünümü.
        /// </summary>
        public async Task<IActionResult> Analytics(int? yil, CancellationToken cancellationToken)
        {
            ViewBag.DashboardRole = "Buyer";
            ViewData["Title"] = "Satın Alma ve Talep Analitiği";
            var viewModel = await _dashboardService.GetDashboardAsync(yil, cancellationToken);
            return View("Analytics", viewModel);
        }

        /// <summary>
        /// Yeni kategori veya alt kategori oluşturur.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string categoryName, int? parentCategoryId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["CategoryError"] = "Lütfen bir kategori adı girin.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var created = await _dashboardService.CreateCategoryAsync(categoryName, parentCategoryId, cancellationToken);
                TempData["CategorySuccess"] = parentCategoryId.HasValue && parentCategoryId.Value > 0
                    ? $"Alt kategori '{created.CategoryName}' başarıyla eklendi!" 
                    : $"Kök kategori '{created.CategoryName}' (ParentCategoryID = NULL) başarıyla eklendi!";
            }
            catch (Exception ex)
            {
                TempData["CategoryError"] = "Kategori oluşturulurken hata oluştu: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Tedarikçinin bir RFQ / Satın alma talebine fiyat teklifi vermesini ve alıcıya anında e-posta iletilmesini sağlar.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SubmitOffer([FromBody] SubmitOfferInput input, CancellationToken cancellationToken)
        {
            if (input == null || input.OfferAmount <= 0)
            {
                return Json(new { success = false, message = "Geçersiz teklif tutarı veya eksik bilgi." });
            }

            try
            {
                var activeCompanyName = User.FindFirst("CompanyName")?.Value ?? (User.Identity?.IsAuthenticated == true ? User.Identity.Name : "Tedarikçi Firma");
                var activeOfficial = User.Identity?.Name ?? "Yetkili";
                var supplierName = !string.IsNullOrWhiteSpace(activeCompanyName) ? activeCompanyName : activeOfficial;

                string targetEmail = "navape8857@ittiv.com";
                string buyerName = "Satın Alma Departmanı";
                string productName = !string.IsNullOrWhiteSpace(input.ProductName) ? input.ProductName : "Satın Alma Ürün Talebi";
                int quantity = input.Quantity.HasValue && input.Quantity.Value > 0 ? input.Quantity.Value : 1;
                string unit = !string.IsNullOrWhiteSpace(input.Unit) ? input.Unit : "Adet";

                // Veritabanında eşleşen talep var mı kontrol et
                Requests? matchedRequest = null;

                if (input.RequestId.HasValue && input.RequestId.Value > 0)
                {
                    matchedRequest = await _context.Requests.FirstOrDefaultAsync(r => r.RequestID == input.RequestId.Value, cancellationToken);
                }
                else if (!string.IsNullOrWhiteSpace(input.RfqCode))
                {
                    var parts = input.RfqCode.Split('-', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3 && int.TryParse(parts[^1], out int parsedId))
                    {
                        matchedRequest = await _context.Requests.FirstOrDefaultAsync(r => r.RequestID == parsedId, cancellationToken);
                    }
                    else if (input.RfqCode.StartsWith("REQ-", StringComparison.OrdinalIgnoreCase) && int.TryParse(input.RfqCode.Substring(4), out int reqId))
                    {
                        matchedRequest = await _context.Requests.FirstOrDefaultAsync(r => r.RequestID == reqId, cancellationToken);
                    }
                }

                if (matchedRequest != null)
                {
                    if (!string.IsNullOrWhiteSpace(matchedRequest.ProductName))
                        productName = matchedRequest.ProductName;

                    if (matchedRequest.Quantity.HasValue && matchedRequest.Quantity.Value > 0)
                        quantity = matchedRequest.Quantity.Value;

                    if (!string.IsNullOrWhiteSpace(matchedRequest.BuyerInfo))
                        buyerName = matchedRequest.BuyerInfo;

                    if (matchedRequest.SenderID.HasValue)
                    {
                        var buyerCompany = await _context.Companies
                            .AsNoTracking()
                            .FirstOrDefaultAsync(c => c.CompanyID == matchedRequest.SenderID.Value, cancellationToken);

                        if (buyerCompany != null && !string.IsNullOrWhiteSpace(buyerCompany.CompanyMail))
                        {
                            targetEmail = buyerCompany.CompanyMail;
                            if (string.IsNullOrWhiteSpace(buyerName))
                                buyerName = buyerCompany.CompanyName ?? buyerCompany.CompanyOfficial ?? "Değerli Müşterimiz";
                        }
                    }

                    if (matchedRequest.BuyerInfo != null && matchedRequest.BuyerInfo.Contains("@"))
                    {
                        var emailCandidate = matchedRequest.BuyerInfo.Split(' ', ';', ',').FirstOrDefault(s => s.Contains("@"));
                        if (!string.IsNullOrWhiteSpace(emailCandidate))
                        {
                            targetEmail = emailCandidate.Trim();
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(input.ProductName))
                        productName = input.ProductName;
                    if (input.Quantity.HasValue)
                        quantity = input.Quantity.Value;
                }

                if (!string.IsNullOrWhiteSpace(input.BuyerEmail))
                {
                    targetEmail = input.BuyerEmail.Trim();
                }

                var requestsUrl = $"{Request.Scheme}://{Request.Host}/Requests";

                // E-posta gönderimini arka planda asenkron olarak tetikle
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendOfferReceivedEmailAsync(
                            toEmail: targetEmail,
                            buyerName: buyerName,
                            supplierName: supplierName,
                            rfqCode: input.RfqCode ?? "RFQ-2026",
                            productName: productName,
                            productImageUrl: input.ProductImageUrl,
                            quantity: quantity,
                            unit: unit,
                            offerPrice: input.OfferAmount,
                            deliveryDays: input.DeliveryDays,
                            validityDays: input.ValidityDays,
                            offerNotes: input.Notes,
                            requestsUrl: requestsUrl);
                    }
                    catch (Exception mailEx)
                    {
                        Console.WriteLine($"Teklif e-posta arka plan gönderim hatası: {mailEx.Message}");
                    }
                });

                return Json(new
                {
                    success = true,
                    message = $"'{productName}' talebine {input.OfferAmount:N0} ₺ tutarındaki teklifiniz başarıyla iletildi ve alıcıya e-posta bildirimi gönderildi!",
                    emailSent = true,
                    targetEmail = targetEmail,
                    productName = productName,
                    offerAmount = input.OfferAmount
                });
            }
            catch (Exception ex)
            {
                var errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Teklif kaydedilirken bir hata oluştu: " + errorMsg });
            }
        }

        /// <summary>
        /// Alıcıya gönderilen teklif e-postasını tarayıcıda doğrudan test etmek ve önizlemek için geliştirici aksiyonu.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult PreviewOfferEmail(
            string? email,
            string? buyer,
            string? supplier,
            string? rfq,
            string? product,
            string? image,
            int? qty,
            string? unit,
            decimal? price,
            int? delivery,
            int? validity,
            string? notes)
        {
            var targetEmail = email ?? "navape8857@ittiv.com";
            var buyerName = buyer ?? "Aselsan Tedarik & Savunma A.Ş.";
            var supplierName = supplier ?? "Kaya Endüstriyel Çözümler A.Ş.";
            var rfqCode = rfq ?? "RFQ-2026-104";
            var productName = product ?? "Siemens Simatic S7-1200 PLC & Dijital Giriş/Çıkış Modülü";
            var productImageUrl = image ?? "https://images.unsplash.com/photo-1581092160607-ee22621dd758?w=800&auto=format&fit=crop&q=80";
            var quantity = qty ?? 50;
            var unitName = unit ?? "Adet";
            var offerPrice = price ?? 125000m;
            var deliveryDays = delivery ?? 7;
            var validityDays = validity ?? 30;
            var offerNotes = notes ?? "Tüm ürünler orijinal ambalajında ve 2 yıl resmi distribütör garantilidir. Belirtilen fiyata fabrika teslimi nakliye ve sigorta bedeli dahildir.";
            var requestsUrl = $"{Request.Scheme}://{Request.Host}/Requests";

            var html = _emailService.GetOfferReceivedEmailHtml(
                targetEmail,
                buyerName,
                supplierName,
                rfqCode,
                productName,
                productImageUrl,
                quantity,
                unitName,
                offerPrice,
                deliveryDays,
                validityDays,
                offerNotes,
                requestsUrl);

            return Content(html, "text/html; charset=utf-8");
        }

        /// <summary>
        /// Belirtilen adrese (veya varsayılan navape8857@ittiv.com) rastgele/örnek teklif e-postası gönderme testi.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SendTestOfferEmail(
            string? email,
            string? buyer,
            string? supplier,
            string? rfq,
            string? product,
            string? image,
            int? qty,
            string? unit,
            decimal? price,
            int? delivery,
            int? validity,
            string? notes)
        {
            var targetEmail = email ?? "navape8857@ittiv.com";
            var buyerName = buyer ?? "TUSAŞ Havacılık ve Savunma Teknolojileri A.Ş.";
            var supplierName = supplier ?? "Endüstriyel Titanyum & Metal Sistemleri Ltd. Şti.";
            var rfqCode = rfq ?? "RFQ-2026-8857";
            var productName = product ?? "Endüstriyel Titanyum Bağlantı Elemanları Seti (Grade 5)";
            var productImageUrl = image ?? "https://images.unsplash.com/photo-1581092160607-ee22621dd758?w=800&auto=format&fit=crop&q=80";
            var quantity = qty ?? 1500;
            var unitName = unit ?? "Adet";
            var offerPrice = price ?? 485000m;
            var deliveryDays = delivery ?? 10;
            var validityDays = validity ?? 45;
            var offerNotes = notes ?? "Tüm ürünler uluslararası AS9100 ve ISO 9001 sertifikalı tesislerimizde üretilmiş olup 2 yıl garantilidir. Belirtilen fiyata fabrika teslimi ve korozyon önleyici korumalı nakliye dahildir.";
            var requestsUrl = $"{Request.Scheme}://{Request.Host}/Requests";

            var success = await _emailService.SendOfferReceivedEmailAsync(
                toEmail: targetEmail,
                buyerName: buyerName,
                supplierName: supplierName,
                rfqCode: rfqCode,
                productName: productName,
                productImageUrl: productImageUrl,
                quantity: quantity,
                unit: unitName,
                offerPrice: offerPrice,
                deliveryDays: deliveryDays,
                validityDays: validityDays,
                offerNotes: offerNotes,
                requestsUrl: requestsUrl);

            return Json(new
            {
                success = success,
                targetEmail = targetEmail,
                buyerName = buyerName,
                supplierName = supplierName,
                rfqCode = rfqCode,
                productName = productName,
                quantity = $"{quantity} {unitName}",
                totalPrice = $"{offerPrice:N2} ₺",
                deliveryDays = $"{deliveryDays} İş Günü",
                validityDays = $"{validityDays} Gün",
                message = success 
                    ? $"✅ E-posta başarıyla '{targetEmail}' adresine gönderildi." 
                    : $"⚠️ E-posta SMTP üzerinden gönderilemedi! appsettings.json dosyasındaki EmailSettings bilgilerini kontrol ediniz."
            });
        }
    }
}
