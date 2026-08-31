using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using satinalmaportali2.Data;
using satinalmaportali2.Hubs;
using satinalmaportali2.Models;
using satinalmaportali2.Services;

namespace satinalmaportali2.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly IHubContext<PortalHub> _hubContext;

        public OrderController(ApplicationDbContext context, EmailService emailService, IHubContext<PortalHub> hubContext)
        {
            _context = context;
            _emailService = emailService;
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

        private static bool IsProductOwnedByCompany(ItemsModel item, int currentCompanyId, string currentCompanyName, Company? comp)
        {
            if (currentCompanyId <= 0 && string.IsNullOrWhiteSpace(currentCompanyName))
                return false;

            if (!string.IsNullOrWhiteSpace(item.Properties) && currentCompanyId > 0)
            {
                if (item.Properties.Contains($"FirmaID: {currentCompanyId}") ||
                    item.Properties.Contains($"CompanyID: {currentCompanyId}") ||
                    item.Properties.Contains($"FirmaID:{currentCompanyId}") ||
                    item.Properties.Contains($"CompanyID:{currentCompanyId}"))
                {
                    return true;
                }
            }

            if (!string.IsNullOrWhiteSpace(item.Brand))
            {
                var brand = item.Brand.Trim();
                if (!string.IsNullOrWhiteSpace(currentCompanyName) && string.Equals(brand, currentCompanyName.Trim(), StringComparison.OrdinalIgnoreCase))
                    return true;

                if (comp != null)
                {
                    if (!string.IsNullOrWhiteSpace(comp.CompanyName) && string.Equals(brand, comp.CompanyName.Trim(), StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (!string.IsNullOrWhiteSpace(comp.CompanyOfficial) && string.Equals(brand, comp.CompanyOfficial.Trim(), StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (!string.IsNullOrWhiteSpace(comp.CompanyMail) && string.Equals(brand, comp.CompanyMail.Trim(), StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Oturum açmış firmanın veritabanında kayıtlı kurumsal fatura ve teslimat profil bilgilerini döner.
        /// Satın alma modalında alanların otomatik doldurulmasını sağlar.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCheckoutProfile()
        {
            try
            {
                int companyId = GetCurrentCompanyId();
                if (companyId <= 0)
                {
                    return Json(new { isAuthenticated = false });
                }

                var comp = await _context.Companies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CompanyID == companyId);

                if (comp == null)
                {
                    return Json(new { isAuthenticated = false });
                }

                string addressText = "";
                string deliveryCity = "";

                if (comp.AddressID.HasValue && comp.AddressID.Value > 0)
                {
                    var addr = await _context.Addresses
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.AddressID == comp.AddressID.Value);

                    if (addr != null)
                    {
                        addressText = addr.AddressText ?? "";
                        if (addr.DistrictID.HasValue)
                        {
                            var district = await _context.Districts
                                .AsNoTracking()
                                .FirstOrDefaultAsync(d => d.DistrictID == addr.DistrictID.Value);
                            if (district != null)
                            {
                                string dName = district.DistrictName ?? "";
                                if (district.TownID.HasValue)
                                {
                                    var town = await _context.Towns
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync(t => t.TownID == district.TownID.Value);
                                    if (town != null)
                                    {
                                        deliveryCity = $"{town.TownName} / {dName}".Trim(' ', '/');
                                    }
                                    else
                                    {
                                        deliveryCity = dName;
                                    }
                                }
                                else
                                {
                                    deliveryCity = dName;
                                }
                            }
                        }
                    }
                }

                return Json(new
                {
                    isAuthenticated = true,
                    companyId = comp.CompanyID,
                    companyName = comp.CompanyName ?? comp.CompanyOfficial ?? "",
                    companyOfficial = comp.CompanyOfficial ?? "",
                    taxNumber = comp.TCNO ?? "",
                    taxOffice = comp.TaxOffice ?? "",
                    phone = comp.CompanyPhone ?? "",
                    email = comp.CompanyMail ?? "",
                    deliveryCity = deliveryCity,
                    deliveryAddress = addressText
                });
            }
            catch (Exception ex)
            {
                return Json(new { isAuthenticated = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Doğrudan sepetteki ürünler veya hızlı satın alma için sipariş oluşturan AJAX endpoint.
        /// Firma kendi satışa sunduğu ürünü satın alamaz.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderInputModel? input)
        {
            if (input == null || input.Items == null || !input.Items.Any())
            {
                return Json(new { success = false, message = "Sepetinizde ürün bulunmamaktadır." });
            }

            try
            {
                int currentCompanyId = GetCurrentCompanyId();
                string currentCompanyName = User.FindFirst("CompanyName")?.Value ?? "";
                string currentEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? input.BuyerEmail ?? "";
                Company? comp = null;

                // Firma varlık doğrulaması ve veritabanı profilinden eksik alanları tamamlama
                if (currentCompanyId > 0)
                {
                    comp = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId);
                    if (comp != null)
                    {
                        if (string.IsNullOrWhiteSpace(currentCompanyName)) currentCompanyName = comp.CompanyName ?? comp.CompanyOfficial ?? "";
                        if (string.IsNullOrWhiteSpace(input.BuyerCompany)) input.BuyerCompany = comp.CompanyName ?? comp.CompanyOfficial ?? "";
                        if (string.IsNullOrWhiteSpace(input.BuyerTaxNumber)) input.BuyerTaxNumber = comp.TCNO;
                        if (string.IsNullOrWhiteSpace(input.BuyerTaxOffice)) input.BuyerTaxOffice = comp.TaxOffice;
                        if (string.IsNullOrWhiteSpace(input.ReceiverPhone)) input.ReceiverPhone = comp.CompanyPhone;
                        if (string.IsNullOrWhiteSpace(input.ReceiverName)) input.ReceiverName = comp.CompanyOfficial ?? comp.CompanyName;

                        if (comp.AddressID.HasValue && comp.AddressID.Value > 0 && 
                            (string.IsNullOrWhiteSpace(input.DeliveryCity) || string.IsNullOrWhiteSpace(input.DeliveryAddress)))
                        {
                            var addr = await _context.Addresses.AsNoTracking().FirstOrDefaultAsync(a => a.AddressID == comp.AddressID.Value);
                            if (addr != null)
                            {
                                if (string.IsNullOrWhiteSpace(input.DeliveryAddress)) input.DeliveryAddress = addr.AddressText ?? "";
                                if (string.IsNullOrWhiteSpace(input.DeliveryCity) && addr.DistrictID.HasValue)
                                {
                                    var district = await _context.Districts.AsNoTracking().FirstOrDefaultAsync(d => d.DistrictID == addr.DistrictID.Value);
                                    if (district != null)
                                    {
                                        var town = district.TownID.HasValue ? await _context.Towns.AsNoTracking().FirstOrDefaultAsync(t => t.TownID == district.TownID.Value) : null;
                                        input.DeliveryCity = town != null ? $"{town.TownName} / {district.DistrictName}" : district.DistrictName ?? "";
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        currentCompanyId = 0;
                    }
                }

                if (string.IsNullOrWhiteSpace(input.BuyerCompany) || string.IsNullOrWhiteSpace(input.DeliveryCity) || string.IsNullOrWhiteSpace(input.DeliveryAddress))
                {
                    return Json(new { success = false, message = "Lütfen firma ünvanı, teslimat şehri ve adresini eksiksiz doldurunuz." });
                }

                if (!string.IsNullOrWhiteSpace(input.BuyerTaxNumber))
                {
                    var cleanTax = new string(input.BuyerTaxNumber.Where(char.IsDigit).ToArray());
                    if (cleanTax.Length != 10 && cleanTax.Length != 11)
                    {
                        return Json(new { success = false, message = "Vergi Kimlik Numarası 10 hane veya TC Kimlik Numarası 11 hane olmalıdır." });
                    }
                    input.BuyerTaxNumber = cleanTax;
                }

                if (!string.IsNullOrWhiteSpace(input.ReceiverPhone))
                {
                    var cleanPhone = new string(input.ReceiverPhone.Where(char.IsDigit).ToArray());
                    if (cleanPhone.StartsWith("0")) cleanPhone = cleanPhone.Substring(1);
                    if (cleanPhone.Length != 10)
                    {
                        return Json(new { success = false, message = "Telefon numarası başında 0 olmadan 10 hane olmalıdır (Örn: 5321234567)." });
                    }
                    input.ReceiverPhone = "0" + cleanPhone;
                }

                // Kendi ürününü satın alma kontrolü
                var itemProductIds = input.Items.Where(i => i.ProductID > 0).Select(i => i.ProductID).Distinct().ToList();
                if (itemProductIds.Any())
                {
                    var checkedDbItems = await _context.Items.AsNoTracking()
                        .Where(i => itemProductIds.Contains(i.ItemID))
                        .ToListAsync();

                    foreach (var dbIt in checkedDbItems)
                    {
                        if (IsProductOwnedByCompany(dbIt, currentCompanyId, currentCompanyName, comp))
                        {
                            return Json(new { success = false, message = $"Kendi satışa sunduğunuz ürünü ({dbIt.ItemName}) satın alamazsınız." });
                        }
                    }
                }

                string finalBuyerName = !string.IsNullOrWhiteSpace(input.BuyerCompany) 
                    ? input.BuyerCompany.Trim() 
                    : (!string.IsNullOrWhiteSpace(currentCompanyName) ? currentCompanyName : "Kurumsal Alıcı");

                // Sipariş Numarası Üret
                int countToday = await _context.Orders.CountAsync(o => o.OrderDate.Date == DateTime.UtcNow.Date);
                string orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{(countToday + 1):D4}";

                decimal totalAmount = 0;
                var orderItemsList = new List<OrderItem>();

                foreach (var item in input.Items)
                {
                    if (item.Quantity <= 0) item.Quantity = 1;
                    decimal lineTotal = item.Price * item.Quantity;
                    totalAmount += lineTotal;

                    // Eğer veritabanında bu ItemID varsa stok düşümü yapalım
                    int? matchedItemId = null;
                    int? supplierCompId = null;
                    string supplierName = item.Seller ?? "Onaylı Tedarikçi";

                    if (item.ProductID > 0)
                    {
                        var dbItem = await _context.Items.FirstOrDefaultAsync(i => i.ItemID == item.ProductID);
                        if (dbItem != null)
                        {
                            matchedItemId = dbItem.ItemID;
                            if (!string.IsNullOrWhiteSpace(dbItem.Brand)) supplierName = dbItem.Brand;

                            // Stok düşümü
                            if (dbItem.Quantity.HasValue && dbItem.Quantity.Value >= item.Quantity)
                            {
                                dbItem.Quantity -= item.Quantity;
                            }
                            else if (dbItem.Quantity.HasValue)
                            {
                                dbItem.Quantity = 0;
                            }

                            // Properties içindeki FirmaID kontrolü
                            if (!string.IsNullOrWhiteSpace(dbItem.Properties))
                            {
                                var match = System.Text.RegularExpressions.Regex.Match(dbItem.Properties, @"(?:FirmaID|CompanyID)\s*:\s*(\d+)");
                                if (match.Success && int.TryParse(match.Groups[1].Value, out int pid))
                                {
                                    supplierCompId = pid;
                                }
                            }
                        }
                    }

                    if (!supplierCompId.HasValue && !string.IsNullOrWhiteSpace(supplierName))
                    {
                        var matchedComp = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => 
                            (c.CompanyName != null && c.CompanyName.ToLower() == supplierName.Trim().ToLower()) ||
                            (c.CompanyOfficial != null && c.CompanyOfficial.ToLower() == supplierName.Trim().ToLower()));
                        if (matchedComp != null)
                        {
                            supplierCompId = matchedComp.CompanyID;
                            if (string.IsNullOrWhiteSpace(supplierName) && !string.IsNullOrWhiteSpace(matchedComp.CompanyName))
                            {
                                supplierName = matchedComp.CompanyName;
                            }
                        }
                    }

                    orderItemsList.Add(new OrderItem
                    {
                        ItemID = matchedItemId,
                        ProductName = item.Title,
                        Sku = item.Sku ?? $"SKU-{item.ProductID}",
                        CategoryName = item.Category ?? "Endüstriyel",
                        UnitPrice = item.Price,
                        Quantity = item.Quantity,
                        LineTotal = lineTotal,
                        SupplierCompanyName = supplierName,
                        SupplierCompanyID = supplierCompId,
                        ItemStatus = "Onaylandı"
                    });
                }

                decimal taxRate = 0.20m; // %20 KDV
                decimal taxAmount = Math.Round(totalAmount * taxRate, 2);
                decimal grandTotal = totalAmount + taxAmount;

                string paymentStatus = input.PaymentMethod == "Kredi Kartı" ? "Ödendi" : 
                                      (input.PaymentMethod == "Cari Hesap / Vadeli" ? "Vadeli Onaylı" : "Beklemede");

                var order = new Order
                {
                    OrderNumber = orderNumber,
                    BuyerCompanyID = currentCompanyId > 0 ? currentCompanyId : null,
                    BuyerCompanyName = finalBuyerName,
                    BuyerTaxNumber = input.BuyerTaxNumber,
                    BuyerTaxOffice = input.BuyerTaxOffice,
                    BuyerEmail = currentEmail,
                    BuyerPhone = input.BuyerPhone ?? input.ReceiverPhone,
                    SupplierCompanyID = orderItemsList.FirstOrDefault(oi => oi.SupplierCompanyID.HasValue)?.SupplierCompanyID,
                    SupplierCompanyName = orderItemsList.FirstOrDefault()?.SupplierCompanyName ?? "Onaylı Tedarikçi",
                    OrderDate = DateTime.UtcNow,
                    DeliveryDate = DateTime.UtcNow.AddDays(3),
                    TotalAmount = totalAmount,
                    TaxAmount = taxAmount,
                    GrandTotal = grandTotal,
                    PaymentMethod = input.PaymentMethod,
                    PaymentStatus = paymentStatus,
                    OrderStatus = "Onaylandı",
                    DeliveryCity = input.DeliveryCity.Trim(),
                    DeliveryAddress = input.DeliveryAddress.Trim(),
                    ReceiverName = input.ReceiverName ?? finalBuyerName,
                    ReceiverPhone = input.ReceiverPhone,
                    Notes = input.Notes,
                    OrderItems = orderItemsList
                };

                _context.Orders.Add(order);

                // Kullanıcının veritabanı sepetini temizle
                if (currentCompanyId > 0)
                {
                    var userCartItems = await _context.CartItems.Where(c => c.CompanyID == currentCompanyId).ToListAsync();
                    if (userCartItems.Any())
                    {
                        _context.CartItems.RemoveRange(userCartItems);
                    }
                }

                await _context.SaveChangesAsync();

                // SignalR ile Anlık Bildirim ve Sipariş Senkronizasyonu
                try
                {
                    var orderBroadcast = new
                    {
                        orderId = order.OrderID,
                        orderNumber = order.OrderNumber,
                        buyerCompanyName = order.BuyerCompanyName,
                        supplierCompanyName = order.SupplierCompanyName,
                        totalAmount = order.TotalAmount,
                        grandTotal = order.GrandTotal,
                        grandTotalFormatted = order.GrandTotal.ToString("N0") + " ₺",
                        orderDate = order.OrderDate.ToString("dd.MM.yyyy HH:mm"),
                        orderStatus = order.OrderStatus,
                        paymentStatus = order.PaymentStatus,
                        itemCount = order.OrderItems.Count,
                        firstItemTitle = order.OrderItems.FirstOrDefault()?.ProductName ?? "Sipariş Kalemi"
                    };

                    await _hubContext.Clients.All.SendAsync("OnOrderPlaced", orderBroadcast);

                    // Stok düşümü olan ürünlerin canlı güncellenmesi
                    foreach (var oi in orderItemsList.Where(x => x.ItemID.HasValue))
                    {
                        var updatedItem = await _context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.ItemID == oi.ItemID!.Value);
                        if (updatedItem != null)
                        {
                            await _hubContext.Clients.All.SendAsync("OnProductPriceChanged", new
                            {
                                itemId = updatedItem.ItemID,
                                itemName = updatedItem.ItemName,
                                newUnitPrice = (decimal)(updatedItem.ItemPrice ?? 0),
                                newUnitPriceFormatted = ((decimal)(updatedItem.ItemPrice ?? 0)).ToString("N0") + " ₺",
                                newStock = updatedItem.Quantity ?? 0,
                                status = (updatedItem.Quantity ?? 0) <= 0 ? "Tükendi" : ((updatedItem.Quantity ?? 0) < 50 ? "Kritik" : "Stokta"),
                                brand = updatedItem.Brand
                            });
                        }
                    }
                }
                catch (Exception hubEx)
                {
                    Console.WriteLine($"SignalR CreateOrder broadcast error: {hubEx.Message}");
                }

                // 1. Alıcı Firmaya Sipariş Onay E-Postası Gönder (Async Arka Plan)
                try
                {
                    if (!string.IsNullOrWhiteSpace(currentEmail))
                    {
                        string emailBody = $@"
                            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 8px;'>
                                <h2 style='color: #0f172a;'>Siparişiniz Başarıyla Alındı 🎉</h2>
                                <p>Sayın <strong>{finalBuyerName}</strong>,</p>
                                <p><strong>{orderNumber}</strong> numaralı kurumsal satın alım siparişiniz sisteme kaydedildi ve tedarikçiye iletildi.</p>
                                <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 16px 0;'/>
                                <h3>Sipariş Özeti</h3>
                                <p><strong>Ödeme Yöntemi:</strong> {order.PaymentMethod} ({order.PaymentStatus})</p>
                                <p><strong>Teslimat Adresi:</strong> {order.DeliveryCity} - {order.DeliveryAddress}</p>
                                <p><strong>Toplam Tutar:</strong> {order.GrandTotal:N2} ₺ (KDV Dahil)</p>
                                <br/>
                                <a href='https://localhost:7019/Dashboard/Orders' style='background: #0f172a; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 6px; display: inline-block;'>Siparişimi Takip Et</a>
                            </div>
                        ";
                        await _emailService.SendEmailAsync(currentEmail, $"Sipariş Onayı - {orderNumber}", emailBody);
                    }
                }
                catch (Exception emailEx)
                {
                    Console.WriteLine($"Order confirmation email to buyer failed: {emailEx.Message}");
                }

                // 2. Ürünleri Satın Alınan Tedarikçi/Satıcı Firmalara Bildirim E-Postası Gönder (Async Arka Plan)
                try
                {
                    // Siparişteki ürünleri satıcı firmaya göre grupla
                    var supplierGroups = orderItemsList
                        .GroupBy(oi => oi.SupplierCompanyID.HasValue && oi.SupplierCompanyID.Value > 0
                            ? oi.SupplierCompanyID.Value.ToString()
                            : (oi.SupplierCompanyName ?? "Onaylı Tedarikçi"))
                        .ToList();

                    foreach (var group in supplierGroups)
                    {
                        try
                        {
                            Company? supplierComp = null;
                            var firstItem = group.First();

                            if (firstItem.SupplierCompanyID.HasValue && firstItem.SupplierCompanyID.Value > 0)
                            {
                                supplierComp = await _context.Companies
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(c => c.CompanyID == firstItem.SupplierCompanyID.Value);
                            }

                            if (supplierComp == null && !string.IsNullOrWhiteSpace(firstItem.SupplierCompanyName))
                            {
                                var sName = firstItem.SupplierCompanyName.Trim().ToLower();
                                supplierComp = await _context.Companies
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(c => (c.CompanyName != null && c.CompanyName.ToLower() == sName) ||
                                                             (c.CompanyOfficial != null && c.CompanyOfficial.ToLower() == sName));
                            }

                            string targetEmail = supplierComp?.CompanyMail?.Trim() ?? "";
                            string displaySupplierName = supplierComp?.CompanyName ?? supplierComp?.CompanyOfficial ?? firstItem.SupplierCompanyName ?? "Değerli Tedarikçimiz";

                            if (!string.IsNullOrWhiteSpace(targetEmail))
                            {
                                var supplierItemsDto = group.Select((oi, idx) => new SoldProductItemEmailDto
                                {
                                    ItemOrder = idx + 1,
                                    ProductName = oi.ProductName,
                                    Sku = oi.Sku,
                                    CategoryName = oi.CategoryName,
                                    Quantity = oi.Quantity,
                                    Unit = "Adet",
                                    UnitPrice = oi.UnitPrice,
                                    LineTotal = oi.LineTotal
                                }).ToList();

                                decimal groupTotalAmount = supplierItemsDto.Sum(i => i.LineTotal);
                                decimal groupTaxAmount = Math.Round(groupTotalAmount * taxRate, 2);
                                decimal groupGrandTotal = groupTotalAmount + groupTaxAmount;

                                await _emailService.SendProductSoldSupplierNotificationEmailAsync(
                                    toEmail: targetEmail,
                                    supplierName: displaySupplierName,
                                    buyerCompanyName: finalBuyerName,
                                    receiverName: order.ReceiverName,
                                    receiverPhone: order.ReceiverPhone,
                                    deliveryCity: order.DeliveryCity,
                                    deliveryAddress: order.DeliveryAddress,
                                    orderNumber: order.OrderNumber,
                                    orderDate: order.OrderDate,
                                    paymentMethod: order.PaymentMethod,
                                    paymentStatus: order.PaymentStatus,
                                    notes: order.Notes,
                                    items: supplierItemsDto,
                                    totalAmount: groupTotalAmount,
                                    taxAmount: groupTaxAmount,
                                    grandTotal: groupGrandTotal,
                                    supplierOrdersUrl: "https://localhost:7019/Dashboard/SupplierOrders");

                                Console.WriteLine($"✅ Satıcı firmaya ({displaySupplierName} - {targetEmail}) ürün satış e-postası başarıyla gönderildi.");
                            }
                            else
                            {
                                Console.WriteLine($"⚠️ Satıcı firma ({displaySupplierName}) için kayıtlı e-posta adresi bulunamadı.");
                            }
                        }
                        catch (Exception supEmailEx)
                        {
                            Console.WriteLine($"Supplier order notification email error: {supEmailEx.Message}");
                        }
                    }
                }
                catch (Exception groupEx)
                {
                    Console.WriteLine($"Supplier email notification grouping error: {groupEx.Message}");
                }

                var distinctSellers = orderItemsList
                    .Where(oi => !string.IsNullOrWhiteSpace(oi.SupplierCompanyName))
                    .GroupBy(oi => (oi.SupplierCompanyID ?? 0, oi.SupplierCompanyName!.Trim()))
                    .Select(g => new
                    {
                        sellerCompanyId = g.Key.Item1,
                        sellerName = g.Key.Item2,
                        itemCount = g.Count()
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    orderId = order.OrderID,
                    orderNumber = order.OrderNumber,
                    grandTotal = order.GrandTotal,
                    sellerCount = distinctSellers.Count,
                    singleSellerName = distinctSellers.Count == 1 ? distinctSellers[0].sellerName : null,
                    singleSellerCompanyId = distinctSellers.Count == 1 ? distinctSellers[0].sellerCompanyId : 0,
                    sellers = distinctSellers,
                    message = $"🎉 Tebrikler! {order.OrderNumber} numaralı kurumsal siparişiniz {order.GrandTotal:N0} ₺ tutarında başarıyla onaylandı."
                });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine($"CreateOrder Error: {detail}");
                return Json(new { success = false, message = "Sipariş kaydedilirken bir hata oluştu: " + detail });
            }
        }
    }
}
