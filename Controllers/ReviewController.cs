using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using satinalmaportali2.Data;
using satinalmaportali2.Hubs;
using satinalmaportali2.Models;

namespace satinalmaportali2.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<PortalHub> _hubContext;

        public ReviewController(ApplicationDbContext context, IHubContext<PortalHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        private int GetCurrentCompanyId()
        {
            var cidClaim = User.FindFirst("CompanyID")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(cidClaim) && int.TryParse(cidClaim, out int cid))
            {
                return cid;
            }
            return 0;
        }

        private async Task<string> GetCurrentCompanyNameAsync()
        {
            string compName = User.FindFirst("CompanyName")?.Value ?? "";
            if (string.IsNullOrWhiteSpace(compName))
            {
                int cid = GetCurrentCompanyId();
                if (cid > 0)
                {
                    var comp = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == cid);
                    compName = comp?.CompanyName ?? comp?.CompanyOfficial ?? "";
                }
            }
            return compName.Trim();
        }

        /// <summary>
        /// Doğrulanmış kurumsal alıcının satın aldığı sipariş için satıcıyı 10 yıldız üzerinden değerlendirmesi (AJAX).
        /// ŞART: Alıcı bu ürünü/siparişi satın almış olmalıdır.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SubmitBuyerReview([FromBody] SubmitBuyerReviewInputModel? input)
        {
            if (input == null || input.OrderID <= 0)
            {
                return Json(new { success = false, message = "Geçersiz sipariş bilgisi." });
            }

            if (input.ReviewScore < 1 || input.ReviewScore > 10)
            {
                return Json(new { success = false, message = "Değerlendirme puanı 1 ile 10 yıldız arasında olmalıdır." });
            }

            try
            {
                int currentCompanyId = GetCurrentCompanyId();
                string currentEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
                string currentCompanyName = await GetCurrentCompanyNameAsync();

                if (currentCompanyId <= 0 && string.IsNullOrWhiteSpace(currentEmail))
                {
                    return Json(new { success = false, message = "Değerlendirme yapabilmek için lütfen kurumsal hesabınızla giriş yapınız." });
                }

                // 1. Siparişi bul ve ürünlerini yükle
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderID == input.OrderID);

                if (order == null)
                {
                    return Json(new { success = false, message = "Değerlendirilecek sipariş sistemde bulunamadı." });
                }

                // 2. DOĞRULANMIŞ SATIN ALMA KONTROLÜ (Critical Verification)
                bool isBuyer = false;
                if (currentCompanyId > 0 && order.BuyerCompanyID.HasValue && order.BuyerCompanyID.Value == currentCompanyId)
                {
                    isBuyer = true;
                }
                else if (!string.IsNullOrWhiteSpace(currentEmail) && !string.IsNullOrWhiteSpace(order.BuyerEmail) &&
                         string.Equals(currentEmail.Trim(), order.BuyerEmail.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    isBuyer = true;
                }
                else if (!string.IsNullOrWhiteSpace(currentCompanyName) && !string.IsNullOrWhiteSpace(order.BuyerCompanyName) &&
                         string.Equals(currentCompanyName.Trim(), order.BuyerCompanyName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    isBuyer = true;
                }

                if (!isBuyer)
                {
                    return Json(new
                    {
                        success = false,
                        message = "🛡️ Güvenlik Engeli: Yalnızca bu ürünü sistem üzerinden satın almış olan kurumsal alıcılar satıcıyı değerlendirebilir."
                    });
                }

                if (order.OrderStatus == "İptal")
                {
                    return Json(new { success = false, message = "İptal edilmiş siparişler için değerlendirme yapılamaz." });
                }

                // 3. Hedef Satıcı Firmayı Belirle
                int? targetSupplierId = order.SupplierCompanyID;
                if (!targetSupplierId.HasValue && !string.IsNullOrWhiteSpace(order.SupplierCompanyName))
                {
                    var matchedSupplier = await _context.Companies
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.CompanyName != null && c.CompanyName.ToLower() == order.SupplierCompanyName.Trim().ToLower());
                    if (matchedSupplier != null)
                    {
                        targetSupplierId = matchedSupplier.CompanyID;
                    }
                }

                // Eğer hala ID yoksa ilk satıcı firmayı bağla veya ilk sipariş kaleminden al
                if (!targetSupplierId.HasValue && order.OrderItems.Any(oi => oi.SupplierCompanyID.HasValue))
                {
                    targetSupplierId = order.OrderItems.First(oi => oi.SupplierCompanyID.HasValue).SupplierCompanyID;
                }

                // 4. Daha önce bu siparişe değerlendirme yapılmış mı kontrol et
                var existingReview = await _context.Reviews.FirstOrDefaultAsync(r =>
                    r.OrderID == input.OrderID &&
                    r.ReviewType == "BuyerToSeller" &&
                    (r.ReviewerCompanyID == currentCompanyId || (currentCompanyId == 0 && r.ReviewerName == currentCompanyName)));

                string productName = order.OrderItems.FirstOrDefault()?.ProductName ?? "Kurumsal B2B Ürünü";

                if (existingReview != null)
                {
                    // Güncelle
                    existingReview.ReviewScore = Math.Round(input.ReviewScore, 1);
                    existingReview.DeliveryScore = input.DeliveryScore.HasValue ? Math.Round(input.DeliveryScore.Value, 1) : input.ReviewScore;
                    existingReview.QualityScore = input.QualityScore.HasValue ? Math.Round(input.QualityScore.Value, 1) : input.ReviewScore;
                    existingReview.CommunicationScore = input.CommunicationScore.HasValue ? Math.Round(input.CommunicationScore.Value, 1) : input.ReviewScore;
                    existingReview.ReviewText = !string.IsNullOrWhiteSpace(input.ReviewText) ? input.ReviewText.Trim() : existingReview.ReviewText;
                    existingReview.ReviewDate = DateTime.UtcNow;
                    existingReview.IsVerifiedPurchase = true;
                }
                else
                {
                    // Yeni Doğrulanmış Değerlendirme Ekle
                    var newReview = new Reviews
                    {
                        OrderID = order.OrderID,
                        OrderItemID = order.OrderItems.FirstOrDefault()?.OrderItemID,
                        CompanyID = targetSupplierId, // Değerlendirilen Satıcı
                        ReviewerCompanyID = currentCompanyId > 0 ? currentCompanyId : null,
                        ReviewerName = !string.IsNullOrWhiteSpace(currentCompanyName) ? currentCompanyName : (order.BuyerCompanyName ?? "Doğrulanmış Alıcı"),
                        ReviewType = "BuyerToSeller",
                        ReviewScore = Math.Round(input.ReviewScore, 1),
                        DeliveryScore = input.DeliveryScore.HasValue ? Math.Round(input.DeliveryScore.Value, 1) : input.ReviewScore,
                        QualityScore = input.QualityScore.HasValue ? Math.Round(input.QualityScore.Value, 1) : input.ReviewScore,
                        CommunicationScore = input.CommunicationScore.HasValue ? Math.Round(input.CommunicationScore.Value, 1) : input.ReviewScore,
                        ReviewText = !string.IsNullOrWhiteSpace(input.ReviewText) ? input.ReviewText.Trim() : null,
                        ReviewDate = DateTime.UtcNow,
                        IsVerifiedPurchase = true
                    };

                    _context.Reviews.Add(newReview);
                }

                await _context.SaveChangesAsync();

                // SignalR: Satıcının güncel istatistiklerini hesapla ve gerçek zamanlı yayınla
                double newAverage = 10.0;
                int totalReviewsCount = 1;
                if (targetSupplierId.HasValue)
                {
                    var supplierScores = await _context.Reviews
                        .Where(r => r.CompanyID == targetSupplierId.Value && r.ReviewType == "BuyerToSeller" && r.ReviewScore != null)
                        .Select(r => r.ReviewScore!.Value)
                        .ToListAsync();

                    if (supplierScores.Any())
                    {
                        newAverage = Math.Round(supplierScores.Average(), 1);
                        totalReviewsCount = supplierScores.Count;
                    }
                }

                var reviewBroadcast = new
                {
                    sellerId = targetSupplierId,
                    orderId = order.OrderID,
                    orderNumber = order.OrderNumber,
                    reviewerName = !string.IsNullOrWhiteSpace(currentCompanyName) ? currentCompanyName : (order.BuyerCompanyName ?? "Doğrulanmış Kurumsal Alıcı"),
                    reviewScore = Math.Round(input.ReviewScore, 1),
                    deliveryScore = input.DeliveryScore.HasValue ? Math.Round(input.DeliveryScore.Value, 1) : input.ReviewScore,
                    qualityScore = input.QualityScore.HasValue ? Math.Round(input.QualityScore.Value, 1) : input.ReviewScore,
                    communicationScore = input.CommunicationScore.HasValue ? Math.Round(input.CommunicationScore.Value, 1) : input.ReviewScore,
                    reviewText = !string.IsNullOrWhiteSpace(input.ReviewText) ? input.ReviewText.Trim() : "",
                    reviewDate = DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm"),
                    newAverageScore = newAverage,
                    totalReviews = totalReviewsCount
                };

                // 1. Genel Canlı Yayın (Satıcı sayfasında açık olan tüm kullanıcılara)
                await _hubContext.Clients.All.SendAsync("ReceiveReviewUpdate", reviewBroadcast);

                // 2. Satıcıya Doğrudan Kurumsal Bildirim Gönder
                if (targetSupplierId.HasValue)
                {
                    await _hubContext.Clients.Group($"company_{targetSupplierId.Value}").SendAsync("ReceiveNotification", new
                    {
                        type = "review",
                        title = "⭐ Yeni Kurumsal Değerlendirme!",
                        message = $"{reviewBroadcast.reviewerName}, #{order.OrderNumber} siparişi için firmanıza ★ {input.ReviewScore:0.#} / 10 puan verdi.",
                        score = input.ReviewScore,
                        orderId = order.OrderID
                    });
                }

                // 3. Alıcıya Sipariş Değerlendirildi Bildirimi Gönder
                if (order.BuyerCompanyID.HasValue)
                {
                    await _hubContext.Clients.Group($"company_{order.BuyerCompanyID.Value}").SendAsync("ReceiveOrderRated", new
                    {
                        orderId = order.OrderID,
                        score = input.ReviewScore,
                        sellerName = order.SupplierCompanyName
                    });
                }

                return Json(new
                {
                    success = true,
                    message = $"🎉 Teşekkürler! {order.SupplierCompanyName} için verdiğiniz ★ {input.ReviewScore:0.#} / 10 puan ve değerlendirmeniz başarıyla satıcı sayfasına eklendi.",
                    score = input.ReviewScore,
                    orderId = order.OrderID,
                    newAverageScore = newAverage,
                    totalReviews = totalReviewsCount
                });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine($"SubmitBuyerReview Error: {detail}");
                return Json(new { success = false, message = "Değerlendirme kaydedilirken bir hata oluştu: " + detail });
            }
        }

        /// <summary>
        /// Satıcının, sipariş veren alıcı firmayı 10 yıldız üzerinden değerlendirmesi (AJAX).
        /// İki taraflı değerlendirme sistemi: Satıcı -> Alıcı.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SubmitSellerReview([FromBody] SubmitSellerReviewInputModel? input)
        {
            if (input == null || input.OrderID <= 0)
            {
                return Json(new { success = false, message = "Geçersiz sipariş bilgisi." });
            }

            if (input.ReviewScore < 1 || input.ReviewScore > 10)
            {
                return Json(new { success = false, message = "Değerlendirme puanı 1 ile 10 yıldız arasında olmalıdır." });
            }

            try
            {
                int currentCompanyId = GetCurrentCompanyId();
                string currentCompanyName = await GetCurrentCompanyNameAsync();

                if (currentCompanyId <= 0)
                {
                    return Json(new { success = false, message = "Alıcıyı değerlendirmek için tedarikçi hesabınızla oturum açmış olmanız gerekmektedir." });
                }

                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderID == input.OrderID);
                if (order == null)
                {
                    return Json(new { success = false, message = "Sipariş sistemde bulunamadı." });
                }

                // Satıcı Doğrulaması: Oturum açan firma bu siparişin tedarikçisi mi?
                bool isSupplier = false;
                if (order.SupplierCompanyID.HasValue && order.SupplierCompanyID.Value == currentCompanyId)
                {
                    isSupplier = true;
                }
                else if (!string.IsNullOrWhiteSpace(order.SupplierCompanyName) && !string.IsNullOrWhiteSpace(currentCompanyName) &&
                         string.Equals(order.SupplierCompanyName.Trim(), currentCompanyName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    isSupplier = true;
                }

                if (!isSupplier)
                {
                    return Json(new
                    {
                        success = false,
                        message = "🛡️ Yalnızca bu siparişin tedarikçisi olan firma alıcıyı değerlendirebilir."
                    });
                }

                // Hedef Alıcı Firma ID
                int? targetBuyerId = order.BuyerCompanyID;
                if (!targetBuyerId.HasValue && !string.IsNullOrWhiteSpace(order.BuyerCompanyName))
                {
                    var matchedBuyer = await _context.Companies
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.CompanyName != null && c.CompanyName.ToLower() == order.BuyerCompanyName.Trim().ToLower());
                    if (matchedBuyer != null)
                    {
                        targetBuyerId = matchedBuyer.CompanyID;
                    }
                }

                // Mevcut değerlendirme var mı kontrol et
                var existingReview = await _context.Reviews.FirstOrDefaultAsync(r =>
                    r.OrderID == input.OrderID &&
                    r.ReviewType == "SellerToBuyer" &&
                    r.ReviewerCompanyID == currentCompanyId);

                if (existingReview != null)
                {
                    existingReview.ReviewScore = Math.Round(input.ReviewScore, 1);
                    existingReview.DeliveryScore = input.PaymentPunctualityScore.HasValue ? Math.Round(input.PaymentPunctualityScore.Value, 1) : input.ReviewScore;
                    existingReview.CommunicationScore = input.CommunicationScore.HasValue ? Math.Round(input.CommunicationScore.Value, 1) : input.ReviewScore;
                    existingReview.ReviewText = !string.IsNullOrWhiteSpace(input.ReviewText) ? input.ReviewText.Trim() : existingReview.ReviewText;
                    existingReview.ReviewDate = DateTime.UtcNow;
                }
                else
                {
                    var newReview = new Reviews
                    {
                        OrderID = order.OrderID,
                        CompanyID = targetBuyerId, // Değerlendirilen Alıcı
                        ReviewerCompanyID = currentCompanyId, // Değerlendiren Satıcı
                        ReviewerName = !string.IsNullOrWhiteSpace(currentCompanyName) ? currentCompanyName : (order.SupplierCompanyName ?? "Onaylı Tedarikçi"),
                        ReviewType = "SellerToBuyer",
                        ReviewScore = Math.Round(input.ReviewScore, 1),
                        DeliveryScore = input.PaymentPunctualityScore.HasValue ? Math.Round(input.PaymentPunctualityScore.Value, 1) : input.ReviewScore,
                        CommunicationScore = input.CommunicationScore.HasValue ? Math.Round(input.CommunicationScore.Value, 1) : input.ReviewScore,
                        ReviewText = !string.IsNullOrWhiteSpace(input.ReviewText) ? input.ReviewText.Trim() : null,
                        ReviewDate = DateTime.UtcNow,
                        IsVerifiedPurchase = true
                    };

                    _context.Reviews.Add(newReview);
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"✓ Sayın tedarikçi, {order.BuyerCompanyName} alıcı firmasına verdiğiniz ★ {input.ReviewScore:0.#} / 10 puan başarıyla kaydedildi.",
                    score = input.ReviewScore,
                    orderId = order.OrderID
                });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine($"SubmitSellerReview Error: {detail}");
                return Json(new { success = false, message = "Değerlendirme kaydedilirken bir hata oluştu: " + detail });
            }
        }

        /// <summary>
        /// Satıcıya ait 10 yıldızlı değerlendirme özetini ve yorum listesini JSON olarak döndürür
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSellerReviewsJson(int companyId)
        {
            if (companyId <= 0) return Json(new { success = false, message = "Geçersiz firma ID." });

            try
            {
                var reviews = await _context.Reviews
                    .Include(r => r.Order)
                    .Where(r => r.CompanyID == companyId && r.ReviewType == "BuyerToSeller" && r.ReviewScore.HasValue)
                    .OrderByDescending(r => r.ReviewDate)
                    .ToListAsync();

                int totalCount = reviews.Count;
                double avgScore = totalCount > 0 ? Math.Round(reviews.Average(r => r.ReviewScore!.Value), 1) : 9.8;
                double deliveryAvg = totalCount > 0 && reviews.Any(r => r.DeliveryScore.HasValue) 
                    ? Math.Round(reviews.Where(r => r.DeliveryScore.HasValue).Average(r => r.DeliveryScore!.Value), 1) : 9.7;
                double qualityAvg = totalCount > 0 && reviews.Any(r => r.QualityScore.HasValue)
                    ? Math.Round(reviews.Where(r => r.QualityScore.HasValue).Average(r => r.QualityScore!.Value), 1) : 9.9;
                double commAvg = totalCount > 0 && reviews.Any(r => r.CommunicationScore.HasValue)
                    ? Math.Round(reviews.Where(r => r.CommunicationScore.HasValue).Average(r => r.CommunicationScore!.Value), 1) : 9.8;

                var list = reviews.Select(r => new
                {
                    id = r.ReviewID,
                    score = r.ReviewScore,
                    text = r.ReviewText,
                    reviewer = r.ReviewerName ?? "Kurumsal Alıcı",
                    date = r.ReviewDate.ToString("dd.MM.yyyy"),
                    isVerified = r.IsVerifiedPurchase,
                    orderNo = r.Order?.OrderNumber
                }).ToList();

                return Json(new
                {
                    success = true,
                    overallScore = avgScore,
                    totalReviews = totalCount,
                    deliveryAvg,
                    qualityAvg,
                    commAvg,
                    reviews = list
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
