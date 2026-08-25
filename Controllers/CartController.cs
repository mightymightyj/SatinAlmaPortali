using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using satinalmaportali2.Data;
using satinalmaportali2.Models;

namespace satinalmaportali2.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
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

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Kurumsal Satınalma Sepetim";
            return View();
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
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SyncCart([FromBody] List<CartItemInputModel>? localItems)
        {
            try
            {
                int companyId = GetCurrentCompanyId();
                if (companyId <= 0)
                {
                    return Json(new { success = true, isAuthenticated = false, items = localItems ?? new List<CartItemInputModel>() });
                }

                var dbItems = await _context.CartItems
                    .Where(c => c.CompanyID == companyId)
                    .ToListAsync();

                bool changes = false;

                if (localItems != null && localItems.Any())
                {
                    foreach (var lItem in localItems)
                    {
                        if (lItem.ProductID <= 0) continue;

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
                int companyId = GetCurrentCompanyId();
                if (companyId <= 0)
                {
                    return Json(new { success = true, isAuthenticated = false, message = "Misafir oturumu." });
                }

                var existing = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.CompanyID == companyId && c.ProductID == input.ProductID);

                int addQty = input.Quantity > 0 ? input.Quantity : 1;

                if (existing != null)
                {
                    existing.Quantity += addQty;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var newItem = new CartItem
                    {
                        CompanyID = companyId,
                        ProductID = input.ProductID,
                        Title = input.Title ?? "Ürün",
                        Sku = input.Sku ?? "",
                        Category = input.Category ?? "Endüstriyel",
                        Price = input.Price,
                        Quantity = addQty,
                        Seller = input.Seller ?? "Onaylı Tedarikçi",
                        Image = input.Image ?? "",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.CartItems.Add(newItem);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, isAuthenticated = true });
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
                }

                return Json(new { success = true, isAuthenticated = true });
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
                }

                return Json(new { success = true, isAuthenticated = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
