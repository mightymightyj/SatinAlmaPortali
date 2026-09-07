using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using satinalmaportali2.Data;
using satinalmaportali2.Models;
using satinalmaportali2.Services;
using satinalmaportali2.Helpers;

namespace satinalmaportali2.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IIyzicoPaymentService _iyzicoService;
        private readonly EmailService _emailService;

        public PaymentController(
            ApplicationDbContext context,
            IIyzicoPaymentService iyzicoService,
            EmailService emailService)
        {
            _context = context;
            _iyzicoService = iyzicoService;
            _emailService = emailService;
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

        private bool IsSuperAdmin()
        {
            var userType = User.FindFirst("UserType")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;
            var compTypeClaim = User.FindFirst("CompanyTypeID")?.Value;
            return string.Equals(userType, "admin", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(userType, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                   compTypeClaim == "4";
        }

        /// <summary>
        /// 3D Secure Kurumsal Ödeme Başlatma (AJAX).
        /// Sipariş oluşturur ve iyzico 3D Secure HTML form içeriğini döner.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Initialize3DCheckout([FromBody] CorporateCheckoutPaymentInputModel input)
        {
            if (input == null || input.OrderData == null || input.OrderData.Items == null || !input.OrderData.Items.Any())
            {
                return Json(new { success = false, message = "Geçersiz sipariş verisi veya sepet boş." });
            }

            if (input.CardInfo == null || string.IsNullOrWhiteSpace(input.CardInfo.CardNumber))
            {
                return Json(new { success = false, message = "Lütfen kredi kartı bilgilerinizi eksiksiz giriniz." });
            }

            try
            {
                int currentCompanyId = GetCurrentCompanyId();
                if (currentCompanyId <= 0 || User?.Identity?.IsAuthenticated != true)
                {
                    return Json(new 
                    { 
                        success = false, 
                        requireLogin = true, 
                        message = "Satın alma işlemi yapabilmek için lütfen kurumsal hesabınızla giriş yapınız veya yeni bir firma hesabı oluşturunuz." 
                    });
                }

                string currentCompanyName = User.FindFirst("CompanyName")?.Value ?? "";
                string currentEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? input.OrderData.BuyerEmail ?? "";

                Company? comp = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId);
                if (comp != null)
                {
                    if (string.IsNullOrWhiteSpace(input.OrderData.BuyerCompany)) input.OrderData.BuyerCompany = comp.CompanyName ?? comp.CompanyOfficial ?? "";
                    if (string.IsNullOrWhiteSpace(input.OrderData.BuyerTaxNumber)) input.OrderData.BuyerTaxNumber = comp.TCNO;
                    if (string.IsNullOrWhiteSpace(input.OrderData.BuyerTaxOffice)) input.OrderData.BuyerTaxOffice = comp.TaxOffice;
                    if (string.IsNullOrWhiteSpace(input.OrderData.ReceiverPhone)) input.OrderData.ReceiverPhone = comp.CompanyPhone;
                    if (string.IsNullOrWhiteSpace(input.OrderData.ReceiverName)) input.OrderData.ReceiverName = comp.CompanyOfficial ?? comp.CompanyName;
                }

                // Sipariş Numarası ve Konuşma ID Üret
                int countToday = await _context.Orders.CountAsync(o => o.OrderDate.Date == DateTime.UtcNow.Date);
                string orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{(countToday + 1):D4}";
                string conversationId = $"CONV-{orderNumber}";

                decimal totalAmount = 0;
                var memoryItems = new List<OrderItem>();
                int? primarySupplierCompId = null;
                string primarySupplierName = "Onaylı Tedarikçi";

                foreach (var item in input.OrderData.Items)
                {
                    if (item.Quantity <= 0) item.Quantity = 1;
                    decimal lineTotal = item.Price * item.Quantity;
                    totalAmount += lineTotal;

                    string sellerName = item.Seller ?? "Onaylı Tedarikçi";
                    int? sCompId = null;

                    if (item.ProductID > 0)
                    {
                        var dbItem = await _context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.ItemID == item.ProductID);
                        if (dbItem != null)
                        {
                            int availableStock = dbItem.Quantity ?? 0;
                            if (availableStock <= 0)
                            {
                                return Json(new { success = false, message = $"'{dbItem.ItemName ?? item.Title}' ürününün stoğu tükenmiştir." });
                            }
                            if (item.Quantity > availableStock)
                            {
                                return Json(new { success = false, message = $"'{dbItem.ItemName ?? item.Title}' ürünü için talep edilen adet ({item.Quantity}), mevcut stok miktarını ({availableStock}) aşmaktadır." });
                            }

                            if (!string.IsNullOrWhiteSpace(dbItem.Brand)) sellerName = dbItem.Brand;
                            if (!string.IsNullOrWhiteSpace(dbItem.Properties))
                            {
                                var match = System.Text.RegularExpressions.Regex.Match(dbItem.Properties, @"(?:FirmaID|CompanyID)\s*:\s*(\d+)");
                                if (match.Success && int.TryParse(match.Groups[1].Value, out int pid))
                                    sCompId = pid;
                            }
                        }
                    }

                    if (!sCompId.HasValue && !string.IsNullOrWhiteSpace(sellerName))
                    {
                        var matchedComp = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => 
                            (c.CompanyName != null && c.CompanyName.ToLower() == sellerName.Trim().ToLower()) ||
                            (c.CompanyOfficial != null && c.CompanyOfficial.ToLower() == sellerName.Trim().ToLower()));
                        if (matchedComp != null) sCompId = matchedComp.CompanyID;
                    }

                    if (!primarySupplierCompId.HasValue && sCompId.HasValue)
                    {
                        primarySupplierCompId = sCompId;
                        primarySupplierName = sellerName;
                    }

                    memoryItems.Add(new OrderItem
                    {
                        ItemID = item.ProductID > 0 ? item.ProductID : null,
                        ProductName = item.Title,
                        Sku = item.Sku ?? $"SKU-{item.ProductID}",
                        CategoryName = item.Category ?? "Endüstriyel",
                        UnitPrice = item.Price,
                        Quantity = item.Quantity,
                        LineTotal = lineTotal,
                        SupplierCompanyName = sellerName,
                        SupplierCompanyID = sCompId,
                        ItemStatus = "Onaylandı"
                    });
                }

                decimal taxRate = 0.20m;
                decimal taxAmount = Math.Round(totalAmount * taxRate, 2);
                decimal grandTotal = totalAmount + taxAmount;

                // Siparişi doğrudan veritabanına 'Ödeme Bekleniyor' durumunda kaydet
                var pendingOrder = new Order
                {
                    OrderNumber = orderNumber,
                    BuyerCompanyID = currentCompanyId > 0 ? currentCompanyId : null,
                    BuyerCompanyName = !string.IsNullOrWhiteSpace(input.OrderData.BuyerCompany) ? input.OrderData.BuyerCompany.Trim() : (currentCompanyName ?? "Kurumsal Alıcı"),
                    BuyerTaxNumber = input.OrderData.BuyerTaxNumber,
                    BuyerTaxOffice = input.OrderData.BuyerTaxOffice,
                    BuyerEmail = currentEmail,
                    BuyerPhone = input.OrderData.BuyerPhone ?? input.OrderData.ReceiverPhone,
                    SupplierCompanyID = primarySupplierCompId,
                    SupplierCompanyName = primarySupplierName,
                    OrderDate = DateTime.UtcNow,
                    DeliveryDate = DateTime.UtcNow.AddDays(3),
                    TotalAmount = totalAmount,
                    TaxAmount = taxAmount,
                    GrandTotal = grandTotal,
                    PaymentMethod = "iyzico Kredi Kartı (Havuz)",
                    PaymentStatus = "Beklemede (3D)",
                    OrderStatus = "Ödeme Bekleniyor",
                    EscrowStatus = "PendingPayment",
                    DeliveryCity = input.OrderData.DeliveryCity?.Trim(),
                    DeliveryAddress = input.OrderData.DeliveryAddress?.Trim(),
                    ReceiverName = input.OrderData.ReceiverName ?? input.OrderData.BuyerCompany,
                    ReceiverPhone = input.OrderData.ReceiverPhone,
                    Notes = input.OrderData.Notes,
                    OrderItems = memoryItems
                };

                _context.Orders.Add(pendingOrder);
                await _context.SaveChangesAsync();

                // iyzico 3D Secure Başlat
                string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                string callbackUrl = $"{Request.Scheme}://{Request.Host}/Payment/IyzicoCallback";

                var iyzicoResult = await _iyzicoService.Initialize3DCheckoutAsync(pendingOrder, input.CardInfo, ipAddress, callbackUrl);

                if (iyzicoResult.Success)
                {
                    return Json(new
                    {
                        success = true,
                        orderNumber = pendingOrder.OrderNumber,
                        htmlContent = iyzicoResult.HtmlContent,
                        grandTotal = pendingOrder.GrandTotal,
                        message = "3D Secure kimlik doğrulama penceresi yükleniyor..."
                    });
                }
                else
                {
                    // 3D Başlatılamadıysa veritabanında geçersiz/çöp sipariş bırakma!
                    // Düşülen stokları geri iade et ve siparişi veritabanından tamamen kaldır
                    try
                    {
                        if (pendingOrder.OrderItems != null)
                        {
                            foreach (var oi in pendingOrder.OrderItems)
                            {
                                if (oi.ItemID.HasValue && oi.ItemID.Value > 0)
                                {
                                    var dbItem = await _context.Items.FirstOrDefaultAsync(i => i.ItemID == oi.ItemID.Value);
                                    if (dbItem != null && dbItem.Quantity.HasValue)
                                    {
                                        dbItem.Quantity += oi.Quantity;
                                    }
                                }
                            }
                        }
                        _context.Orders.Remove(pendingOrder);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception cleanEx)
                    {
                        Console.WriteLine($"Cleanup error: {cleanEx.Message}");
                    }

                    return Json(new { success = false, message = iyzicoResult.Message });
                }
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Ödeme başlatılırken hata oluştu: " + detail });
            }
        }

        /// <summary>
        /// iyzico 3D Secure Callback Endpoint.
        /// Banka onayından sonra tarayıcı bu endpoint'e yönlendirilir.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> IyzicoCallback(
            [FromForm] string? status,
            [FromForm] string? paymentId,
            [FromForm] string? conversationData,
            [FromForm] string? conversationId,
            [FromForm] string? mdStatus)
        {
            // Private Network Access (PNA) header'ları (Chrome/Edge localhost CORS & PNA engeli için)
            Response.Headers["Access-Control-Allow-Private-Network"] = "true";
            Response.Headers["Access-Control-Allow-Origin"] = "*";

            string redirectUrl = Url.Action("Orders", "Dashboard") ?? "/Dashboard/Orders";

            if (string.IsNullOrWhiteSpace(paymentId) || string.IsNullOrWhiteSpace(conversationId))
            {
                TempData["PaymentError"] = "Geçersiz ödeme callback parametreleri.";
                return Content(BuildIframeBreakoutHtml(false, "Geçersiz ödeme parametreleri.", redirectUrl), "text/html");
            }

            var completeResult = await _iyzicoService.Complete3DPaymentAsync(conversationId, paymentId, conversationData);

            if (completeResult.Success)
            {
                TempData["PaymentSuccess"] = $"🎉 Tebrikler! {completeResult.OrderNumber} numaralı kurumsal siparişiniz {completeResult.GrandTotal:N2} ₺ tutarında iyzico Havuz Koruması ile güvenceye alındı.";
                return Content(BuildIframeBreakoutHtml(true, $"🎉 Ödemeniz başarıyla alındı! {completeResult.OrderNumber} nolu sipariş havuzda güvenceye alındı.", redirectUrl), "text/html");
            }
            else
            {
                // 3D Secure banka onayı alınamadıysa / başarısız olduysa:
                // Sipariş veritabanından tamamen kaldırılır ve stoklar geri iade edilir.
                try
                {
                    string cleanOrderNo = conversationId.Replace("CONV-", "").Trim();
                    var failedOrder = await _context.Orders
                        .Include(o => o.OrderItems)
                        .FirstOrDefaultAsync(o => o.OrderNumber == conversationId || o.OrderNumber == cleanOrderNo || conversationId.Contains(o.OrderNumber));

                    if (failedOrder != null)
                    {
                        if (failedOrder.OrderItems != null)
                        {
                            foreach (var oi in failedOrder.OrderItems)
                            {
                                if (oi.ItemID.HasValue && oi.ItemID.Value > 0)
                                {
                                    var dbItem = await _context.Items.FirstOrDefaultAsync(i => i.ItemID == oi.ItemID.Value);
                                    if (dbItem != null && dbItem.Quantity.HasValue)
                                    {
                                        dbItem.Quantity += oi.Quantity;
                                    }
                                }
                            }
                        }
                        _context.Orders.Remove(failedOrder);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception cancelEx)
                {
                    Console.WriteLine($"Payment callback cleanup error: {cancelEx.Message}");
                }

                TempData["PaymentError"] = $"Ödeme Onaylanamadı: {completeResult.Message}";
                return Content(BuildIframeBreakoutHtml(false, $"Ödeme Onaylanamadı: {completeResult.Message}", redirectUrl), "text/html");
            }
        }

        private static string BuildIframeBreakoutHtml(bool success, string message, string targetUrl)
        {
            string icon = success ? "🎉" : "⚠️";
            string title = success ? "Ödeme Başarıyla Tamamlandı!" : "Ödeme Başarısız";
            string color = success ? "#059669" : "#dc2626";

            return $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>{title}</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #f8fafc; display: flex; align-items: center; justify-content: center; height: 100vh; margin: 0; }}
        .card {{ background: #ffffff; border-radius: 14px; padding: 28px; text-align: center; box-shadow: 0 10px 25px rgba(0,0,0,0.08); max-width: 400px; width: 90%; }}
        .icon {{ font-size: 44px; margin-bottom: 12px; }}
        h3 {{ margin: 0 0 10px 0; color: #0f172a; font-size: 1.25rem; font-weight: 800; }}
        p {{ margin: 0 0 18px 0; color: #475569; font-size: 0.9rem; line-height: 1.5; }}
        .btn {{ display: inline-block; background: {color}; color: #ffffff; padding: 10px 22px; border-radius: 8px; text-decoration: none; font-size: 0.9rem; font-weight: 700; box-shadow: 0 4px 12px rgba(0,0,0,0.15); }}
    </style>
</head>
<body>
    <div class='card'>
        <div class='icon'>{icon}</div>
        <h3>{title}</h3>
        <p>{message}</p>
        <p style='font-size:0.78rem; color:#94a3b8; margin-bottom: 20px;'>Sipariş takip paneline yönlendiriliyorsunuz...</p>
        <a href='{targetUrl}' target='_top' class='btn'>Siparişlerime Git →</a>
    </div>
    <script>
        try {{
            var payload = {{
                type: 'IYZICO_3D_RESULT',
                success: {(success ? "true" : "false")},
                targetUrl: '{targetUrl}'
            }};
            if (window.opener && !window.opener.closed) {{
                window.opener.postMessage(payload, '*');
                try {{ window.opener.location.href = '{targetUrl}'; }} catch(e) {{}}
            }}
            if (window.top && window.top !== window) {{
                window.top.postMessage(payload, '*');
                try {{ window.top.location.href = '{targetUrl}'; }} catch(e) {{}}
            }}
        }} catch(err) {{}}

        setTimeout(function() {{
            if (window.opener && !window.opener.closed) {{
                try {{ window.opener.location.href = '{targetUrl}'; }} catch(e) {{}}
                window.close();
            }} else if (window.top && window.top !== window) {{
                window.top.location.href = '{targetUrl}';
            }} else {{
                window.location.href = '{targetUrl}';
            }}
        }}, 800);
    </script>
</body>
</html>";
        }

        /// <summary>
        /// Alıcı firma siparişi teslim aldığında havuzdaki parayı çözer ve tedarikçiye aktarır.
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ApproveEscrowRelease([FromBody] EscrowActionInputDto input)
        {
            if (input == null || input.OrderID <= 0)
            {
                return Json(new { success = false, message = "Geçersiz sipariş ID." });
            }

            int currentCompanyId = GetCurrentCompanyId();
            bool isAdmin = IsSuperAdmin();

            var result = await _iyzicoService.ApproveEscrowReleaseAsync(input.OrderID, currentCompanyId, isAdmin);
            return Json(new { success = result.Success, message = result.Message });
        }

        /// <summary>
        /// Süper Admin iade talebini onaylayıp parayı alıcıya kesintisiz iade eder.
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DisapproveEscrowRefund([FromBody] EscrowActionInputDto input)
        {
            if (input == null || input.OrderID <= 0)
            {
                return Json(new { success = false, message = "Geçersiz sipariş ID." });
            }

            if (!IsSuperAdmin())
            {
                return Json(new { success = false, message = "İade yetkisi yalnızca Süper Admin hesaba aittir." });
            }

            int currentCompanyId = GetCurrentCompanyId();
            var result = await _iyzicoService.DisapproveEscrowRefundAsync(input.OrderID, currentCompanyId, input.Reason ?? "Admin Tarafından İade Edildi", isAdmin: true);
            return Json(new { success = result.Success, message = result.Message });
        }

        /// <summary>
        /// Alıcı firma eksik/kusurlu ürün için itiraz talebi açar.
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateDispute([FromBody] DisputeCreateInputDto input)
        {
            if (input == null || input.OrderID <= 0 || string.IsNullOrWhiteSpace(input.Reason))
            {
                return Json(new { success = false, message = "Lütfen itiraz nedenini ve açıklamasını belirtiniz." });
            }

            int currentCompanyId = GetCurrentCompanyId();
            var result = await _iyzicoService.CreateDisputeAsync(input, currentCompanyId);
            return Json(new { success = result.Success, message = result.Message });
        }

        /// <summary>
        /// Tedarikçi firmanın iyzico Alt Üye İşyeri (Sub-Merchant) kaydını gerçekleştirir.
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RegisterSubMerchant([FromBody] SubMerchantRegisterDto input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.IBAN) || string.IsNullOrWhiteSpace(input.IdentityNumber))
            {
                return Json(new { success = false, message = "Lütfen IBAN, Vergi/TC Kimlik Numarası ve Şirket Unvanını eksiksiz doldurunuz." });
            }

            int currentCompanyId = GetCurrentCompanyId();
            if (currentCompanyId <= 0 && !IsSuperAdmin())
            {
                return Json(new { success = false, message = "Oturum açmış geçerli bir firma bulunamadı." });
            }

            input.CompanyID = input.CompanyID > 0 && IsSuperAdmin() ? input.CompanyID : currentCompanyId;

            var result = await _iyzicoService.CreateOrUpdateSubMerchantAsync(input);
            return Json(new { success = result.Success, subMerchantKey = result.SubMerchantKey, message = result.Message });
        }

        /// <summary>
        /// Giriş yapan tedarikçinin iyzico alt üye işyeri durumunu döner.
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetSubMerchantStatus()
        {
            int currentCompanyId = GetCurrentCompanyId();
            if (currentCompanyId <= 0)
            {
                return Json(new { isAuthenticated = false });
            }

            var comp = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId);
            if (comp == null)
            {
                return Json(new { isAuthenticated = false });
            }

            return Json(new
            {
                isAuthenticated = true,
                companyId = comp.CompanyID,
                companyName = comp.CompanyName ?? comp.CompanyOfficial ?? "",
                legalCompanyTitle = comp.LegalCompanyTitle ?? comp.CompanyName ?? "",
                identityNumber = comp.IdentityNumber ?? comp.TCNO ?? "",
                taxOffice = comp.TaxOffice ?? "",
                iban = comp.IBAN ?? "",
                subMerchantType = comp.SubMerchantType ?? "LIMITED_OR_JOINT_STOCK_COMPANY",
                subMerchantKey = comp.IyzicoSubMerchantKey ?? "",
                isIyzicoApproved = comp.IsIyzicoApproved || !string.IsNullOrWhiteSpace(comp.IyzicoSubMerchantKey),
                phone = comp.CompanyPhone ?? "",
                email = comp.CompanyMail ?? ""
            });
        }

        /// <summary>
        /// Üyelik Paketi Satın Alma (AJAX).
        /// KULLANICIDAN KESİNLİKLE ADRES İSTEMEZ (dijital paket).
        /// Doğrudan kart bilgileri ile ödeme alır, siparişi oluşturur ve firmanın MembershipTypeID'sini anında günceller.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> PurchaseMembership([FromBody] PurchaseMembershipInputModel input)
        {
            if (input == null)
            {
                return Json(new { success = false, message = "Geçersiz üyelik satın alma isteği." });
            }

            int targetTier = input.TargetMembershipTypeId;
            if (targetTier != MembershipConstants.TierPro && targetTier != MembershipConstants.TierKurumsal)
            {
                return Json(new { success = false, message = "Geçersiz üyelik planı seçildi. Yalnızca Pro veya Kurumsal plan satın alınabilir." });
            }

            int currentCompanyId = GetCurrentCompanyId();
            if (currentCompanyId <= 0 || User?.Identity?.IsAuthenticated != true)
            {
                return Json(new
                {
                    success = false,
                    requireLogin = true,
                    message = "Üyelik satın alabilmek için lütfen firma hesabınızla giriş yapınız veya yeni bir firma hesabı oluşturunuz."
                });
            }

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId);
            if (company == null)
            {
                return Json(new { success = false, message = "Firma hesabı bulunamadı." });
            }

            int currentTier = company.MembershipTypeID ?? MembershipConstants.TierBedava;
            if (currentTier == targetTier)
            {
                string currentPlanName = MembershipConstants.GetTierName(currentTier);
                return Json(new { success = false, message = $"Firmanız zaten '{currentPlanName}' planına sahiptir." });
            }

            // Kart Bilgileri Doğrulama
            if (input.CardInfo == null)
            {
                return Json(new { success = false, message = "Lütfen kredi veya şirket kartı bilgilerinizi eksiksiz giriniz." });
            }

            string cleanCardHolder = (input.CardInfo.CardHolderName ?? "").Trim();
            string cleanCardNo = new string((input.CardInfo.CardNumber ?? "").Where(char.IsDigit).ToArray());
            string cleanMonth = new string((input.CardInfo.ExpireMonth ?? "").Where(char.IsDigit).ToArray()).PadLeft(2, '0');
            string cleanYear = new string((input.CardInfo.ExpireYear ?? "").Where(char.IsDigit).ToArray());
            string cleanCvc = new string((input.CardInfo.Cvc ?? "").Where(char.IsDigit).ToArray());

            if (string.IsNullOrWhiteSpace(cleanCardHolder))
            {
                return Json(new { success = false, message = "Lütfen kart üzerindeki isim alanını doldurunuz." });
            }

            if (cleanCardNo.Length < 15 || cleanCardNo.Length > 19)
            {
                return Json(new { success = false, message = "Geçersiz kart numarası. Kart numarası 16 haneli olmalıdır." });
            }

            if (string.IsNullOrWhiteSpace(cleanMonth) || cleanMonth.Length != 2 || int.Parse(cleanMonth) < 1 || int.Parse(cleanMonth) > 12)
            {
                return Json(new { success = false, message = "Geçersiz son kullanma ayı." });
            }

            if (cleanCvc.Length < 3 || cleanCvc.Length > 4)
            {
                return Json(new { success = false, message = "Geçersiz CVV / CVC güvenlik kodu." });
            }

            try
            {
                decimal planPrice = MembershipConstants.GetPrice(targetTier);
                string targetTierName = MembershipConstants.GetTierName(targetTier);

                // Sipariş Numarası Üret
                int countToday = await _context.Orders.CountAsync(o => o.OrderDate.Date == DateTime.UtcNow.Date);
                string orderNumber = $"MEM-{DateTime.UtcNow:yyyyMMdd}-{(countToday + 1):D4}";

                // Sipariş Kaydı Oluştur - ADRES ALANI DİJİTAL TESLİMAT OLARAK DOLDURULUR, KULLANICIDAN ADRES İSTENMEZ
                var order = new Order
                {
                    OrderNumber = orderNumber,
                    BuyerCompanyID = company.CompanyID,
                    BuyerCompanyName = company.CompanyName ?? company.CompanyOfficial ?? "Kurumsal Üye",
                    BuyerTaxNumber = company.TCNO ?? company.IdentityNumber,
                    BuyerTaxOffice = company.TaxOffice,
                    BuyerEmail = company.CompanyMail ?? User.FindFirst(ClaimTypes.Email)?.Value ?? "",
                    BuyerPhone = company.CompanyPhone ?? "",
                    SupplierCompanyID = null,
                    SupplierCompanyName = "Tedarik Portalı Dijital Hizmetler",
                    OrderDate = DateTime.UtcNow,
                    DeliveryDate = DateTime.UtcNow,
                    TotalAmount = planPrice,
                    TaxAmount = 0m,
                    GrandTotal = planPrice,
                    PaymentMethod = "Kredi / Banka Kartı",
                    PaymentStatus = "Ödendi",
                    OrderStatus = "Tamamlandı",
                    DeliveryCity = "Dijital / Online",
                    DeliveryAddress = "Dijital Hizmet / Üyelik Paketi (Fiziksel Teslimat & Adres Gerekmiyor)",
                    ReceiverName = company.CompanyOfficial ?? company.CompanyName ?? "Yetkili",
                    ReceiverPhone = company.CompanyPhone ?? "",
                    Notes = $"Üyelik Paketi Yükseltme: {targetTierName} Planı (Aylık Abonelik)"
                };

                var orderItem = new OrderItem
                {
                    ProductName = $"{targetTierName} Üyelik Paketi (Aylık Abonelik)",
                    Sku = $"SUB-{targetTierName.ToUpper()}",
                    CategoryName = "Üyelik Paketleri",
                    UnitPrice = planPrice,
                    Quantity = 1,
                    LineTotal = planPrice,
                    SupplierCompanyName = "Tedarik Portalı Dijital Hizmetler",
                    ItemStatus = "Aktif"
                };

                order.OrderItems = new List<OrderItem> { orderItem };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Finansal İşlem Kaydı Ekle
                var transaction = new PaymentTransaction
                {
                    OrderID = order.OrderID,
                    ConversationId = $"CONV-{orderNumber}",
                    TotalAmount = planPrice,
                    MerchantPayoutAmount = planPrice,
                    PlatformCommission = 0m,
                    TransactionType = "MembershipSubscription",
                    Status = "Success",
                    RawResponse = $"Kart sahibi: {cleanCardHolder}, Son 4 hane: {(cleanCardNo.Length >= 4 ? cleanCardNo.Substring(cleanCardNo.Length - 4) : "0000")}"
                };
                _context.PaymentTransactions.Add(transaction);

                // Firmanın Üyelik Seviyesini Anında Güncelle
                company.MembershipTypeID = targetTier;
                await _context.SaveChangesAsync();

                // Bilgilendirme e-postası (Opsiyonel / Hata durumunda işlemi bozmaz)
                try
                {
                    if (!string.IsNullOrWhiteSpace(company.CompanyMail))
                    {
                        await _emailService.SendEmailAsync(
                            company.CompanyMail,
                            $"🎉 Tebrikler! {targetTierName} Üyeliğiniz Aktifleştirildi",
                            $@"<h3>Sayın {company.CompanyName ?? company.CompanyOfficial},</h3>
                               <p>Firmanızın üyelik paketi başarıyla <strong>{targetTierName}</strong> seviyesine yükseltilmiştir.</p>
                               <p><strong>Sipariş No:</strong> {orderNumber}<br/>
                               <strong>Ödenen Tutar:</strong> {planPrice:N2} ₺<br/>
                               <strong>Ödeme Yöntemi:</strong> Kredi Kartı (*{(cleanCardNo.Length >= 4 ? cleanCardNo.Substring(cleanCardNo.Length - 4) : "0000")})</p>
                               <p>Artık genişletilmiş talep ve ürün satışı limitlerinizden anında yararlanabilirsiniz.</p>
                               <p>İyi çalışmalar dileriz.<br/><strong>Tedarik Portalı Ekibi</strong></p>"
                        );
                    }
                }
                catch (Exception emailEx)
                {
                    Console.WriteLine($"Membership purchase email note: {emailEx.Message}");
                }

                return Json(new
                {
                    success = true,
                    orderNumber = order.OrderNumber,
                    newTierId = targetTier,
                    newTierName = targetTierName,
                    message = $"🎉 Tebrikler! Üyeliğiniz başarıyla '{targetTierName}' planına yükseltildi. Yeni limitleriniz anında tanımlandı."
                });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Üyelik satın alma işlemi sırasında bir hata oluştu: " + detail });
            }
        }
    }

    public class CorporateCheckoutPaymentInputModel
    {
        public CreateOrderInputModel? OrderData { get; set; }
        public CardPaymentInputDto? CardInfo { get; set; }
    }
}
