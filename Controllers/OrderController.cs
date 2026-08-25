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

        /// <summary>
        /// Doğrudan sepetteki ürünler veya hızlı satın alma için sipariş oluşturan AJAX endpoint.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderInputModel? input)
        {
            if (input == null || input.Items == null || !input.Items.Any())
            {
                return Json(new { success = false, message = "Sepetinizde ürün bulunmamaktadır." });
            }

            if (string.IsNullOrWhiteSpace(input.BuyerCompany) || string.IsNullOrWhiteSpace(input.DeliveryCity) || string.IsNullOrWhiteSpace(input.DeliveryAddress))
            {
                return Json(new { success = false, message = "Lütfen firma ünvanı, teslimat şehri ve adresini eksiksiz doldurunuz." });
            }

            try
            {
                int currentCompanyId = GetCurrentCompanyId();
                string currentCompanyName = User.FindFirst("CompanyName")?.Value ?? "";
                string currentEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? input.BuyerEmail ?? "";

                // Firma varlık doğrulaması
                if (currentCompanyId > 0)
                {
                    var comp = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId);
                    if (comp != null)
                    {
                        if (string.IsNullOrWhiteSpace(currentCompanyName)) currentCompanyName = comp.CompanyName ?? comp.CompanyOfficial ?? "";
                        if (string.IsNullOrWhiteSpace(input.BuyerTaxNumber)) input.BuyerTaxNumber = comp.TCNO;
                        if (string.IsNullOrWhiteSpace(input.BuyerTaxOffice)) input.BuyerTaxOffice = comp.TaxOffice;
                    }
                    else
                    {
                        currentCompanyId = 0;
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
                        SupplierCompanyID = supplierCompId
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

                // E-Posta Bildirimi Gönder (Async Arka Plan)
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
                    Console.WriteLine($"Order confirmation email failed: {emailEx.Message}");
                }

                return Json(new
                {
                    success = true,
                    orderId = order.OrderID,
                    orderNumber = order.OrderNumber,
                    grandTotal = order.GrandTotal,
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
