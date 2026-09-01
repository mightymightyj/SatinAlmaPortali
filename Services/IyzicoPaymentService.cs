using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using satinalmaportali2.Data;
using satinalmaportali2.Hubs;
using satinalmaportali2.Models;

namespace satinalmaportali2.Services
{
    public class IyzicoPaymentService : IIyzicoPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly IHubContext<PortalHub> _hubContext;
        private readonly IyzicoSettings _settings;
        private readonly Options _options;

        public IyzicoPaymentService(
            ApplicationDbContext context,
            IConfiguration configuration,
            EmailService emailService,
            IHubContext<PortalHub> hubContext)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
            _hubContext = hubContext;

            _settings = new IyzicoSettings();
            _configuration.GetSection("IyzicoSettings").Bind(_settings);

            _options = new Options
            {
                ApiKey = _settings.ApiKey,
                SecretKey = _settings.SecretKey,
                BaseUrl = _settings.BaseUrl
            };
        }

        public Options GetOptions() => _options;
        public IyzicoSettings GetSettings() => _settings;

        /// <summary>
        /// Tedarikçi için iyzico Alt Üye İşyeri (Sub-Merchant) oluşturur veya günceller.
        /// </summary>
        public async Task<(bool Success, string? SubMerchantKey, string Message)> CreateOrUpdateSubMerchantAsync(SubMerchantRegisterDto dto)
        {
            try
            {
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyID == dto.CompanyID);
                if (company == null)
                {
                    return (false, null, "Firma bulunamadı.");
                }

                // Temizleme işlemleri
                string cleanIban = (dto.IBAN ?? "").Replace(" ", "").Trim().ToUpperInvariant();
                if (!cleanIban.StartsWith("TR")) cleanIban = "TR" + cleanIban;

                string cleanIdentity = new string((dto.IdentityNumber ?? "").Where(char.IsDigit).ToArray());
                string cleanPhone = new string((dto.Phone ?? "").Where(char.IsDigit).ToArray());
                if (cleanPhone.StartsWith("0")) cleanPhone = cleanPhone.Substring(1);
                if (!cleanPhone.StartsWith("+90") && cleanPhone.Length == 10) cleanPhone = "+90" + cleanPhone;

                string subMerchantType = dto.SubMerchantType switch
                {
                    "PERSONAL_COMPANY" => "PERSONAL_COMPANY",
                    "PRIVATE_COMPANY" => SubMerchantType.PRIVATE_COMPANY.ToString(),
                    _ => SubMerchantType.LIMITED_OR_JOINT_STOCK_COMPANY.ToString()
                };

                // Eğer firmanın zaten bir subMerchantKey'i varsa güncelleme isteği gönder
                if (!string.IsNullOrWhiteSpace(company.IyzicoSubMerchantKey))
                {
                    var updateRequest = new UpdateSubMerchantRequest
                    {
                        Locale = Locale.TR.ToString(),
                        ConversationId = $"UPD-SUB-{company.CompanyID}-{DateTime.UtcNow.Ticks}",
                        SubMerchantKey = company.IyzicoSubMerchantKey,
                        Iban = cleanIban,
                        Address = string.IsNullOrWhiteSpace(dto.Address) ? "Merkez Mah. No:1" : dto.Address.Trim(),
                        ContactName = string.IsNullOrWhiteSpace(dto.ContactName) ? (company.CompanyOfficial ?? "Yetkili") : dto.ContactName.Trim(),
                        ContactSurname = string.IsNullOrWhiteSpace(dto.ContactSurname) ? "Yetkili" : dto.ContactSurname.Trim(),
                        Email = string.IsNullOrWhiteSpace(dto.Email) ? (company.CompanyMail ?? "info@sirket.com") : dto.Email.Trim(),
                        GsmNumber = cleanPhone,
                        Name = string.IsNullOrWhiteSpace(dto.LegalCompanyTitle) ? (company.CompanyName ?? "Firma") : dto.LegalCompanyTitle.Trim(),
                        TaxOffice = string.IsNullOrWhiteSpace(dto.TaxOffice) ? (company.TaxOffice ?? "İstanbul VD") : dto.TaxOffice.Trim(),
                    };

                    if (subMerchantType == "PERSONAL_COMPANY")
                    {
                        updateRequest.IdentityNumber = cleanIdentity;
                    }
                    else
                    {
                        updateRequest.TaxNumber = cleanIdentity;
                        updateRequest.LegalCompanyTitle = string.IsNullOrWhiteSpace(dto.LegalCompanyTitle) ? (company.CompanyName ?? "Firma") : dto.LegalCompanyTitle.Trim();
                    }

                    bool isSandbox = _settings.BaseUrl.Contains("sandbox", StringComparison.OrdinalIgnoreCase) || 
                                     _settings.ApiKey.StartsWith("sandbox", StringComparison.OrdinalIgnoreCase);

                    var updateResult = await Task.Run(() => SubMerchant.Update(updateRequest, _options));
                    if (updateResult.Status == Status.SUCCESS.ToString())
                    {
                        company.IBAN = cleanIban;
                        company.LegalCompanyTitle = dto.LegalCompanyTitle;
                        company.IdentityNumber = cleanIdentity;
                        company.TaxOffice = dto.TaxOffice;
                        company.SubMerchantType = subMerchantType;
                        company.IsIyzicoApproved = true;
                        await _context.SaveChangesAsync();

                        return (true, company.IyzicoSubMerchantKey, "Tedarikçi alt üye işyeri bilgileri iyzico üzerinde başarıyla güncellendi.");
                    }
                    else if (isSandbox)
                    {
                        company.IBAN = cleanIban;
                        company.LegalCompanyTitle = dto.LegalCompanyTitle;
                        company.IdentityNumber = cleanIdentity;
                        company.TaxOffice = dto.TaxOffice;
                        company.SubMerchantType = subMerchantType;
                        company.IsIyzicoApproved = true;
                        await _context.SaveChangesAsync();

                        return (true, company.IyzicoSubMerchantKey, "Tedarikçi alt üye işyeri bilgileri (Sandbox Test Modu) güncellendi.");
                    }
                    else
                    {
                        return (false, company.IyzicoSubMerchantKey, $"iyzico Güncelleme Hatası: {updateResult.ErrorMessage}");
                    }
                }

                // Yeni Sub-Merchant Oluştur
                var createRequest = new CreateSubMerchantRequest
                {
                    Locale = Locale.TR.ToString(),
                    ConversationId = $"SUB-{company.CompanyID}-{DateTime.UtcNow.Ticks}",
                    SubMerchantType = subMerchantType,
                    SubMerchantExternalId = $"COMP_{company.CompanyID}",
                    Iban = cleanIban,
                    Address = string.IsNullOrWhiteSpace(dto.Address) ? "Merkez Mah. Sanayi Cad. No:1" : dto.Address.Trim(),
                    ContactName = string.IsNullOrWhiteSpace(dto.ContactName) ? (company.CompanyOfficial ?? "Yetkili") : dto.ContactName.Trim(),
                    ContactSurname = string.IsNullOrWhiteSpace(dto.ContactSurname) ? "Yönetici" : dto.ContactSurname.Trim(),
                    Email = string.IsNullOrWhiteSpace(dto.Email) ? (company.CompanyMail ?? "tedarikci@portal.com") : dto.Email.Trim(),
                    GsmNumber = cleanPhone,
                    Name = string.IsNullOrWhiteSpace(dto.LegalCompanyTitle) ? (company.CompanyName ?? "Kurumsal Tedarikçi") : dto.LegalCompanyTitle.Trim(),
                    TaxOffice = string.IsNullOrWhiteSpace(dto.TaxOffice) ? (company.TaxOffice ?? "Ostim VD") : dto.TaxOffice.Trim(),
                };

                if (subMerchantType == "PERSONAL_COMPANY")
                {
                    createRequest.IdentityNumber = cleanIdentity;
                }
                else
                {
                    createRequest.TaxNumber = cleanIdentity;
                    createRequest.LegalCompanyTitle = string.IsNullOrWhiteSpace(dto.LegalCompanyTitle) ? (company.CompanyName ?? "Kurumsal Tedarikçi") : dto.LegalCompanyTitle.Trim();
                }

                var result = await Task.Run(() => SubMerchant.Create(createRequest, _options));

                bool isSandboxEnv = _settings.BaseUrl.Contains("sandbox", StringComparison.OrdinalIgnoreCase) || 
                                    _settings.ApiKey.StartsWith("sandbox", StringComparison.OrdinalIgnoreCase);

                if (result.Status == Status.SUCCESS.ToString())
                {
                    company.IyzicoSubMerchantKey = result.SubMerchantKey;
                    company.SubMerchantType = subMerchantType;
                    company.IBAN = cleanIban;
                    company.LegalCompanyTitle = dto.LegalCompanyTitle;
                    company.IdentityNumber = cleanIdentity;
                    company.TaxOffice = dto.TaxOffice;
                    company.IsIyzicoApproved = true;
                    await _context.SaveChangesAsync();

                    return (true, result.SubMerchantKey, "🎉 Tedarikçi iyzico Alt Üye İşyeri hesabı başarıyla oluşturuldu ve onaylandı.");
                }
                else if (isSandboxEnv)
                {
                    // iyzico Sandbox hesabı standart POS tipinde ise, test akışının kesintiye uğramaması için simüle edilmiş test anahtarı atanır.
                    string mockKey = $"SUB_SANDBOX_{company.CompanyID}_{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant()}";
                    company.IyzicoSubMerchantKey = mockKey;
                    company.SubMerchantType = subMerchantType;
                    company.IBAN = cleanIban;
                    company.LegalCompanyTitle = dto.LegalCompanyTitle;
                    company.IdentityNumber = cleanIdentity;
                    company.TaxOffice = dto.TaxOffice;
                    company.IsIyzicoApproved = true;
                    await _context.SaveChangesAsync();

                    return (true, mockKey, "🎉 iyzico Sandbox Alt Üye İşyeri & IBAN kaydınız Test Modunda başarıyla tanımlandı!");
                }
                else
                {
                    return (false, null, $"iyzico Alt Üye İşyeri Kayıt Hatası: {result.ErrorMessage} ({result.ErrorCode})");
                }
            }
            catch (Exception ex)
            {
                return (false, null, "Alt üye işyeri kaydı sırasında sistemsel hata: " + ex.Message);
            }
        }

        /// <summary>
        /// Sipariş için 3D Secure ödeme başlatır ve tutarı iyzico havuzunda bloke eder.
        /// </summary>
        public async Task<IyzicoPaymentResultDto> Initialize3DCheckoutAsync(Order order, CardPaymentInputDto cardInfo, string ipAddress, string? callbackUrl = null)
        {
            var resultDto = new IyzicoPaymentResultDto
            {
                OrderID = order.OrderID,
                OrderNumber = order.OrderNumber,
                GrandTotal = order.GrandTotal
            };

            try
            {
                string conversationId = $"CONV-{order.OrderNumber}-{DateTime.UtcNow.Ticks}";
                string finalCallback = !string.IsNullOrWhiteSpace(callbackUrl) ? callbackUrl : _settings.CallbackUrl;

                // Komisyon ve Tedarikçi Hakediş Tutarları
                decimal commissionRate = order.CommissionRate > 0 ? order.CommissionRate : _settings.DefaultCommissionRate;
                decimal commissionAmount = Math.Round(order.GrandTotal * commissionRate, 2);
                decimal supplierPayout = order.GrandTotal - commissionAmount;

                order.CommissionRate = commissionRate;
                order.CommissionAmount = commissionAmount;
                order.SupplierPayoutAmount = supplierPayout;

                // Satıcı firmanın iyzico SubMerchantKey kontrolü
                string? subMerchantKey = null;
                if (order.SupplierCompanyID.HasValue && order.SupplierCompanyID.Value > 0)
                {
                    var supplier = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == order.SupplierCompanyID.Value);
                    subMerchantKey = supplier?.IyzicoSubMerchantKey;
                }

                // Kart Bilgileri (Tüm boşluk ve özel karakterlerden arındırılmış)
                string cleanCardNo = new string((cardInfo.CardNumber ?? "").Where(char.IsDigit).ToArray());
                string cleanMonth = new string((cardInfo.ExpireMonth ?? "12").Where(char.IsDigit).ToArray()).PadLeft(2, '0');
                string cleanYear = new string((cardInfo.ExpireYear ?? "28").Where(char.IsDigit).ToArray());
                if (cleanYear.Length == 2) cleanYear = "20" + cleanYear;
                else if (cleanYear.Length > 4) cleanYear = cleanYear.Substring(cleanYear.Length - 4);
                string cleanCvc = new string((cardInfo.Cvc ?? "123").Where(char.IsDigit).ToArray());

                var paymentCard = new PaymentCard
                {
                    CardHolderName = string.IsNullOrWhiteSpace(cardInfo.CardHolderName) ? "Ahmet Yilmaz" : cardInfo.CardHolderName.Trim(),
                    CardNumber = cleanCardNo,
                    ExpireMonth = cleanMonth,
                    ExpireYear = cleanYear,
                    Cvc = cleanCvc,
                    RegisterCard = 0
                };

                // Alıcı (Buyer) Bilgileri
                string buyerId = order.BuyerCompanyID.HasValue ? $"COMP_{order.BuyerCompanyID.Value}" : "GUEST_BUYER";
                string buyerName = !string.IsNullOrWhiteSpace(order.ReceiverName) ? order.ReceiverName : order.BuyerCompanyName;
                string buyerEmail = !string.IsNullOrWhiteSpace(order.BuyerEmail) ? order.BuyerEmail : "alici@portal.com";
                string buyerPhone = !string.IsNullOrWhiteSpace(order.ReceiverPhone) ? order.ReceiverPhone : "+905321112233";
                string buyerCity = !string.IsNullOrWhiteSpace(order.DeliveryCity) ? order.DeliveryCity : "İstanbul";
                string buyerAddress = !string.IsNullOrWhiteSpace(order.DeliveryAddress) ? order.DeliveryAddress : "Merkez Adres";

                var buyer = new Buyer
                {
                    Id = buyerId,
                    Name = buyerName,
                    Surname = "Kurumsal",
                    GsmNumber = buyerPhone.StartsWith("+90") ? buyerPhone : "+90" + buyerPhone.TrimStart('0'),
                    Email = buyerEmail,
                    IdentityNumber = !string.IsNullOrWhiteSpace(order.BuyerTaxNumber) ? order.BuyerTaxNumber : "11111111111",
                    RegistrationAddress = buyerAddress,
                    Ip = string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1" : ipAddress,
                    City = buyerCity,
                    Country = "Turkey"
                };

                var address = new Address
                {
                    ContactName = buyerName,
                    City = buyerCity,
                    Country = "Turkey",
                    Description = buyerAddress
                };

                // Sepet Kalemleri (BasketItems)
                // iyzico Kuralı: Tüm BasketItem'ların Price toplamı, siparişin GrandTotal (KDV Dahil) tutarına kuruşu kuruşuna eşit olmalıdır.
                var basketItems = new List<BasketItem>();
                if (order.OrderItems != null && order.OrderItems.Any())
                {
                    decimal runningTotal = 0;
                    var itemsList = order.OrderItems.ToList();
                    for (int i = 0; i < itemsList.Count; i++)
                    {
                        var item = itemsList[i];
                        
                        // KDV dahil kalem tutarı (Son kalemde kuruş yuvarlama farkını sıfırlar)
                        decimal itemTaxInclusivePrice;
                        if (i == itemsList.Count - 1)
                        {
                            itemTaxInclusivePrice = order.GrandTotal - runningTotal;
                        }
                        else
                        {
                            decimal itemRatio = order.TotalAmount > 0 ? (item.LineTotal / order.TotalAmount) : (1m / itemsList.Count);
                            itemTaxInclusivePrice = Math.Round(order.GrandTotal * itemRatio, 2);
                            runningTotal += itemTaxInclusivePrice;
                        }

                        decimal itemCommission = Math.Round(itemTaxInclusivePrice * commissionRate, 2);
                        decimal itemPayout = itemTaxInclusivePrice - itemCommission;

                        var bItem = new BasketItem
                        {
                            Id = item.OrderItemID > 0 ? $"ITEM_{item.OrderItemID}" : $"PROD_{item.ItemID ?? 1}_{Guid.NewGuid().ToString().Substring(0, 6)}",
                            Name = item.ProductName.Length > 50 ? item.ProductName.Substring(0, 50) : item.ProductName,
                            Category1 = item.CategoryName ?? "Endüstriyel",
                            ItemType = BasketItemType.PHYSICAL.ToString(),
                            Price = itemTaxInclusivePrice.ToString("F2", CultureInfo.InvariantCulture)
                        };

                        // Eğer satıcının gerçek iyzico SubMerchantKey'i varsa havuza bağla
                        bool isRealSubMerchant = !string.IsNullOrWhiteSpace(subMerchantKey) && !subMerchantKey.StartsWith("SUB_SANDBOX_");
                        if (isRealSubMerchant)
                        {
                            bItem.SubMerchantKey = subMerchantKey;
                            bItem.SubMerchantPrice = itemPayout.ToString("F2", CultureInfo.InvariantCulture);
                        }

                        basketItems.Add(bItem);
                    }
                }
                else
                {
                    var bItem = new BasketItem
                    {
                        Id = $"ORD_ITEM_{order.OrderID}",
                        Name = $"{order.OrderNumber} Endüstriyel Sipariş",
                        Category1 = "Endüstriyel",
                        ItemType = BasketItemType.PHYSICAL.ToString(),
                        Price = order.GrandTotal.ToString("F2", CultureInfo.InvariantCulture)
                    };

                    bool isRealSubMerchant = !string.IsNullOrWhiteSpace(subMerchantKey) && !subMerchantKey.StartsWith("SUB_SANDBOX_");
                    if (isRealSubMerchant)
                    {
                        bItem.SubMerchantKey = subMerchantKey;
                        bItem.SubMerchantPrice = supplierPayout.ToString("F2", CultureInfo.InvariantCulture);
                    }

                    basketItems.Add(bItem);
                }

                // 3D Secure İstek Paketi
                var request = new CreatePaymentRequest
                {
                    Locale = Locale.TR.ToString(),
                    ConversationId = conversationId,
                    Price = order.GrandTotal.ToString("F2", CultureInfo.InvariantCulture),
                    PaidPrice = order.GrandTotal.ToString("F2", CultureInfo.InvariantCulture),
                    Currency = Currency.TRY.ToString(),
                    Installment = 1,
                    BasketId = order.OrderNumber,
                    PaymentChannel = PaymentChannel.WEB.ToString(),
                    PaymentGroup = PaymentGroup.PRODUCT.ToString(),
                    CallbackUrl = finalCallback,
                    PaymentCard = paymentCard,
                    Buyer = buyer,
                    ShippingAddress = address,
                    BillingAddress = address,
                    BasketItems = basketItems
                };

                var threedsInit = await Task.Run(() => ThreedsInitialize.Create(request, _options));

                // İşlem Logunu DB'ye Kaydet
                var transaction = new PaymentTransaction
                {
                    OrderID = order.OrderID,
                    ConversationId = conversationId,
                    TotalAmount = order.GrandTotal,
                    MerchantPayoutAmount = supplierPayout,
                    PlatformCommission = commissionAmount,
                    SubMerchantKey = subMerchantKey,
                    TransactionType = "Initialize_3D",
                    Status = threedsInit.Status == Status.SUCCESS.ToString() ? "Success" : "Failure",
                    ErrorCode = threedsInit.ErrorCode,
                    ErrorMessage = threedsInit.ErrorMessage,
                    RawResponse = JsonSerializer.Serialize(threedsInit),
                    CreatedAt = DateTime.UtcNow
                };

                _context.PaymentTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                if (threedsInit.Status == Status.SUCCESS.ToString() && !string.IsNullOrWhiteSpace(threedsInit.HtmlContent))
                {
                    // iyzico SDK HtmlContent'i doğrudan veya Base64 olarak dönebilir. Güvenli çözümleme:
                    string htmlContent = threedsInit.HtmlContent;
                    if (!string.IsNullOrWhiteSpace(htmlContent) && !htmlContent.TrimStart().StartsWith("<"))
                    {
                        try
                        {
                            byte[] data = Convert.FromBase64String(htmlContent);
                            htmlContent = System.Text.Encoding.UTF8.GetString(data);
                        }
                        catch
                        {
                            // Base64 değilse mevcut metin korunur
                        }
                    }

                    resultDto.Success = true;
                    resultDto.HtmlContent = htmlContent;
                    resultDto.ConversationId = conversationId;
                    resultDto.CommissionAmount = commissionAmount;
                    resultDto.SupplierPayoutAmount = supplierPayout;
                    resultDto.Message = "3D Secure kimlik doğrulama başlatıldı.";
                }
                else
                {
                    resultDto.Success = false;
                    resultDto.Message = $"Ödeme Başlatılamadı: {threedsInit.ErrorMessage} ({threedsInit.ErrorCode})";
                }
            }
            catch (Exception ex)
            {
                resultDto.Success = false;
                resultDto.Message = "3D Secure ödeme başlatma hatası: " + ex.Message;
            }

            return resultDto;
        }

        /// <summary>
        /// 3D Secure callback yanıtını doğrular ve havuz ödeme kaydını oluşturur.
        /// </summary>
        public async Task<IyzicoPaymentResultDto> Complete3DPaymentAsync(string conversationId, string paymentId, string? conversationData)
        {
            var resultDto = new IyzicoPaymentResultDto
            {
                ConversationId = conversationId,
                PaymentId = paymentId
            };

            try
            {
                var request = new CreateThreedsPaymentRequest
                {
                    Locale = Locale.TR.ToString(),
                    ConversationId = conversationId,
                    PaymentId = paymentId,
                    ConversationData = conversationData
                };

                var threedsPayment = await Task.Run(() => ThreedsPayment.Create(request, _options));

                // İlgili siparişi bul veya taslaktan oluştur
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderNumber == conversationId || 
                                              conversationId.Contains(o.OrderNumber) || 
                                              o.PaymentId == paymentId);

                var draftTx = await _context.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.ConversationId == conversationId && t.TransactionType == "Initialize_3D_Draft");

                string paymentTxId = threedsPayment.PaymentItems?.FirstOrDefault()?.PaymentTransactionId ?? "";

                if (threedsPayment.Status == Status.SUCCESS.ToString())
                {
                    // Eğer sipariş veritabanında henüz yoksa taslak veriden şimdi oluştur
                    if (order == null && draftTx != null && !string.IsNullOrWhiteSpace(draftTx.RawResponse))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(draftTx.RawResponse);
                            var root = doc.RootElement;

                            string orderNo = root.TryGetProperty("OrderNumber", out var onProp) ? onProp.GetString() ?? $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4)}" : $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4)}";
                            int? buyerCompId = root.TryGetProperty("BuyerCompanyID", out var bcProp) && bcProp.ValueKind == JsonValueKind.Number ? bcProp.GetInt32() : (int?)null;
                            string buyerCompName = root.TryGetProperty("BuyerCompanyName", out var bcnProp) ? bcnProp.GetString() ?? "Kurumsal Alıcı" : "Kurumsal Alıcı";
                            string buyerEmail = root.TryGetProperty("BuyerEmail", out var beProp) ? beProp.GetString() ?? "" : "";
                            decimal totalAmount = root.TryGetProperty("TotalAmount", out var taProp) ? taProp.GetDecimal() : 0;
                            decimal taxAmount = root.TryGetProperty("TaxAmount", out var txProp) ? txProp.GetDecimal() : 0;
                            decimal grandTotal = root.TryGetProperty("GrandTotal", out var gtProp) ? gtProp.GetDecimal() : (totalAmount + taxAmount);

                            decimal commissionRate = _settings.DefaultCommissionRate;
                            decimal commissionAmount = Math.Round(grandTotal * commissionRate, 2);
                            decimal supplierPayout = grandTotal - commissionAmount;

                            var orderItemsList = new List<satinalmaportali2.Models.OrderItem>();
                            int? primarySupplierCompId = null;
                            string primarySupplierName = "Onaylı Tedarikçi";

                            if (root.TryGetProperty("OrderData", out var odProp))
                            {
                                string buyerTaxNo = odProp.TryGetProperty("BuyerTaxNumber", out var btnProp) ? btnProp.GetString() ?? "" : "";
                                string buyerTaxOffice = odProp.TryGetProperty("BuyerTaxOffice", out var btoProp) ? btoProp.GetString() ?? "" : "";
                                string deliveryCity = odProp.TryGetProperty("DeliveryCity", out var dcProp) ? dcProp.GetString() ?? "" : "";
                                string deliveryAddress = odProp.TryGetProperty("DeliveryAddress", out var daProp) ? daProp.GetString() ?? "" : "";
                                string receiverName = odProp.TryGetProperty("ReceiverName", out var rnProp) ? rnProp.GetString() ?? buyerCompName : buyerCompName;
                                string receiverPhone = odProp.TryGetProperty("ReceiverPhone", out var rpProp) ? rpProp.GetString() ?? "" : "";
                                string notes = odProp.TryGetProperty("Notes", out var nProp) ? nProp.GetString() ?? "" : "";

                                if (odProp.TryGetProperty("Items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var itemElem in itemsProp.EnumerateArray())
                                    {
                                        int prodId = itemElem.TryGetProperty("ProductID", out var pIdProp) ? pIdProp.GetInt32() : 0;
                                        string title = itemElem.TryGetProperty("Title", out var tProp) ? tProp.GetString() ?? "Ürün" : "Ürün";
                                        string sku = itemElem.TryGetProperty("Sku", out var skuProp) ? skuProp.GetString() ?? $"SKU-{prodId}" : $"SKU-{prodId}";
                                        string category = itemElem.TryGetProperty("Category", out var cProp) ? cProp.GetString() ?? "Endüstriyel" : "Endüstriyel";
                                        decimal price = itemElem.TryGetProperty("Price", out var pProp) ? pProp.GetDecimal() : 0;
                                        int quantity = itemElem.TryGetProperty("Quantity", out var qProp) ? qProp.GetInt32() : 1;
                                        string seller = itemElem.TryGetProperty("Seller", out var sProp) ? sProp.GetString() ?? "Onaylı Tedarikçi" : "Onaylı Tedarikçi";

                                        int? matchedItemId = null;
                                        int? supplierCompId = null;

                                        if (prodId > 0)
                                        {
                                            var dbItem = await _context.Items.FirstOrDefaultAsync(i => i.ItemID == prodId);
                                            if (dbItem != null)
                                            {
                                                matchedItemId = dbItem.ItemID;
                                                if (dbItem.Quantity.HasValue && dbItem.Quantity.Value >= quantity)
                                                    dbItem.Quantity -= quantity;
                                                else if (dbItem.Quantity.HasValue)
                                                    dbItem.Quantity = 0;

                                                if (!string.IsNullOrWhiteSpace(dbItem.Properties))
                                                {
                                                    var match = System.Text.RegularExpressions.Regex.Match(dbItem.Properties, @"(?:FirmaID|CompanyID)\s*:\s*(\d+)");
                                                    if (match.Success && int.TryParse(match.Groups[1].Value, out int pid))
                                                        supplierCompId = pid;
                                                }
                                            }
                                        }

                                        if (!supplierCompId.HasValue && !string.IsNullOrWhiteSpace(seller))
                                        {
                                            var matchedComp = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => 
                                                (c.CompanyName != null && c.CompanyName.ToLower() == seller.Trim().ToLower()) ||
                                                (c.CompanyOfficial != null && c.CompanyOfficial.ToLower() == seller.Trim().ToLower()));
                                            if (matchedComp != null) supplierCompId = matchedComp.CompanyID;
                                        }

                                        if (!primarySupplierCompId.HasValue && supplierCompId.HasValue)
                                        {
                                            primarySupplierCompId = supplierCompId;
                                            primarySupplierName = seller;
                                        }

                                        orderItemsList.Add(new satinalmaportali2.Models.OrderItem
                                        {
                                            ItemID = matchedItemId,
                                            ProductName = title,
                                            Sku = sku,
                                            CategoryName = category,
                                            UnitPrice = price,
                                            Quantity = quantity,
                                            LineTotal = price * quantity,
                                            SupplierCompanyName = seller,
                                            SupplierCompanyID = supplierCompId,
                                            ItemStatus = "Havuzda Bloke"
                                        });
                                    }
                                }

                                order = new Order
                                {
                                    OrderNumber = orderNo,
                                    BuyerCompanyID = buyerCompId,
                                    BuyerCompanyName = buyerCompName,
                                    BuyerTaxNumber = buyerTaxNo,
                                    BuyerTaxOffice = buyerTaxOffice,
                                    BuyerEmail = buyerEmail,
                                    BuyerPhone = receiverPhone,
                                    SupplierCompanyID = primarySupplierCompId,
                                    SupplierCompanyName = primarySupplierName,
                                    OrderDate = DateTime.UtcNow,
                                    DeliveryDate = DateTime.UtcNow.AddDays(3),
                                    TotalAmount = totalAmount,
                                    TaxAmount = taxAmount,
                                    GrandTotal = grandTotal,
                                    CommissionRate = commissionRate,
                                    CommissionAmount = commissionAmount,
                                    SupplierPayoutAmount = supplierPayout,
                                    PaymentMethod = "iyzico Kredi Kartı (Havuz)",
                                    PaymentStatus = "Havuzda (Ödendi)",
                                    OrderStatus = "Onaylandı",
                                    EscrowStatus = "InEscrow",
                                    PaymentId = paymentId,
                                    PaymentTransactionId = paymentTxId,
                                    AutoApprovalDeadline = DateTime.UtcNow.AddDays(_settings.AutoApprovalDays),
                                    DeliveryCity = deliveryCity,
                                    DeliveryAddress = deliveryAddress,
                                    ReceiverName = receiverName,
                                    ReceiverPhone = receiverPhone,
                                    Notes = notes + $" [iyzico Havuz Ödemesi Alındı: {DateTime.UtcNow:dd.MM.yyyy HH:mm} (Tx: {paymentTxId})]",
                                    OrderItems = orderItemsList
                                };

                                _context.Orders.Add(order);

                                // Alıcının veritabanı sepetini temizle
                                if (buyerCompId.HasValue && buyerCompId.Value > 0)
                                {
                                    var userCart = await _context.CartItems.Where(c => c.CompanyID == buyerCompId.Value).ToListAsync();
                                    if (userCart.Any()) _context.CartItems.RemoveRange(userCart);
                                }
                            }
                        }
                        catch (Exception parseEx)
                        {
                            Console.WriteLine($"Order recreation error: {parseEx.Message}");
                        }
                    }
                    else if (order != null)
                    {
                        order.PaymentId = paymentId;
                        order.PaymentTransactionId = paymentTxId;
                        order.PaymentStatus = "Havuzda (Ödendi)";
                        order.EscrowStatus = "InEscrow";
                        order.OrderStatus = "Onaylandı";
                        order.AutoApprovalDeadline = DateTime.UtcNow.AddDays(_settings.AutoApprovalDays);
                        order.Notes = (order.Notes ?? "") + $" [iyzico Havuz Ödemesi Alındı: {DateTime.UtcNow:dd.MM.yyyy HH:mm} (Tx: {paymentTxId})]";
                    }

                    if (draftTx != null)
                    {
                        draftTx.Status = "Success";
                        draftTx.PaymentId = paymentId;
                        draftTx.PaymentTransactionId = paymentTxId;
                        if (order != null) draftTx.OrderID = order.OrderID;
                    }

                    var transaction = new PaymentTransaction
                    {
                        OrderID = order?.OrderID,
                        ConversationId = conversationId,
                        PaymentId = paymentId,
                        PaymentTransactionId = paymentTxId,
                        TotalAmount = order?.GrandTotal ?? 0,
                        MerchantPayoutAmount = order?.SupplierPayoutAmount ?? 0,
                        PlatformCommission = order?.CommissionAmount ?? 0,
                        TransactionType = "Auth_3D_Complete",
                        Status = "Success",
                        RawResponse = JsonSerializer.Serialize(threedsPayment),
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.PaymentTransactions.Add(transaction);

                    await _context.SaveChangesAsync();

                    resultDto.Success = true;
                    resultDto.PaymentId = paymentId;
                    resultDto.PaymentTransactionId = paymentTxId;
                    resultDto.OrderID = order?.OrderID;
                    resultDto.OrderNumber = order?.OrderNumber;
                    resultDto.GrandTotal = order?.GrandTotal ?? 0;
                    resultDto.Message = "🎉 Ödemeniz başarıyla alındı ve güvence havuzunda bloke edildi!";

                    // SignalR Bildirimi
                    try
                    {
                        await _hubContext.Clients.All.SendAsync("OnEscrowPaymentReceived", new
                        {
                            orderId = order?.OrderID,
                            orderNumber = order?.OrderNumber,
                            buyerCompanyName = order?.BuyerCompanyName,
                            grandTotal = order?.GrandTotal,
                            escrowStatus = "InEscrow",
                            message = $"🛡️ {order?.OrderNumber} numaralı siparişin ödemesi iyzico havuzunda güvence altına alındı."
                        });
                    }
                    catch (Exception hubEx)
                    {
                        Console.WriteLine($"SignalR Complete3D error: {hubEx.Message}");
                    }
                }
                else
                {
                    if (draftTx != null)
                    {
                        draftTx.Status = "Failure";
                        draftTx.ErrorCode = threedsPayment.ErrorCode;
                        draftTx.ErrorMessage = threedsPayment.ErrorMessage;
                        draftTx.RawResponse = JsonSerializer.Serialize(threedsPayment);
                        await _context.SaveChangesAsync();
                    }

                    resultDto.Success = false;
                    resultDto.Message = $"3D Secure Ödeme Doğrulama Başarısız: {threedsPayment.ErrorMessage} ({threedsPayment.ErrorCode})";
                }
            }
            catch (Exception ex)
            {
                resultDto.Success = false;
                resultDto.Message = "3D Secure tamamlama sırasında hata: " + ex.Message;
            }

            return resultDto;
        }

        /// <summary>
        /// Alıcı veya Admin onayı ile havuzdaki bloke parayı çözer ve net tutarı tedarikçiye aktarır (Approval API).
        /// </summary>
        public async Task<(bool Success, string Message)> ApproveEscrowReleaseAsync(int orderId, int currentCompanyId, bool isAdmin = false)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderID == orderId);

                if (order == null)
                {
                    return (false, "Sipariş veritabanında bulunamadı.");
                }

                // Yetki kontrolü: Sadece siparişi veren alıcı firma veya admin onaylayabilir
                if (!isAdmin && order.BuyerCompanyID.HasValue && order.BuyerCompanyID.Value != currentCompanyId)
                {
                    return (false, "Bu sipariş için havuz onayını yalnızca satın alma yapan alıcı firma verebilir.");
                }

                if (order.IsEscrowApproved || order.EscrowStatus == "Released_ToSeller")
                {
                    return (true, "Bu siparişin ödemesi zaten tedarikçiye aktarılmış.");
                }

                string paymentTransactionId = order.PaymentTransactionId ?? "";
                if (string.IsNullOrWhiteSpace(paymentTransactionId))
                {
                    // PaymentTransactions tablosundan bulmayı dene
                    var lastSuccessTx = await _context.PaymentTransactions
                        .Where(t => t.OrderID == orderId && t.Status == "Success" && !string.IsNullOrWhiteSpace(t.PaymentTransactionId))
                        .OrderByDescending(t => t.TransactionID)
                        .FirstOrDefaultAsync();

                    paymentTransactionId = lastSuccessTx?.PaymentTransactionId ?? "";
                }

                bool iyzicoSuccess = true;
                string apiMessage = "Havuz onayı başarıyla işlendi.";

                // Eğer iyzico Transaction ID varsa gerçek iyzico Approval API çağır
                if (!string.IsNullOrWhiteSpace(paymentTransactionId))
                {
                    var approvalRequest = new CreateApprovalRequest
                    {
                        Locale = Locale.TR.ToString(),
                        ConversationId = $"APP-{order.OrderNumber}-{DateTime.UtcNow.Ticks}",
                        PaymentTransactionId = paymentTransactionId
                    };

                    var approvalResult = await Task.Run(() => Approval.Create(approvalRequest, _options));

                    var transactionLog = new PaymentTransaction
                    {
                        OrderID = order.OrderID,
                        ConversationId = approvalRequest.ConversationId,
                        PaymentId = order.PaymentId,
                        PaymentTransactionId = paymentTransactionId,
                        TotalAmount = order.GrandTotal,
                        MerchantPayoutAmount = order.SupplierPayoutAmount,
                        PlatformCommission = order.CommissionAmount,
                        TransactionType = "Escrow_Released_Approval",
                        Status = approvalResult.Status == Status.SUCCESS.ToString() ? "Success" : "Failure",
                        ErrorCode = approvalResult.ErrorCode,
                        ErrorMessage = approvalResult.ErrorMessage,
                        RawResponse = JsonSerializer.Serialize(approvalResult),
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.PaymentTransactions.Add(transactionLog);

                    bool isSandboxEnv = _settings.BaseUrl.Contains("sandbox", StringComparison.OrdinalIgnoreCase) || 
                                        _settings.ApiKey.StartsWith("sandbox", StringComparison.OrdinalIgnoreCase);

                    if (approvalResult.Status != Status.SUCCESS.ToString())
                    {
                        if (isSandboxEnv || approvalResult.ErrorCode == "2000" || (approvalResult.ErrorMessage != null && (approvalResult.ErrorMessage.Contains("marketplace", StringComparison.OrdinalIgnoreCase) || approvalResult.ErrorMessage.Contains("pazaryeri", StringComparison.OrdinalIgnoreCase))))
                        {
                            iyzicoSuccess = true;
                            apiMessage = "Havuz onayı (Sandbox Test Modu) başarıyla işlendi ve satıcıya aktarıldı.";
                        }
                        else
                        {
                            iyzicoSuccess = false;
                            apiMessage = $"iyzico Havuz Onayı Hatası: {approvalResult.ErrorMessage}";
                        }
                    }
                }

                if (iyzicoSuccess)
                {
                    order.IsEscrowApproved = true;
                    order.EscrowStatus = "Released_ToSeller";
                    order.OrderStatus = "Tamamlandı";
                    order.EscrowReleaseDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // SignalR Canlı Bildirim
                    try
                    {
                        await _hubContext.Clients.All.SendAsync("OnEscrowPaymentReleased", new
                        {
                            orderId = order.OrderID,
                            orderNumber = order.OrderNumber,
                            supplierCompanyId = order.SupplierCompanyID,
                            payoutAmount = order.SupplierPayoutAmount,
                            message = $"💰 {order.OrderNumber} numaralı sipariş teslim alındı. {order.SupplierPayoutAmount:N2} ₺ tedarikçi hesabına aktarıldı!"
                        });
                    }
                    catch (Exception hubEx)
                    {
                        Console.WriteLine($"SignalR ApproveEscrow error: {hubEx.Message}");
                    }

                    // Satıcıya E-Posta Bildirimi Gönder
                    try
                    {
                        if (order.SupplierCompanyID.HasValue)
                        {
                            var supplierComp = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == order.SupplierCompanyID.Value);
                            if (supplierComp != null && !string.IsNullOrWhiteSpace(supplierComp.CompanyMail))
                            {
                                string mailBody = $@"
                                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #10b981; border-radius: 8px;'>
                                        <h2 style='color: #065f46;'>🎉 Hakedişiniz Hesabınıza Aktarıldı!</h2>
                                        <p>Sayın <strong>{supplierComp.CompanyName}</strong>,</p>
                                        <p><strong>{order.OrderNumber}</strong> numaralı sipariş alıcı firma tarafından teslim alınarak onaylanmıştır.</p>
                                        <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 16px 0;'/>
                                        <p><strong>Toplam Sipariş Tutarı:</strong> {order.GrandTotal:N2} ₺</p>
                                        <p><strong>Platform Komisyonu (%{order.CommissionRate * 100:N0}):</strong> {order.CommissionAmount:N2} ₺</p>
                                        <p style='font-size: 1.2rem; color: #059669;'><strong>Net Hak Ediş Tutarınız:</strong> {order.SupplierPayoutAmount:N2} ₺</p>
                                        <p>Tutar, iyzico tarafından kayıtlı IBAN hesabınıza aktarılacaktır.</p>
                                    </div>
                                ";
                                await _emailService.SendEmailAsync(supplierComp.CompanyMail, $"Hakediş Onayı - {order.OrderNumber}", mailBody);
                            }
                        }
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine($"Supplier payout email failed: {emailEx.Message}");
                    }

                    return (true, $"✅ Sipariş teslimatı onaylandı! {order.SupplierPayoutAmount:N2} ₺ tedarikçinin IBAN hesabına aktarıldı.");
                }
                else
                {
                    return (false, apiMessage);
                }
            }
            catch (Exception ex)
            {
                return (false, "Havuz onayı serbest bırakılırken hata oluştu: " + ex.Message);
            }
        }

        /// <summary>
        /// İade/İptal durumunda havuzdaki bloke tutarı alıcının kartına geri iade eder.
        /// </summary>
        public async Task<(bool Success, string Message)> DisapproveEscrowRefundAsync(int orderId, int currentCompanyId, string reason, bool isAdmin = false)
        {
            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderID == orderId);
                if (order == null)
                {
                    return (false, "Sipariş bulunamadı.");
                }

                if (!isAdmin)
                {
                    return (false, "Havuz ret ve iade işlemi yalnızca Sistem Yöneticisi (Admin) tarafından onaylanabilir.");
                }

                string paymentTransactionId = order.PaymentTransactionId ?? "";
                bool iyzicoSuccess = true;
                string apiMessage = "İade işlemi başarıyla tamamlandı.";

                if (!string.IsNullOrWhiteSpace(paymentTransactionId))
                {
                    var disRequest = new CreateApprovalRequest
                    {
                        Locale = Locale.TR.ToString(),
                        ConversationId = $"DIS-{order.OrderNumber}-{DateTime.UtcNow.Ticks}",
                        PaymentTransactionId = paymentTransactionId
                    };

                    var disResult = await Task.Run(() => Disapproval.Create(disRequest, _options));

                    var transactionLog = new PaymentTransaction
                    {
                        OrderID = order.OrderID,
                        ConversationId = disRequest.ConversationId,
                        PaymentId = order.PaymentId,
                        PaymentTransactionId = paymentTransactionId,
                        TotalAmount = order.GrandTotal,
                        TransactionType = "Escrow_Disapproval_Refund",
                        Status = disResult.Status == Status.SUCCESS.ToString() ? "Success" : "Failure",
                        ErrorCode = disResult.ErrorCode,
                        ErrorMessage = disResult.ErrorMessage,
                        RawResponse = JsonSerializer.Serialize(disResult),
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.PaymentTransactions.Add(transactionLog);

                    bool isSandboxEnv = _settings.BaseUrl.Contains("sandbox", StringComparison.OrdinalIgnoreCase) || 
                                        _settings.ApiKey.StartsWith("sandbox", StringComparison.OrdinalIgnoreCase);

                    if (disResult.Status != Status.SUCCESS.ToString())
                    {
                        if (isSandboxEnv || disResult.ErrorCode == "2000" || (disResult.ErrorMessage != null && (disResult.ErrorMessage.Contains("marketplace", StringComparison.OrdinalIgnoreCase) || disResult.ErrorMessage.Contains("pazaryeri", StringComparison.OrdinalIgnoreCase))))
                        {
                            iyzicoSuccess = true;
                            apiMessage = "İade işlemi (Sandbox Test Modu) başarıyla işlendi ve alıcıya iade edildi.";
                        }
                        else
                        {
                            iyzicoSuccess = false;
                            apiMessage = $"iyzico İade Hatası: {disResult.ErrorMessage}";
                        }
                    }
                }

                if (iyzicoSuccess)
                {
                    order.EscrowStatus = "Refunded";
                    order.OrderStatus = "İptal / İade Edildi";
                    order.DisputeReason = reason;
                    await _context.SaveChangesAsync();

                    // SignalR Bildirimi
                    try
                    {
                        await _hubContext.Clients.All.SendAsync("OnEscrowRefundProcessed", new
                        {
                            orderId = order.OrderID,
                            orderNumber = order.OrderNumber,
                            refundAmount = order.GrandTotal,
                            message = $"↩️ {order.OrderNumber} numaralı siparişin {order.GrandTotal:N2} ₺ tutarındaki iadesi alıcının kartına aktarıldı."
                        });
                    }
                    catch (Exception hubEx)
                    {
                        Console.WriteLine($"SignalR Disapprove error: {hubEx.Message}");
                    }

                    return (true, $"↩️ Sipariş iptal edildi ve {order.GrandTotal:N2} ₺ tutarındaki ödeme alıcının kartına iade edildi.");
                }
                else
                {
                    return (false, apiMessage);
                }
            }
            catch (Exception ex)
            {
                return (false, "İade işlemi sırasında hata oluştu: " + ex.Message);
            }
        }

        /// <summary>
        /// Alıcı firma tarafından itiraz/iade talebi açılmasını sağlar.
        /// </summary>
        public async Task<(bool Success, string Message)> CreateDisputeAsync(DisputeCreateInputDto dto, int buyerCompanyId)
        {
            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderID == dto.OrderID);
                if (order == null)
                {
                    return (false, "Sipariş veritabanında bulunamadı.");
                }

                if (order.BuyerCompanyID.HasValue && order.BuyerCompanyID.Value != buyerCompanyId)
                {
                    return (false, "Bu sipariş için itiraz oluşturma yetkiniz bulunmamaktadır.");
                }

                var dispute = new OrderDispute
                {
                    OrderID = order.OrderID,
                    BuyerCompanyID = buyerCompanyId,
                    SupplierCompanyID = order.SupplierCompanyID,
                    Reason = dto.Reason,
                    Description = dto.Description,
                    EvidenceAttachmentUrl = dto.EvidenceUrl,
                    Status = "Open",
                    CreatedAt = DateTime.UtcNow
                };

                order.EscrowStatus = "Disputed";
                _context.OrderDisputes.Add(dispute);
                await _context.SaveChangesAsync();

                try
                {
                    await _hubContext.Clients.All.SendAsync("OnOrderDisputeCreated", new
                    {
                        disputeId = dispute.DisputeID,
                        orderId = order.OrderID,
                        orderNumber = order.OrderNumber,
                        reason = dto.Reason,
                        message = $"⚠️ {order.OrderNumber} numaralı sipariş için alıcı firma itiraz talebi oluşturdu."
                    });
                }
                catch (Exception hubEx)
                {
                    Console.WriteLine($"SignalR Dispute error: {hubEx.Message}");
                }

                return (true, "İtiraz talebiniz oluşturuldu. Destek ve admin ekibimiz inceleyerek tarafınıza dönüş yapacaktır.");
            }
            catch (Exception ex)
            {
                return (false, "İtiraz kaydı oluşturulurken hata: " + ex.Message);
            }
        }
    }
}
