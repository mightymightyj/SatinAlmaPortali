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

namespace satinalmaportali2.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<PortalHub> _hubContext;

        public CartController(ApplicationDbContext context, IHubContext<PortalHub> hubContext)
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

        private async Task<(int companyId, string companyName, Company? company)> GetCurrentCompanyContextAsync()
        {
            int companyId = GetCurrentCompanyId();
            string currentCompanyName = User.FindFirst("CompanyName")?.Value ?? "";
            Company? comp = null;
            if (companyId > 0)
            {
                comp = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == companyId);
                if (string.IsNullOrWhiteSpace(currentCompanyName))
                {
                    currentCompanyName = comp?.CompanyName ?? comp?.CompanyOfficial ?? "";
                }
            }
            return (companyId, currentCompanyName, comp);
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

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Kurumsal Satınalma Sepetim";
            var profile = new CheckoutProfileDto { IsAuthenticated = false };

            int companyId = GetCurrentCompanyId();
            if (companyId > 0)
            {
                var comp = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == companyId);
                if (comp != null)
                {
                    profile.IsAuthenticated = true;
                    profile.CompanyID = comp.CompanyID;
                    profile.CompanyName = comp.CompanyName ?? comp.CompanyOfficial ?? "";
                    profile.CompanyOfficial = comp.CompanyOfficial ?? "";
                    profile.TaxNumber = comp.TCNO ?? "";
                    profile.TaxOffice = comp.TaxOffice ?? "";
                    profile.Phone = comp.CompanyPhone ?? "";
                    profile.Email = comp.CompanyMail ?? "";

                    if (comp.AddressID.HasValue && comp.AddressID.Value > 0)
                    {
                        var addr = await _context.Addresses.AsNoTracking().FirstOrDefaultAsync(a => a.AddressID == comp.AddressID.Value);
                        if (addr != null)
                        {
                            profile.DeliveryAddress = addr.AddressText ?? "";
                            if (addr.DistrictID.HasValue)
                            {
                                var district = await _context.Districts.AsNoTracking().FirstOrDefaultAsync(d => d.DistrictID == addr.DistrictID.Value);
                                if (district != null)
                                {
                                    var town = district.TownID.HasValue ? await _context.Towns.AsNoTracking().FirstOrDefaultAsync(t => t.TownID == district.TownID.Value) : null;
                                    profile.DeliveryCity = town != null ? $"{town.TownName} / {district.DistrictName}" : district.DistrictName ?? "";
                                }
                            }
                        }
                    }
                }
            }

            return View(profile);
        }

        /// <summary>
        /// Kullanıcının veritabanındaki kayıtlı sepetini döner.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            try
            {
                int companyId = GetCurrentCompanyId();
                if (companyId <= 0)
                {
                    return Json(new { success = true, isAuthenticated = false, items = new List<object>() });
                }

                var dbItems = await _context.CartItems
                    .AsNoTracking()
                    .Where(c => c.CompanyID == companyId)
                    .OrderBy(c => c.CartItemID)
                    .Select(i => new
                    {
                        productId = i.ProductID,
                        title = i.Title,
                        sku = i.Sku ?? "",
                        category = i.Category ?? "",
                        price = i.Price,
                        quantity = i.Quantity,
                        seller = i.Seller ?? "Onaylı Tedarikçi",
                        image = i.Image ?? ""
                    })
                    .ToListAsync();

                return Json(new { success = true, isAuthenticated = true, items = dbItems });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Tarayıcıdaki (localStorage) sepeti sunucudaki kullanıcı sepetiyle senkronize eder.
        /// Farklı cihaz veya tarayıcılardan giriş yapıldığında ürünlerin korunmasını sağlar.
        /// Kullanıcının kendi satışa koyduğu ürünler sepete dahil edilmez.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SyncCart([FromBody] List<CartItemInputModel>? localItems)
        {
            try
            {
                var (companyId, currentCompanyName, comp) = await GetCurrentCompanyContextAsync();
                if (companyId <= 0)
                {
                    return Json(new { success = true, isAuthenticated = false, items = localItems ?? new List<CartItemInputModel>() });
                }

                // Firmanın kendi ürünleri varsa bunları localItems'tan ve veritabanı sepetinden temizle
                var productIds = localItems != null ? localItems.Select(l => l.ProductID).Distinct().ToList() : new List<int>();
                var ownDbItems = await _context.Items
                    .AsNoTracking()
                    .Where(i => productIds.Contains(i.ItemID))
                    .ToListAsync();

                var ownProductIds = ownDbItems
                    .Where(i => IsProductOwnedByCompany(i, companyId, currentCompanyName, comp))
                    .Select(i => i.ItemID)
                    .ToHashSet();

                if (localItems != null && ownProductIds.Any())
                {
                    localItems = localItems.Where(l => !ownProductIds.Contains(l.ProductID)).ToList();
                }

                var dbItems = await _context.CartItems
                    .Where(c => c.CompanyID == companyId)
                    .ToListAsync();

                // Veritabanı sepetindeki olası kendi ürünlerini sil
                var cartItemsToRemove = dbItems.Where(d => ownProductIds.Contains(d.ProductID)).ToList();
                if (cartItemsToRemove.Any())
                {
                    _context.CartItems.RemoveRange(cartItemsToRemove);
                    dbItems.RemoveAll(d => ownProductIds.Contains(d.ProductID));
                }

                bool changes = cartItemsToRemove.Any();

                if (localItems != null && localItems.Any())
                {
                    foreach (var lItem in localItems)
                    {
                        if (lItem.ProductID <= 0 || ownProductIds.Contains(lItem.ProductID)) continue;

                        var existing = dbItems.FirstOrDefault(d => d.ProductID == lItem.ProductID);
                        if (existing != null)
                        {
                            // Eğer cihazdaki miktar daha güncel ise veya eşitse koru
                            if (lItem.Quantity > existing.Quantity)
                            {
                                existing.Quantity = lItem.Quantity;
                                existing.UpdatedAt = DateTime.UtcNow;
                                changes = true;
                            }
                        }
                        else
                        {
                            var newItem = new CartItem
                            {
                                CompanyID = companyId,
                                ProductID = lItem.ProductID,
                                Title = lItem.Title ?? "Ürün",
                                Sku = lItem.Sku ?? "",
                                Category = lItem.Category ?? "Endüstriyel",
                                Price = lItem.Price,
                                Quantity = lItem.Quantity > 0 ? lItem.Quantity : 1,
                                Seller = lItem.Seller ?? "Onaylı Tedarikçi",
                                Image = lItem.Image ?? "",
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _context.CartItems.Add(newItem);
                            dbItems.Add(newItem);
                            changes = true;
                        }
                    }

                    if (changes)
                    {
                        await _context.SaveChangesAsync();
                    }
                }

                // Senkronize edilmiş son sepeti getir
                var mergedItems = await _context.CartItems
                    .AsNoTracking()
                    .Where(c => c.CompanyID == companyId)
                    .OrderBy(c => c.CartItemID)
                    .Select(i => new
                    {
                        productId = i.ProductID,
                        title = i.Title,
                        sku = i.Sku ?? "",
                        category = i.Category ?? "",
                        price = i.Price,
                        quantity = i.Quantity,
                        seller = i.Seller ?? "Onaylı Tedarikçi",
                        image = i.Image ?? ""
                    })
                    .ToListAsync();

                return Json(new { success = true, isAuthenticated = true, items = mergedItems });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Kullanıcının veritabanı sepetine ürün ekler veya miktarını artırır.
        /// Firma kendi satışa sunduğu ürünü sepete ekleyemez.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] CartItemInputModel? input)
        {
            if (input == null || input.ProductID <= 0)
            {
                return Json(new { success = false, message = "Geçersiz ürün bilgisi." });
            }

            try
            {
                var (companyId, currentCompanyName, comp) = await GetCurrentCompanyContextAsync();

                if (companyId <= 0 || User?.Identity?.IsAuthenticated != true)
                {
                    return Json(new 
                    { 
                        success = false, 
                        requireLogin = true, 
                        message = "Sepete ürün ekleyebilmek için lütfen kurumsal hesabınızla giriş yapınız veya yeni bir firma hesabı oluşturunuz." 
                    });
                }

                var dbItem = await _context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.ItemID == input.ProductID);
                if (dbItem == null || dbItem.IsDeleted)
                {
                    return Json(new { success = false, message = "Bu ürün satıştan veya yayından kaldırılmıştır." });
                }

                int availableStock = dbItem.Quantity ?? 0;
                if (availableStock <= 0)
                {
                    return Json(new { success = false, message = $"'{dbItem.ItemName ?? "Ürün"}' ürününün stoğu tükenmiştir, sepete eklenemez." });
                }

                if (IsProductOwnedByCompany(dbItem, companyId, currentCompanyName, comp))
                {
                    return Json(new { success = false, message = "Kendi satışa sunduğunuz ürünü sepete ekleyemez ve satın alamazsınız." });
                }

                var existing = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.CompanyID == companyId && c.ProductID == input.ProductID);

                int addQty = input.Quantity > 0 ? input.Quantity : 1;
                int currentInCart = existing != null ? existing.Quantity : 0;
                int totalRequested = currentInCart + addQty;

                if (totalRequested > availableStock)
                {
                    return Json(new 
                    { 
                        success = false, 
                        availableStock = availableStock,
                        message = $"Yetersiz stok! Bu üründen en fazla {availableStock} adet ekleyebilirsiniz. (Sepetinizde: {currentInCart} adet, Eklenmek istenen: {addQty} adet)" 
                    });
                }

                if (existing != null)
                {
                    existing.Quantity = totalRequested;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var newItem = new CartItem
                    {
                        CompanyID = companyId,
                        ProductID = input.ProductID,
                        Title = input.Title ?? dbItem.ItemName ?? "Ürün",
                        Sku = input.Sku ?? $"SKU-B2B-{dbItem.ItemID:D4}",
                        Category = input.Category ?? "Endüstriyel",
                        Price = dbItem.ItemPrice.HasValue ? (decimal)dbItem.ItemPrice.Value : input.Price,
                        Quantity = addQty,
                        Seller = input.Seller ?? dbItem.Brand ?? "Onaylı Tedarikçi",
                        Image = input.Image ?? "",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.CartItems.Add(newItem);
                }

                await _context.SaveChangesAsync();
                await BroadcastCartUpdatedAsync(companyId);
                return Json(new { success = true, isAuthenticated = true, availableStock = availableStock });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Sepetteki ürünün miktarını günceller. Miktar 0 veya altındaysa ürünü sepetten çıkarır.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateCartQtyInputModel? input)
        {
            if (input == null || input.ProductID <= 0)
            {
                return Json(new { success = false, message = "Geçersiz ürün bilgisi." });
            }

            try
            {
                int companyId = GetCurrentCompanyId();
                if (companyId <= 0)
                {
                    return Json(new { success = true, isAuthenticated = false });
                }

                var dbItem = await _context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.ItemID == input.ProductID);
                if (dbItem == null || dbItem.IsDeleted)
                {
                    return Json(new { success = false, message = "Bu ürün satıştan veya yayından kaldırılmıştır." });
                }
                int availableStock = dbItem?.Quantity ?? 0;

                if (input.Quantity > availableStock)
                {
                    return Json(new 
                    { 
                        success = false, 
                        availableStock = availableStock,
                        message = $"Yetersiz stok! Bu ürün için mevcut maksimum stok {availableStock} adettir." 
                    });
                }

                var item = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.CompanyID == companyId && c.ProductID == input.ProductID);

                if (item != null)
                {
                    if (input.Quantity <= 0)
                    {
                        _context.CartItems.Remove(item);
                    }
                    else
                    {
                        item.Quantity = input.Quantity;
                        item.UpdatedAt = DateTime.UtcNow;
                    }
                    await _context.SaveChangesAsync();
                    await BroadcastCartUpdatedAsync(companyId);
                }

                return Json(new { success = true, isAuthenticated = true, availableStock = availableStock });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Ürünü kullanıcının veritabanı sepetinden siler.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart([FromBody] RemoveCartItemInputModel? input)
        {
            if (input == null || input.ProductID <= 0)
            {
                return Json(new { success = false, message = "Geçersiz ürün bilgisi." });
            }

            try
            {
                int companyId = GetCurrentCompanyId();
                if (companyId <= 0)
                {
                    return Json(new { success = true, isAuthenticated = false });
                }

                var item = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.CompanyID == companyId && c.ProductID == input.ProductID);

                if (item != null)
                {
                    _context.CartItems.Remove(item);
                    await _context.SaveChangesAsync();
                    await BroadcastCartUpdatedAsync(companyId);
                }

                return Json(new { success = true, isAuthenticated = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Kullanıcının tüm sepetini temizler.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                int companyId = GetCurrentCompanyId();
                if (companyId <= 0)
                {
                    return Json(new { success = true, isAuthenticated = false });
                }

                var items = await _context.CartItems
                    .Where(c => c.CompanyID == companyId)
                    .ToListAsync();

                if (items.Any())
                {
                    _context.CartItems.RemoveRange(items);
                    await _context.SaveChangesAsync();
                    await BroadcastCartUpdatedAsync(companyId);
                }

                return Json(new { success = true, isAuthenticated = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task BroadcastCartUpdatedAsync(int companyId)
        {
            if (companyId <= 0) return;
            try
            {
                var cartItems = await _context.CartItems
                    .AsNoTracking()
                    .Where(c => c.CompanyID == companyId)
                    .ToListAsync();

                await _hubContext.Clients.Group($"company_{companyId}").SendAsync("OnCartUpdated", new
                {
                    companyId = companyId,
                    uniqueItemCount = cartItems.Count,
                    totalCount = cartItems.Sum(i => i.Quantity)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SignalR BroadcastCartUpdatedAsync error: {ex.Message}");
            }
        }
    }
}
