using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using satinalmaportali2.Data;
using satinalmaportali2.Hubs;
using satinalmaportali2.Models;
using satinalmaportali2.Services;

namespace satinalmaportali2.Controllers
{
    /// <summary>
    /// Satın alma ve talep analitiği gösterge paneli denetleyicisi.
    /// Hesap türüne göre (Alıcı, Tedarikçi, Her İkisi) erişim ve mod kontrolleri burada yönetilir.
    /// </summary>
    [Authorize]
    public class DashboardController : Controller
    {
        public const string ConnectionName = "TedarikDb";

        private readonly IDashboardService _dashboardService;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<PortalHub> _hubContext;
        private readonly EmailService? _emailService;
        private readonly ICloudinaryService _cloudinaryService;

        public DashboardController(
            IDashboardService dashboardService, 
            ApplicationDbContext context, 
            IHubContext<PortalHub> hubContext,
            ICloudinaryService cloudinaryService,
            EmailService? emailService = null)
        {
            _dashboardService = dashboardService;
            _context = context;
            _hubContext = hubContext;
            _cloudinaryService = cloudinaryService;
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

        private string GetCurrentUserType()
        {
            return User.FindFirstValue("UserType") ?? User.FindFirstValue(ClaimTypes.Role) ?? "User";
        }

        private string GetCurrentEmail()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("Email")?.Value ?? "";
        }

        private bool IsSuperAdmin()
        {
            return User.IsInRole("SuperAdmin") || User.FindFirst("IsAdmin")?.Value == "True" || GetCurrentUserType() == "SuperAdmin";
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
        /// Dashboard ana yönetim merkezi görünümü.
        /// </summary>
        public async Task<IActionResult> Index(int? yil, CancellationToken cancellationToken)
        {
            ViewData["Title"] = "Tedarik Portalı - Genel Bakış";
            int companyId = GetCurrentCompanyId();
            var viewModel = await _dashboardService.GetBuyerDashboardAsync(yil, companyId > 0 ? companyId : null, cancellationToken);
            return View("Buyer", viewModel);
        }

        /// <summary>
        /// Alıcı aksiyonu (Doğrudan ana dashboard'u açar).
        /// </summary>
        public async Task<IActionResult> Buyer(int? yil, CancellationToken cancellationToken)
        {
            return await Index(yil, cancellationToken);
        }

        /// <summary>
        /// Tedarikçi gösterge paneli görünümü.
        /// </summary>
        public async Task<IActionResult> Supplier(int? yil, CancellationToken cancellationToken)
        {
            ViewData["Title"] = "Tedarikçi Satış & RFQ Yönetimi";
            int companyId = GetCurrentCompanyId();
            var viewModel = await _dashboardService.GetSupplierDashboardAsync(yil, companyId > 0 ? companyId : null, cancellationToken);
            return View("Supplier", viewModel);
        }

        /// <summary>
        /// Katalog ürününün birim fiyatını ve stok adedini günceller (AJAX).
        /// Sadece ürünün sahibi olan tedarikçi veya admin güncelleyebilir.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateProductPrice([FromBody] UpdateProductPriceInputModel input)
        {
            if (input == null || input.ItemID <= 0 || input.NewUnitPrice <= 0)
            {
                return Json(new { success = false, message = "Geçerli bir ürün ve fiyat bilgisi giriniz." });
            }

            try
            {
                var item = await _context.Items.FirstOrDefaultAsync(i => i.ItemID == input.ItemID);
                if (item == null)
                {
                    return Json(new { success = false, message = "Güncellenecek ürün veritabanında bulunamadı." });
                }

                int companyId = GetCurrentCompanyId();
                string currentCompanyName = await GetCurrentCompanyNameAsync();
                string userType = GetCurrentUserType();
                bool isAdmin = userType == "SuperAdmin" || userType == "Admin";

                bool isOwner = !string.IsNullOrWhiteSpace(currentCompanyName) &&
                               !string.IsNullOrWhiteSpace(item.Brand) &&
                               string.Equals(item.Brand.Trim(), currentCompanyName, StringComparison.OrdinalIgnoreCase);

                if (!isOwner && !isAdmin)
                {
                    return Json(new { success = false, message = "Yalnızca kendi firmanıza ait ürünlerin fiyat ve stok bilgilerini düzenleyebilirsiniz." });
                }

                item.ItemPrice = (double)input.NewUnitPrice;
                if (input.NewStockQuantity.HasValue && input.NewStockQuantity.Value >= 0)
                {
                    item.Quantity = input.NewStockQuantity.Value;
                }

                await _context.SaveChangesAsync();

                try
                {
                    await _hubContext.Clients.All.SendAsync("OnProductPriceChanged", new
                    {
                        itemId = item.ItemID,
                        itemName = item.ItemName ?? "Ürün",
                        newUnitPrice = input.NewUnitPrice,
                        newUnitPriceFormatted = input.NewUnitPrice.ToString("N0") + " ₺",
                        newStock = item.Quantity ?? 0,
                        status = (item.Quantity ?? 0) <= 0 ? "Tükendi" : ((item.Quantity ?? 0) < 50 ? "Kritik" : "Stokta"),
                        brand = item.Brand ?? ""
                    });
                }
                catch (Exception hubEx)
                {
                    Console.WriteLine($"SignalR UpdateProductPrice error: {hubEx.Message}");
                }

                return Json(new
                {
                    success = true,
                    message = $"'{item.ItemName}' birim fiyatı {input.NewUnitPrice:N0} ₺ olarak başarıyla güncellendi.",
                    itemId = item.ItemID,
                    newPrice = input.NewUnitPrice,
                    newStock = item.Quantity
                });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Fiyat güncellenirken hata oluştu: " + detail });
            }
        }

        /// <summary>
        /// Tedarikçi paneli için Cloudinary ürün görseli yükleme endpoint'i (AJAX).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UploadProductImage(IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "Lütfen geçerli bir görsel dosyası seçiniz." });
            }

            try
            {
                var uploadResult = await _cloudinaryService.UploadImageAsync(
                    file,
                    folder: "satinalmaportali/products",
                    width: 1200,
                    cancellationToken: cancellationToken
                );

                if (!uploadResult.Success || string.IsNullOrWhiteSpace(uploadResult.SecureUrl))
                {
                    return Json(new { success = false, message = uploadResult.Error ?? "Görsel Cloudinary sunucusuna yüklenemedi." });
                }

                return Json(new
                {
                    success = true,
                    imageUrl = uploadResult.SecureUrl,
                    publicId = uploadResult.PublicId,
                    message = "Görsel başarıyla Cloudinary'ye yüklendi."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Yükleme hatası: " + ex.Message });
            }
        }

        /// <summary>
        /// Cloudinary ve veritabanından ürün görseli silme endpoint'i (AJAX).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> DeleteProductImage(string? publicIdOrUrl, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(publicIdOrUrl))
            {
                return Json(new { success = false, message = "Görsel bilgisi belirtilmedi." });
            }

            try
            {
                await _cloudinaryService.DeleteImageAsync(publicIdOrUrl, cancellationToken);

                var dbImg = await _context.ItemImages.FirstOrDefaultAsync(img => img.ImageUrl == publicIdOrUrl || img.PublicId == publicIdOrUrl, cancellationToken);
                if (dbImg != null)
                {
                    _context.ItemImages.Remove(dbImg);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                return Json(new { success = true, message = "Görsel başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Görsel silinirken hata: " + ex.Message });
            }
        }

        /// <summary>
        /// Ürünün mevcut bilgilerini ve kayıtlı tüm görsellerini döner (Düzenleme modalı için AJAX).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProductDetails(int id, CancellationToken cancellationToken)
        {
            if (id <= 0) return Json(new { success = false, message = "Geçersiz ürün." });

            try
            {
                var item = await _context.Items
                    .Include(i => i.Images)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.ItemID == id, cancellationToken);

                if (item == null)
                {
                    return Json(new { success = false, message = "Ürün bulunamadı." });
                }

                var imageDtos = new List<ProductImageDto>();

                // 1. Veritabanındaki ItemImages tablosu kayıtları
                if (item.Images != null && item.Images.Any())
                {
                    var sorted = item.Images.OrderByDescending(x => x.IsMain).ThenBy(x => x.DisplayOrder).ToList();
                    foreach (var img in sorted)
                    {
                        if (!string.IsNullOrWhiteSpace(img.ImageUrl))
                        {
                            imageDtos.Add(new ProductImageDto
                            {
                                ImageID = img.ImageID,
                                ImageUrl = img.ImageUrl.Trim(),
                                PublicId = img.PublicId,
                                IsMain = img.IsMain,
                                DisplayOrder = img.DisplayOrder
                            });
                        }
                    }
                }

                // 2. Properties alanından görselleri çıkar (Geriye dönük uyumluluk ve legacy ürünler)
                if (!string.IsNullOrWhiteSpace(item.Properties))
                {
                    var matches = System.Text.RegularExpressions.Regex.Matches(
                        item.Properties,
                        @"(?:Resim|Image|ImageUrl|Gorsel|Görsel)\s*:\s*(https?://[^\s\|;,]+|/[^\s\|;,]+)",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    foreach (System.Text.RegularExpressions.Match m in matches)
                    {
                        if (m.Success)
                        {
                            string foundUrl = m.Groups[1].Value.Trim();
                            if (!string.IsNullOrWhiteSpace(foundUrl) && 
                                foundUrl != "/images/default-product.svg" && 
                                !imageDtos.Any(x => string.Equals(x.ImageUrl, foundUrl, StringComparison.OrdinalIgnoreCase)))
                            {
                                imageDtos.Add(new ProductImageDto
                                {
                                    ImageID = 0,
                                    ImageUrl = foundUrl,
                                    PublicId = null,
                                    IsMain = !imageDtos.Any(),
                                    DisplayOrder = imageDtos.Count
                                });
                            }
                        }
                    }

                    // Eğer hala hiç görsel bulunamadıysa, Properties içindeki doğrudan Cloudinary veya resim dosya URL'lerini ara
                    if (!imageDtos.Any())
                    {
                        var directMatches = System.Text.RegularExpressions.Regex.Matches(
                            item.Properties,
                            @"(https?://[^\s\|;,]+(?:cloudinary|[^\s\|;,]+\.(?:png|jpg|jpeg|webp|gif|svg)))",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                        foreach (System.Text.RegularExpressions.Match dm in directMatches)
                        {
                            if (dm.Success)
                            {
                                string directUrl = dm.Groups[1].Value.Trim();
                                if (!string.IsNullOrWhiteSpace(directUrl) && 
                                    directUrl != "/images/default-product.svg" && 
                                    !imageDtos.Any(x => string.Equals(x.ImageUrl, directUrl, StringComparison.OrdinalIgnoreCase)))
                                {
                                    imageDtos.Add(new ProductImageDto
                                    {
                                        ImageID = 0,
                                        ImageUrl = directUrl,
                                        PublicId = null,
                                        IsMain = !imageDtos.Any(),
                                        DisplayOrder = imageDtos.Count
                                    });
                                }
                            }
                        }
                    }
                }

                if (imageDtos.Any() && !imageDtos.Any(x => x.IsMain))
                {
                    imageDtos[0].IsMain = true;
                }

                string mainImageUrl = imageDtos.FirstOrDefault(x => x.IsMain)?.ImageUrl 
                    ?? imageDtos.FirstOrDefault()?.ImageUrl 
                    ?? "/images/default-product.svg";

                return Json(new
                {
                    success = true,
                    product = new
                    {
                        itemId = item.ItemID,
                        itemName = item.ItemName ?? "",
                        productName = item.ItemName ?? "",
                        unitPrice = item.ItemPrice ?? 0,
                        price = item.ItemPrice ?? 0,
                        stockQuantity = item.Quantity ?? 0,
                        stock = item.Quantity ?? 0,
                        categoryId = item.CategoryID,
                        description = item.ItemDescryption ?? "",
                        itemDescription = item.ItemDescryption ?? "",
                        properties = item.Properties ?? "",
                        brand = item.Brand ?? "",
                        mainImageUrl = mainImageUrl,
                        images = imageDtos
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetProductDetails error: {ex}");
                return Json(new
                {
                    success = false,
                    message = "Ürün detayları alınırken bir hata oluştu: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Satıcının ürün bilgilerini ve görsellerini tam olarak güncelleyen endpoint (AJAX).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateProductListing([FromForm] UpdateProductListingInputModel input, CancellationToken cancellationToken)
        {
            if (input == null || input.ItemID <= 0 || string.IsNullOrWhiteSpace(input.ProductName) || input.UnitPrice <= 0)
            {
                return Json(new { success = false, message = "Lütfen geçerli ürün adı ve birim fiyat giriniz." });
            }

            try
            {
                var item = await _context.Items
                    .Include(i => i.Images)
                    .FirstOrDefaultAsync(i => i.ItemID == input.ItemID, cancellationToken);

                if (item == null)
                {
                    return Json(new { success = false, message = "Güncellenecek ürün veritabanında bulunamadı." });
                }

                int companyId = GetCurrentCompanyId();
                string currentCompanyName = await GetCurrentCompanyNameAsync();
                string userType = GetCurrentUserType();
                bool isAdmin = userType == "SuperAdmin" || userType == "Admin";

                bool isOwner = !string.IsNullOrWhiteSpace(currentCompanyName) &&
                               !string.IsNullOrWhiteSpace(item.Brand) &&
                               string.Equals(item.Brand.Trim(), currentCompanyName, StringComparison.OrdinalIgnoreCase);

                if (!isOwner && !isAdmin)
                {
                    return Json(new { success = false, message = "Yalnızca kendi firmanıza ait ürünleri düzenleyebilirsiniz." });
                }

                item.ItemName = input.ProductName.Trim();
                item.ItemPrice = (double)input.UnitPrice;
                if (input.StockQuantity.HasValue && input.StockQuantity.Value >= 0)
                {
                    item.Quantity = input.StockQuantity.Value;
                }
                if (input.CategoryID.HasValue && input.CategoryID.Value > 0)
                {
                    item.CategoryID = input.CategoryID.Value;
                }
                if (input.ItemDescription != null)
                {
                    item.ItemDescryption = input.ItemDescription.Trim();
                }

                // Görselleri senkronize et
                var allImageUrls = new List<string>();
                string mainImageUrl = "/images/default-product.svg";

                // 1. Mevcut korunan görselleri çözümle
                List<ProductImageDto> keptExistingList = new();
                if (!string.IsNullOrWhiteSpace(input.ExistingImagesJson))
                {
                    try
                    {
                        keptExistingList = JsonSerializer.Deserialize<List<ProductImageDto>>(input.ExistingImagesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ExistingImagesJson parse error: {ex.Message}");
                    }
                }

                if (!string.IsNullOrWhiteSpace(input.ExistingImagesJson) || (input.NewImageFiles != null && input.NewImageFiles.Any()))
                {
                    // Satıcının sildiği mevcut veritabanı görsellerini kaldır
                    var dbImages = item.Images.ToList();
                    foreach (var dbImg in dbImages)
                    {
                        var kept = keptExistingList.FirstOrDefault(k => k.ImageID == dbImg.ImageID);
                        if (kept != null)
                        {
                            dbImg.IsMain = kept.IsMain && !input.MainImageIsNew;
                            dbImg.DisplayOrder = kept.DisplayOrder;
                            if (dbImg.IsMain) mainImageUrl = dbImg.ImageUrl;
                            allImageUrls.Add(dbImg.ImageUrl);
                        }
                        else
                        {
                            _context.ItemImages.Remove(dbImg);
                        }
                    }

                    // KRİTİK: Eğer keptExistingList içinde ImageID == 0 olan (Properties veya vitrinden gelen eski görseller) varsa,
                    // onları artık kalıcı olarak ItemImages tablosuna ekle!
                    foreach (var keptLegacy in keptExistingList.Where(k => (k.ImageID == 0 || !dbImages.Any(d => d.ImageID == k.ImageID)) && !string.IsNullOrWhiteSpace(k.ImageUrl) && k.ImageUrl != "/images/default-product.svg"))
                    {
                        bool isMain = keptLegacy.IsMain && !input.MainImageIsNew;
                        if (isMain) mainImageUrl = keptLegacy.ImageUrl;

                        var newImg = new ItemImage
                        {
                            ItemID = item.ItemID,
                            ImageUrl = keptLegacy.ImageUrl.Trim(),
                            PublicId = keptLegacy.PublicId,
                            IsMain = isMain,
                            DisplayOrder = keptLegacy.DisplayOrder,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.ItemImages.Add(newImg);
                        allImageUrls.Add(keptLegacy.ImageUrl.Trim());
                    }

                    // Yeni eklenen dosyaları sadece ve sadece şimdi Cloudinary'ye yükle
                    if (input.NewImageFiles != null && input.NewImageFiles.Any())
                    {
                        var filesToProcess = input.NewImageFiles.Take(10).ToList();
                        for (int i = 0; i < filesToProcess.Count; i++)
                        {
                            var file = filesToProcess[i];
                            if (file.Length == 0 || file.Length > 10 * 1024 * 1024) continue;

                            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
                            var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                            if (!allowedExts.Contains(ext)) continue;

                            try
                            {
                                var uploadResult = await _cloudinaryService.UploadImageAsync(
                                    file,
                                    folder: "satinalmaportali/products",
                                    width: 1200,
                                    cancellationToken: cancellationToken
                                );

                                if (uploadResult.Success && !string.IsNullOrWhiteSpace(uploadResult.SecureUrl))
                                {
                                    bool isMain = (input.MainImageIsNew && i == input.MainImageIndex);
                                    if (isMain)
                                    {
                                        mainImageUrl = uploadResult.SecureUrl;
                                        // Mevcut görsellerin isMain'ini false yap
                                        foreach (var dbImg in item.Images) dbImg.IsMain = false;
                                    }

                                    var newImg = new ItemImage
                                    {
                                        ItemID = item.ItemID,
                                        ImageUrl = uploadResult.SecureUrl,
                                        PublicId = uploadResult.PublicId,
                                        IsMain = isMain,
                                        DisplayOrder = 100 + i,
                                        CreatedAt = DateTime.UtcNow
                                    };
                                    _context.ItemImages.Add(newImg);
                                    allImageUrls.Add(uploadResult.SecureUrl);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Cloudinary upload error in UpdateProductListing: {ex.Message}");
                            }
                        }
                    }

                    // Eğer görseller varsa ve hiçbiri ana görsel seçilmediyse ilkini ana görsel yap
                    var allActiveImages = item.Images.ToList();
                    if (allActiveImages.Any() && !allActiveImages.Any(x => x.IsMain))
                    {
                        allActiveImages[0].IsMain = true;
                        mainImageUrl = allActiveImages[0].ImageUrl;
                    }
                }
                else if (input.Images != null)
                {
                    var existingDbImages = item.Images.ToList();
                    _context.ItemImages.RemoveRange(existingDbImages);

                    bool hasMainFlag = input.Images.Any(img => img.IsMain);
                    for (int i = 0; i < input.Images.Count; i++)
                    {
                        var imgInput = input.Images[i];
                        bool isMain = hasMainFlag ? imgInput.IsMain : (i == 0);
                        if (isMain) mainImageUrl = imgInput.ImageUrl;

                        var newImg = new ItemImage
                        {
                            ItemID = item.ItemID,
                            ImageUrl = imgInput.ImageUrl,
                            PublicId = imgInput.PublicId,
                            IsMain = isMain,
                            DisplayOrder = imgInput.DisplayOrder > 0 ? imgInput.DisplayOrder : i,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.ItemImages.Add(newImg);
                        allImageUrls.Add(imgInput.ImageUrl);
                    }
                }

                // Properties alanını da geriye dönük uyumluluk için güncelle
                if (mainImageUrl != "/images/default-product.svg")
                {
                    var prop = item.Properties ?? "";
                    if (System.Text.RegularExpressions.Regex.IsMatch(prop, @"(?:Resim|Image|ImageUrl|Gorsel)\s*:\s*(https?://[^\s\|;,]+|/[^\s\|;,]+)"))
                    {
                        prop = System.Text.RegularExpressions.Regex.Replace(prop, @"(?:Resim|Image|ImageUrl|Gorsel)\s*:\s*(https?://[^\s\|;,]+|/[^\s\|;,]+)", $"Resim: {mainImageUrl}");
                    }
                    else
                    {
                        prop = string.IsNullOrWhiteSpace(prop) ? $"Resim: {mainImageUrl}" : $"{prop} | Resim: {mainImageUrl}";
                    }
                    item.Properties = prop;
                }

                await _context.SaveChangesAsync(cancellationToken);

                // SignalR canlı duyuru
                try
                {
                    await _hubContext.Clients.All.SendAsync("OnProductPriceChanged", new
                    {
                        itemId = item.ItemID,
                        itemName = item.ItemName ?? "Ürün",
                        newUnitPrice = input.UnitPrice,
                        newUnitPriceFormatted = input.UnitPrice.ToString("N0") + " ₺",
                        newStock = item.Quantity ?? 0,
                        status = (item.Quantity ?? 0) <= 0 ? "Tükendi" : ((item.Quantity ?? 0) < 50 ? "Kritik" : "Stokta"),
                        brand = item.Brand ?? "",
                        image = mainImageUrl,
                        images = allImageUrls
                    }, cancellationToken);
                }
                catch (Exception hubEx)
                {
                    Console.WriteLine($"SignalR UpdateProductListing error: {hubEx.Message}");
                }

                return Json(new
                {
                    success = true,
                    message = $"'{item.ItemName}' ürün bilgileri ve görselleri başarıyla güncellendi.",
                    itemId = item.ItemID,
                    itemName = item.ItemName,
                    newPrice = input.UnitPrice,
                    newStock = item.Quantity,
                    mainImageUrl = mainImageUrl,
                    images = allImageUrls
                });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Ürün güncellenirken hata oluştu: " + detail });
            }
        }

        /// <summary>
        /// Sistem genel analitik görünümü.
        /// </summary>
        public async Task<IActionResult> Analytics(int? yil, CancellationToken cancellationToken)
        {
            ViewData["Title"] = "Satın Alma ve Talep Analitiği";
            var viewModel = await _dashboardService.GetDashboardAsync(yil, cancellationToken);
            return View("Analytics", viewModel);
        }

        /// <summary>
        /// Tedarikçi ürün ilanları ve vitrin kataloğu yönetim sekmesi.
        /// Normal kullanıcılar yalnızca kendi firmalarına ait satış ilanlarını görebilir ve yönetebilir.
        /// </summary>
        public async Task<IActionResult> Listings(string? search, string? category, string? status, CancellationToken cancellationToken)
        {
            ViewData["Title"] = "Ürün İlanlarım & Vitrin Kataloğu";
            if (string.IsNullOrWhiteSpace(status))
            {
                status = "Tümü";
            }
            if (string.IsNullOrWhiteSpace(category))
            {
                category = "Tümü";
            }
            int companyId = GetCurrentCompanyId();
            string currentCompanyName = await GetCurrentCompanyNameAsync();
            string userType = GetCurrentUserType();
            bool isAdmin = userType == "SuperAdmin" || userType == "Admin";

            // Veritabanındaki ürünleri sorgula
            var query = _context.Items
                .Include(i => i.Images)
                .AsNoTracking()
                .AsQueryable();

            if (!isAdmin)
            {
                if (!string.IsNullOrWhiteSpace(currentCompanyName))
                {
                    query = query.Where(i => i.Brand == currentCompanyName);
                }
                else
                {
                    query = query.Where(i => false);
                }
            }

            var dbItems = await query.OrderByDescending(i => i.ItemID).ToListAsync(cancellationToken);

            // Kategori eşleştirmeleri
            var catIds = dbItems.Where(i => i.CategoryID.HasValue).Select(i => i.CategoryID!.Value).Distinct().ToList();
            var categoriesMap = await _context.Category
                .AsNoTracking()
                .Where(c => catIds.Contains(c.CategoryID))
                .ToDictionaryAsync(c => c.CategoryID, c => c.CategoryName ?? "Genel", cancellationToken);

            var allCategoryNames = await _context.Category
                .AsNoTracking()
                .Where(c => !string.IsNullOrEmpty(c.CategoryName))
                .Select(c => c.CategoryName!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(cancellationToken);

            var listingDtos = dbItems.Select(item =>
            {
                string catName = "Endüstriyel Ürünler";
                if (item.CategoryID.HasValue && categoriesMap.TryGetValue(item.CategoryID.Value, out var cName))
                {
                    catName = cName;
                }
                else if (!string.IsNullOrWhiteSpace(item.Properties) && item.Properties.StartsWith("Kategori: "))
                {
                    catName = item.Properties.Substring("Kategori: ".Length);
                }

                int stock = item.Quantity ?? 0;
                string itemStatus = stock <= 0 ? "Tükendi" : (stock < 50 ? "Kritik" : "Stokta");

                var imagesList = item.Images != null && item.Images.Any()
                    ? item.Images.OrderByDescending(x => x.IsMain).ThenBy(x => x.DisplayOrder).Select(x => new ProductImageDto
                    {
                        ImageID = x.ImageID,
                        ImageUrl = x.ImageUrl,
                        PublicId = x.PublicId,
                        IsMain = x.IsMain,
                        DisplayOrder = x.DisplayOrder
                    }).ToList()
                    : new List<ProductImageDto>();

                string? mainImg = imagesList.FirstOrDefault(x => x.IsMain)?.ImageUrl ?? imagesList.FirstOrDefault()?.ImageUrl;
                if (string.IsNullOrEmpty(mainImg) && !string.IsNullOrWhiteSpace(item.Properties))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(item.Properties, @"(?:Resim|Image|ImageUrl|Gorsel|Görsel)\s*:\s*(https?://[^\s\|;,]+|/[^\s\|;,]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        mainImg = m.Groups[1].Value.Trim();
                    }
                    else
                    {
                        var directM = System.Text.RegularExpressions.Regex.Match(item.Properties, @"(https?://[^\s\|;,]+(?:cloudinary|[^\s\|;,]+\.(?:png|jpg|jpeg|webp|gif|svg)))", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (directM.Success) mainImg = directM.Groups[1].Value.Trim();
                    }
                }

                if (!imagesList.Any() && !string.IsNullOrEmpty(mainImg) && mainImg != "/images/default-product.svg")
                {
                    imagesList.Add(new ProductImageDto
                    {
                        ImageID = 0,
                        ImageUrl = mainImg,
                        PublicId = null,
                        IsMain = true,
                        DisplayOrder = 0
                    });
                }

                return new SupplierListingDetailDto
                {
                    ItemId = item.ItemID,
                    ItemName = item.ItemName ?? "İsimsiz Ürün",
                    Sku = $"SKU-B2B-{item.ItemID:D4}",
                    CategoryName = catName,
                    CategoryId = item.CategoryID,
                    Stock = stock,
                    UnitPrice = (decimal)(item.ItemPrice ?? 0),
                    Brand = item.Brand ?? (!string.IsNullOrWhiteSpace(currentCompanyName) ? currentCompanyName : "Onaylı Tedarikçi"),
                    Description = item.ItemDescryption,
                    Properties = item.Properties,
                    Status = itemStatus,
                    IsLiveOnVitrin = true,
                    MainImageUrl = mainImg,
                    Images = imagesList
                };
            }).ToList();

            Company? currentComp = null;
            if (companyId > 0)
            {
                currentComp = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == companyId, cancellationToken);
            }

            var vm = new SupplierListingsViewModel
            {
                Listings = listingDtos,
                TotalCount = listingDtos.Count,
                TotalCatalogValue = listingDtos.Sum(x => x.UnitPrice * x.Stock),
                ActiveCount = listingDtos.Count(x => x.Status == "Stokta"),
                LowStockCount = listingDtos.Count(x => x.Status == "Kritik" || x.Status == "Tükendi"),
                AvailableCategories = allCategoryNames,
                SelectedCategory = category,
                SearchQuery = search,
                SelectedStatus = status,
                CompanyName = !string.IsNullOrWhiteSpace(currentCompanyName) ? currentCompanyName : "Kurumsal Tedarikçi",
                IsIyzicoApproved = currentComp != null && (currentComp.IsIyzicoApproved || !string.IsNullOrWhiteSpace(currentComp.IyzicoSubMerchantKey)),
                IyzicoSubMerchantKey = currentComp?.IyzicoSubMerchantKey
            };

            var allCats = await _context.Category.AsNoTracking().ToListAsync(cancellationToken);
            ViewBag.CategoryTree = BuildCategoryHierarchy(allCats);

            return View(vm);
        }

        /// <summary>
        /// Ürün ilanını veritabanından siler (AJAX).
        /// Sadece ürünün sahibi olan tedarikçi veya admin silebilir.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> DeleteProductListing(int id)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "Geçersiz ürün ID." });
            }

            try
            {
                var item = await _context.Items.FirstOrDefaultAsync(i => i.ItemID == id);
                if (item == null)
                {
                    return Json(new { success = false, message = "Silinecek ürün veritabanında bulunamadı." });
                }

                int companyId = GetCurrentCompanyId();
                string currentCompanyName = await GetCurrentCompanyNameAsync();
                string userType = GetCurrentUserType();
                bool isAdmin = userType == "SuperAdmin" || userType == "Admin";

                bool isOwner = !string.IsNullOrWhiteSpace(currentCompanyName) &&
                               !string.IsNullOrWhiteSpace(item.Brand) &&
                               string.Equals(item.Brand.Trim(), currentCompanyName, StringComparison.OrdinalIgnoreCase);

                if (!isOwner && !isAdmin)
                {
                    return Json(new { success = false, message = "Yalnızca kendi firmanıza ait ürün ilanlarını silebilirsiniz." });
                }

                _context.Items.Remove(item);
                await _context.SaveChangesAsync();

                try
                {
                    await _hubContext.Clients.All.SendAsync("OnProductDeleted", new { itemId = id });
                }
                catch (Exception hubEx)
                {
                    Console.WriteLine($"SignalR DeleteProductListing error: {hubEx.Message}");
                }

                return Json(new { success = true, message = $"\"{item.ItemName}\" ilanı katalogdan ve vitrinden kaldırıldı." });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "İlan silinirken hata oluştu: " + detail });
            }
        }

        private List<HierarchicalCategoryItem> BuildCategoryHierarchy(List<Category> allCategories)
        {
            var categoryMap = allCategories.ToDictionary(c => c.CategoryID, c => new HierarchicalCategoryItem
            {
                CategoryID = c.CategoryID,
                CategoryName = c.CategoryName ?? string.Empty,
                ParentCategoryID = c.ParentCategoryID,
                Children = new List<HierarchicalCategoryItem>()
            });

            var rootItems = new List<HierarchicalCategoryItem>();

            foreach (var cat in allCategories)
            {
                var item = categoryMap[cat.CategoryID];
                if (cat.ParentCategoryID.HasValue && categoryMap.TryGetValue(cat.ParentCategoryID.Value, out var parentItem))
                {
                    item.ParentCategoryName = parentItem.CategoryName;
                    parentItem.Children.Add(item);
                    parentItem.HasChildren = true;
                }
                else
                {
                    rootItems.Add(item);
                }
            }

            void ComputePaths(HierarchicalCategoryItem current, string currentPath, int level)
            {
                current.Level = level;
                current.BreadcrumbPath = string.IsNullOrEmpty(currentPath) ? current.CategoryName : $"{currentPath} > {current.CategoryName}";
                current.FormattedName = $"{new string('-', level * 2)} {current.CategoryName}".TrimStart();

                foreach (var child in current.Children)
                {
                    ComputePaths(child, current.BreadcrumbPath, level + 1);
                }
            }

            foreach (var root in rootItems)
            {
                ComputePaths(root, string.Empty, 0);
            }

            return rootItems;
        }

        /// <summary>
        /// Alıcı firmalar için verilen siparişler takip sekmesi.
        /// </summary>
        public async Task<IActionResult> Orders(string? search, string? status, CancellationToken cancellationToken)
        {
            ViewData["Title"] = "Verilen Siparişlerim & Sevkiyat Takibi";
            int companyId = GetCurrentCompanyId();
            string currentCompanyName = User.FindFirst("CompanyName")?.Value ?? "";

            var query = _context.Orders
                .Include(o => o.OrderItems)
                .AsNoTracking()
                .AsQueryable();

            // Yalnızca 3D Secure / ödemesi tamamlanmış veya geçerli kurumsal siparişleri listele (3D onayı bekleyen veya başarısız olan taslaklar görünmez)
            query = query.Where(o => 
                o.OrderStatus != "Ödeme Bekleniyor" && 
                o.OrderStatus != "Ödeme Başlatılamadı" && 
                o.OrderStatus != "Taslak" &&
                o.PaymentStatus != "Beklemede (3D)" &&
                o.PaymentStatus != "Hata");

            if (companyId > 0)
            {
                var userOrdersQuery = query.Where(o => o.BuyerCompanyID == companyId);
                if (await userOrdersQuery.AnyAsync(cancellationToken))
                {
                    query = userOrdersQuery;
                }
            }

            var allOrders = await query.OrderByDescending(o => o.OrderID).ToListAsync(cancellationToken);

            var allCompanies = await _context.Companies
                .AsNoTracking()
                .Select(c => new { c.CompanyID, c.CompanyName, c.CompanyOfficial })
                .ToListAsync(cancellationToken);

            foreach (var o in allOrders)
            {
                if (!o.SupplierCompanyID.HasValue && !string.IsNullOrWhiteSpace(o.SupplierCompanyName))
                {
                    var matched = allCompanies.FirstOrDefault(c => 
                        (!string.IsNullOrWhiteSpace(c.CompanyName) && string.Equals(c.CompanyName.Trim(), o.SupplierCompanyName.Trim(), StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(c.CompanyOfficial) && string.Equals(c.CompanyOfficial.Trim(), o.SupplierCompanyName.Trim(), StringComparison.OrdinalIgnoreCase)));
                    if (matched != null)
                    {
                        o.SupplierCompanyID = matched.CompanyID;
                    }
                    else if (o.OrderItems.Any(oi => oi.SupplierCompanyID.HasValue))
                    {
                        o.SupplierCompanyID = o.OrderItems.First(oi => oi.SupplierCompanyID.HasValue).SupplierCompanyID;
                    }
                }
            }

            var filtered = allOrders.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                filtered = filtered.Where(o => o.OrderNumber.ToLowerInvariant().Contains(s) || 
                                               (o.SupplierCompanyName != null && o.SupplierCompanyName.ToLowerInvariant().Contains(s)) ||
                                               (o.TrackingNumber != null && o.TrackingNumber.ToLowerInvariant().Contains(s)) ||
                                               o.OrderItems.Any(oi => oi.ProductName.ToLowerInvariant().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "Tümü")
            {
                filtered = filtered.Where(o => o.OrderStatus.Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            var result = filtered.ToList();

            var vm = new BuyerOrdersViewModel
            {
                Orders = result,
                TotalOrderCount = allOrders.Count,
                TotalSpent = allOrders.Sum(o => o.GrandTotal),
                ActiveShipmentCount = allOrders.Count(o => o.OrderStatus == "Kargoya Verildi" || o.OrderStatus == "Hazırlanıyor"),
                CompletedCount = allOrders.Count(o => o.OrderStatus == "Teslim Edildi" || o.OrderStatus == "Tamamlandı"),
                SearchQuery = search,
                SelectedStatus = status,
                CompanyName = !string.IsNullOrWhiteSpace(currentCompanyName) ? currentCompanyName : "Kurumsal Alıcı"
            };

            return View("Orders", vm);
        }

        /// <summary>
        /// Tedarikçi firmalar için gelen kurumsal siparişler ve sevkiyat yönetim sekmesi.
        /// </summary>
        public async Task<IActionResult> SupplierOrders(string? search, string? status, CancellationToken cancellationToken)
        {
            ViewData["Title"] = "Gelen Siparişler & Sevkiyat Yönetimi";
            int companyId = GetCurrentCompanyId();
            string currentCompanyName = User.FindFirst("CompanyName")?.Value ?? "";

            var query = _context.Orders
                .Include(o => o.OrderItems)
                .AsNoTracking()
                .AsQueryable();

            // Tedarikçi yalnızca 3D Secure banka onayını tamamlamış veya geçerli kurumsal siparişleri görür!
            // Ödeme aşamasında yarım bırakılmış veya başarısız olan sipariş denemeleri satıcıya ASLA gösterilmez!
            query = query.Where(o => 
                o.OrderStatus != "Ödeme Bekleniyor" && 
                o.OrderStatus != "Ödeme Başlatılamadı" && 
                o.OrderStatus != "Taslak" &&
                o.PaymentStatus != "Beklemede (3D)" &&
                o.PaymentStatus != "Hata");

            if (companyId > 0)
            {
                var comp = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == companyId, cancellationToken);
                string compName = comp?.CompanyName ?? currentCompanyName;
                string compOfficial = comp?.CompanyOfficial ?? "";

                query = query.Where(o => 
                    (o.SupplierCompanyID.HasValue && o.SupplierCompanyID.Value == companyId) ||
                    (!string.IsNullOrWhiteSpace(compName) && o.SupplierCompanyName != null && o.SupplierCompanyName.ToLower() == compName.ToLower()) ||
                    (!string.IsNullOrWhiteSpace(compOfficial) && o.SupplierCompanyName != null && o.SupplierCompanyName.ToLower() == compOfficial.ToLower()) ||
                    (o.OrderItems.Any(oi => (oi.SupplierCompanyID.HasValue && oi.SupplierCompanyID.Value == companyId) ||
                                            (!string.IsNullOrWhiteSpace(compName) && oi.SupplierCompanyName != null && oi.SupplierCompanyName.ToLower() == compName.ToLower()) ||
                                            (!string.IsNullOrWhiteSpace(compOfficial) && oi.SupplierCompanyName != null && oi.SupplierCompanyName.ToLower() == compOfficial.ToLower())))
                );
            }

            var allOrders = await query.OrderByDescending(o => o.OrderID).ToListAsync(cancellationToken);

            var filtered = allOrders.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                filtered = filtered.Where(o => o.OrderNumber.ToLowerInvariant().Contains(s) || 
                                               o.BuyerCompanyName.ToLowerInvariant().Contains(s) ||
                                               (o.TrackingNumber != null && o.TrackingNumber.ToLowerInvariant().Contains(s)) ||
                                               o.OrderItems.Any(oi => oi.ProductName.ToLowerInvariant().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "Tümü")
            {
                filtered = filtered.Where(o => o.OrderStatus.Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            var result = filtered.ToList();

            var vm = new SupplierOrdersViewModel
            {
                Orders = result,
                TotalOrderCount = allOrders.Count,
                TotalRevenue = allOrders.Sum(o => o.GrandTotal),
                PendingShipmentCount = allOrders.Count(o => o.OrderStatus == "Onaylandı" || o.OrderStatus == "Hazırlanıyor"),
                CompletedCount = allOrders.Count(o => o.OrderStatus == "Teslim Edildi" || o.OrderStatus == "Tamamlandı"),
                SearchQuery = search,
                SelectedStatus = status,
                CompanyName = !string.IsNullOrWhiteSpace(currentCompanyName) ? currentCompanyName : "Kurumsal Tedarikçi"
            };

            return View("SupplierOrders", vm);
        }

        /// <summary>
        /// Sipariş durumu metninin hiyerarşik kademe seviyesini döndürür.
        /// Seviyeler: 1: Onaylandı, 2: Hazırlanıyor, 3: Kargoya Verildi, 4: Teslim Edildi, 5: Tamamlandı, 99: İptal Edildi.
        /// </summary>
        private static int GetOrderStatusLevel(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return 0;
            var s = status.Trim().ToLowerInvariant();
            if (s.Contains("onaylan") || s == "ödendi" || s == "yeni sipariş") return 1;
            if (s.Contains("hazırlan") || s.Contains("hazirlan")) return 2;
            if (s.Contains("kargo") || s.Contains("sevk")) return 3;
            if (s.Contains("teslim")) return 4;
            if (s.Contains("tamamlan")) return 5;
            if (s.Contains("iptal")) return 99;
            return 0;
        }

        /// <summary>
        /// Sipariş durumunu ve kargo takip bilgisini kademeli olarak günceller veya gerekçeli iptal eder (AJAX).
        /// Durumlar geriye alınamaz; yalnızca kademeli ilerletilebilir veya olumsuz durumda gerekçeli iptal edilebilir.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusInputModel input)
        {
            if (input == null || input.OrderID <= 0)
            {
                return Json(new { success = false, message = "Geçersiz sipariş ID." });
            }

            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderID == input.OrderID);

                if (order == null)
                {
                    return Json(new { success = false, message = "Sipariş veritabanında bulunamadı." });
                }

                int currentCompanyId = GetCurrentCompanyId();
                string currentCompanyName = await GetCurrentCompanyNameAsync();

                // Yetki kontrolü: Kullanıcı sisteme giriş yapmış bir tedarikçi ise siparişin satıcısı olmalıdır
                bool isAuthorized = (currentCompanyId <= 0) ||
                                    (order.SupplierCompanyID == currentCompanyId) ||
                                    (!string.IsNullOrWhiteSpace(currentCompanyName) && !string.IsNullOrWhiteSpace(order.SupplierCompanyName) && string.Equals(order.SupplierCompanyName.Trim(), currentCompanyName.Trim(), StringComparison.OrdinalIgnoreCase)) ||
                                    (order.OrderItems.Any(oi => oi.SupplierCompanyID == currentCompanyId || (!string.IsNullOrWhiteSpace(currentCompanyName) && oi.SupplierCompanyName != null && string.Equals(oi.SupplierCompanyName.Trim(), currentCompanyName.Trim(), StringComparison.OrdinalIgnoreCase))));

                if (!isAuthorized)
                {
                    return Json(new { success = false, message = "Bu siparişin durumunu güncelleme yetkiniz bulunmamaktadır." });
                }

                string currentStatus = !string.IsNullOrWhiteSpace(order.OrderStatus) ? order.OrderStatus.Trim() : "Onaylandı";
                int currentLevel = GetOrderStatusLevel(currentStatus);

                // 1. NİHAİ DURUM KONTROLÜ (Teslim Edildi, Tamamlandı veya İptal Edildi ise durum kilitlidir)
                if (currentLevel >= 4)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Bu sipariş '{currentStatus}' aşamasındadır (Nihai Durum). Sipariş tamamlandığı veya iptal edildiği için durumu artık değiştirilemez."
                    });
                }

                string requestedStatus = !string.IsNullOrWhiteSpace(input.OrderStatus) ? input.OrderStatus.Trim() : "";
                bool isCancelRequest = requestedStatus.Equals("İptal", StringComparison.OrdinalIgnoreCase) ||
                                       requestedStatus.Equals("İptal Edildi", StringComparison.OrdinalIgnoreCase) ||
                                       requestedStatus.ToLowerInvariant().Contains("iptal");

                // 2. SATICI İPTAL MEKANİZMASI (Olumsuz durumlarda gerekçeli iptal - Yalnızca Onaylandı veya Hazırlanıyor iken)
                if (isCancelRequest)
                {
                    // Kargoya verilmiş veya daha ileri durumdaysa ASLA İPTAL EDİLEMEZ!
                    if (currentLevel >= 3)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Bu sipariş '{currentStatus}' aşamasındadır. Kargonuz yola çıktığı için sipariş artık iptal edilemez! Teslimat sürecinin tamamlanması gerekmektedir."
                        });
                    }

                    string reason = !string.IsNullOrWhiteSpace(input.CancellationReason)
                        ? input.CancellationReason.Trim()
                        : (!string.IsNullOrWhiteSpace(input.Notes) ? input.Notes.Trim() : "");

                    if (string.IsNullOrWhiteSpace(reason))
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Siparişi iptal edebilmek için lütfen alıcı firmaya iletilecek geçerli bir iptal gerekçesi belirtiniz."
                        });
                    }

                    var cancelTimestamp = DateTime.UtcNow.AddHours(3).ToString("dd.MM.yyyy HH:mm");
                    string cancelLog = $"[Satıcı İptal Gerekçesi ({cancelTimestamp})]: {reason}";

                    order.OrderStatus = "İptal Edildi";
                    order.Notes = string.IsNullOrWhiteSpace(order.Notes) ? cancelLog : $"{order.Notes}\n{cancelLog}";
                    order.DisputeReason = reason;

                    // Kalemlerin durumunu da İptal Edildi olarak güncelle
                    foreach (var oi in order.OrderItems)
                    {
                        bool isMyItem = (currentCompanyId <= 0) ||
                                        (oi.SupplierCompanyID == currentCompanyId) ||
                                        (order.SupplierCompanyID == currentCompanyId) ||
                                        (!string.IsNullOrWhiteSpace(currentCompanyName) && oi.SupplierCompanyName != null && string.Equals(oi.SupplierCompanyName.Trim(), currentCompanyName.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (isMyItem)
                        {
                            oi.ItemStatus = "İptal Edildi";
                        }
                    }

                    // Escrow (Havuz) Durumu: İade sürecini başlat
                    if (order.EscrowStatus == "InEscrow" || order.EscrowStatus == "PendingPayment")
                    {
                        order.EscrowStatus = "Refunded";
                        order.PaymentStatus = "İade Edilecek";
                    }

                    await _context.SaveChangesAsync();

                    // SignalR Bildirimleri
                    try
                    {
                        if (order.BuyerCompanyID.HasValue && order.BuyerCompanyID.Value > 0)
                        {
                            await _hubContext.Clients.Group($"company_{order.BuyerCompanyID.Value}").SendAsync("OnOrderStatusUpdatedNotification", new
                            {
                                orderId = order.OrderID,
                                orderNumber = order.OrderNumber,
                                status = "İptal Edildi",
                                trackingNumber = "",
                                message = $"⚠️ #{order.OrderNumber} numaralı siparişiniz satıcı tarafından iptal edildi. Gerekçe: {reason}"
                            });
                        }

                        await _hubContext.Clients.All.SendAsync("OnOrderStatusChanged", new
                        {
                            orderId = order.OrderID,
                            orderNumber = order.OrderNumber,
                            status = "İptal Edildi",
                            trackingNumber = "",
                            shippingCarrier = "",
                            notes = order.Notes ?? ""
                        });
                    }
                    catch (Exception hubEx)
                    {
                        Console.WriteLine($"SignalR Cancel Order error: {hubEx.Message}");
                    }

                    // Alıcıya Bilgilendirme E-postası Gönder (Async Arka Plan)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            if (_emailService != null && !string.IsNullOrWhiteSpace(order.BuyerEmail))
                            {
                                var emailItems = order.OrderItems.Select((oi, idx) => new SoldProductItemEmailDto
                                {
                                    ItemOrder = idx + 1,
                                    ProductName = oi.ProductName,
                                    Quantity = oi.Quantity,
                                    Unit = "Adet",
                                    UnitPrice = oi.UnitPrice,
                                    LineTotal = oi.LineTotal
                                }).ToList();

                                await _emailService.SendOrderCancelledBySupplierEmailAsync(
                                    toEmail: order.BuyerEmail,
                                    buyerCompanyName: order.BuyerCompanyName,
                                    supplierCompanyName: order.SupplierCompanyName ?? (!string.IsNullOrWhiteSpace(currentCompanyName) ? currentCompanyName : "Tedarikçi Firma"),
                                    orderNumber: order.OrderNumber,
                                    orderDate: order.OrderDate,
                                    grandTotal: order.GrandTotal,
                                    cancellationReason: reason,
                                    items: emailItems,
                                    buyerOrdersUrl: $"{Request.Scheme}://{Request.Host}/Dashboard/Orders"
                                );
                            }
                        }
                        catch (Exception emailEx)
                        {
                            Console.WriteLine($"İptal bildirim e-posta gönderim hatası: {emailEx.Message}");
                        }
                    });

                    return Json(new
                    {
                        success = true,
                        message = $"#{order.OrderNumber} numaralı sipariş satıcı gerekçesiyle başarıyla iptal edildi ve alıcıya bildirildi.",
                        orderId = order.OrderID,
                        status = "İptal Edildi"
                    });
                }

                // 3. KADEMELİ İLERLEME KONTROLÜ (Geriye Alınamaz)
                int targetLevel = GetOrderStatusLevel(requestedStatus);
                if (targetLevel == 0)
                {
                    return Json(new { success = false, message = $"Geçersiz sipariş durumu: '{requestedStatus}'." });
                }

                // Geriye dönük durum değişikliği engeli
                if (targetLevel < currentLevel)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Sipariş durumu geriye alınamaz! Sipariş şu anda '{currentStatus}' aşamasındadır ve geriye dönük olarak '{requestedStatus}' durumuna döndürülemez."
                    });
                }

                // Sıralı Aşama Atlama Engeli (Adım adım ilerleme kuralı)
                if (targetLevel > currentLevel + 1)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Sipariş durumları sırasıyla teker teker aktif edilmelidir! '{currentStatus}' aşamasından doğrudan '{requestedStatus}' aşamasına atlanamaz."
                    });
                }

                // Kargoya Verildi seçildiğinde kargo firması ve takip numarası ZORUNLUDUR
                if (targetLevel == 3)
                {
                    string effectiveCarrier = !string.IsNullOrWhiteSpace(input.ShippingCarrier) ? input.ShippingCarrier.Trim() : (order.ShippingCarrier ?? "");
                    string effectiveTracking = !string.IsNullOrWhiteSpace(input.TrackingNumber) ? input.TrackingNumber.Trim() : (order.TrackingNumber ?? "");

                    if (string.IsNullOrWhiteSpace(effectiveCarrier))
                    {
                        return Json(new { success = false, message = "Siparişi 'Kargoya Verildi' durumuna geçirmek için lütfen kargo firması seçiniz." });
                    }
                    if (string.IsNullOrWhiteSpace(effectiveTracking))
                    {
                        return Json(new { success = false, message = "Siparişi 'Kargoya Verildi' durumuna geçirmek için lütfen geçerli bir kargo takip numarası giriniz." });
                    }
                }

                // Standartlaştırılmış durum ismi
                string standardizedStatus = targetLevel switch
                {
                    1 => "Onaylandı",
                    2 => "Hazırlanıyor",
                    3 => "Kargoya Verildi",
                    4 => "Teslim Edildi",
                    5 => "Tamamlandı",
                    _ => requestedStatus
                };

                // Sipariş kalemlerini güncelle
                if (order.OrderItems != null && order.OrderItems.Any())
                {
                    foreach (var oi in order.OrderItems)
                    {
                        bool isMyItem = (currentCompanyId <= 0) ||
                                        (oi.SupplierCompanyID == currentCompanyId) ||
                                        (order.SupplierCompanyID == currentCompanyId) ||
                                        (!string.IsNullOrWhiteSpace(currentCompanyName) && oi.SupplierCompanyName != null && string.Equals(oi.SupplierCompanyName.Trim(), currentCompanyName.Trim(), StringComparison.OrdinalIgnoreCase));

                        if (isMyItem)
                        {
                            bool wasNotCancelled = (oi.ItemStatus != "İptal" && oi.ItemStatus != "İptal Edildi");
                            oi.ItemStatus = standardizedStatus;
                            if (!string.IsNullOrWhiteSpace(input.TrackingNumber)) oi.TrackingNumber = input.TrackingNumber.Trim();
                            if (!string.IsNullOrWhiteSpace(input.ShippingCarrier)) oi.ShippingCarrier = input.ShippingCarrier.Trim();

                            // İptal durumunda stok iadesi
                            if ((targetLevel == -1 || standardizedStatus == "İptal" || standardizedStatus == "İptal Edildi") && wasNotCancelled)
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
                    }
                }

                // ==================== ÇOK SATICILI SİPARİŞ DURUM BELİRLEME ====================
                var activeOrderItems = (order.OrderItems ?? new List<OrderItem>())
                    .Where(oi => oi.ItemStatus != "İptal" && oi.ItemStatus != "İptal Edildi")
                    .ToList();

                if (activeOrderItems.Any())
                {
                    bool allItemsDelivered = activeOrderItems.All(oi => oi.ItemStatus == "Teslim Edildi" || oi.ItemStatus == "Tamamlandı");
                    bool anyItemDelivered = activeOrderItems.Any(oi => oi.ItemStatus == "Teslim Edildi");
                    bool anyItemShipped = activeOrderItems.Any(oi => oi.ItemStatus == "Kargoya Verildi");
                    bool anyItemPreparing = activeOrderItems.Any(oi => oi.ItemStatus == "Hazırlanıyor");

                    if (allItemsDelivered)
                    {
                        // TÜM SATICILAR teslimatı tamamladı -> Ana sipariş Teslim Edildi!
                        order.OrderStatus = "Teslim Edildi";
                        order.DeliveryDate = DateTime.UtcNow;
                    }
                    else if (anyItemDelivered || anyItemShipped)
                    {
                        // En az bir satıcı teslim etti veya kargoladı ama henüz tüm satıcılar teslim etmedi -> Kargoya Verildi
                        order.OrderStatus = "Kargoya Verildi";
                    }
                    else if (anyItemPreparing)
                    {
                        order.OrderStatus = "Hazırlanıyor";
                    }
                    else
                    {
                        order.OrderStatus = standardizedStatus;
                    }
                }
                else
                {
                    order.OrderStatus = standardizedStatus;
                }

                if (!string.IsNullOrWhiteSpace(input.TrackingNumber))
                {
                    order.TrackingNumber = string.IsNullOrWhiteSpace(order.TrackingNumber) 
                        ? input.TrackingNumber.Trim() 
                        : (order.TrackingNumber.Contains(input.TrackingNumber.Trim()) ? order.TrackingNumber : $"{order.TrackingNumber}, {input.TrackingNumber.Trim()}");
                }

                if (!string.IsNullOrWhiteSpace(input.ShippingCarrier))
                {
                    order.ShippingCarrier = string.IsNullOrWhiteSpace(order.ShippingCarrier) 
                        ? input.ShippingCarrier.Trim() 
                        : (order.ShippingCarrier.Contains(input.ShippingCarrier.Trim()) ? order.ShippingCarrier : $"{order.ShippingCarrier}, {input.ShippingCarrier.Trim()}");
                }

                if (order.OrderStatus == "Teslim Edildi" && !order.DeliveryDate.HasValue)
                {
                    order.DeliveryDate = DateTime.UtcNow;
                }

                if (!string.IsNullOrWhiteSpace(input.Notes))
                {
                    order.Notes = string.IsNullOrWhiteSpace(order.Notes) ? input.Notes.Trim() : $"{order.Notes}\n{input.Notes.Trim()}";
                }

                await _context.SaveChangesAsync();

                try
                {
                    var statusPayload = new
                    {
                        orderId = order.OrderID,
                        orderNumber = order.OrderNumber,
                        status = order.OrderStatus,
                        trackingNumber = order.TrackingNumber ?? "",
                        shippingCarrier = order.ShippingCarrier ?? "",
                        notes = order.Notes ?? ""
                    };

                    if (order.BuyerCompanyID.HasValue && order.BuyerCompanyID.Value > 0)
                    {
                        await _hubContext.Clients.Group($"company_{order.BuyerCompanyID.Value}").SendAsync("OnOrderStatusUpdatedNotification", new
                        {
                            orderId = order.OrderID,
                            orderNumber = order.OrderNumber,
                            status = order.OrderStatus,
                            trackingNumber = order.TrackingNumber,
                            message = targetLevel == 4
                                ? $"✅ #{order.OrderNumber} numaralı siparişiniz teslim edildi! Lütfen siparişinizi kontrol edip teslim alma ve havuz onayını veriniz."
                                : $"📦 #{order.OrderNumber} numaralı siparişinizin durumu '{order.OrderStatus}' olarak güncellendi."
                        });
                    }

                    await _hubContext.Clients.All.SendAsync("OnOrderStatusChanged", statusPayload);
                }
                catch (Exception hubEx)
                {
                    Console.WriteLine($"SignalR UpdateOrderStatus error: {hubEx.Message}");
                }

                // ==================== KADEMELİ DURUM E-POSTA BİLDİRİMLERİ ====================
                if (_emailService != null)
                {
                    try
                    {
                        string buyerEmail = "";
                        string buyerCompanyName = order.BuyerCompanyName ?? "";

                        if (order.BuyerCompanyID.HasValue && order.BuyerCompanyID.Value > 0)
                        {
                            var buyerComp = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == order.BuyerCompanyID.Value);
                            if (buyerComp != null)
                            {
                                buyerEmail = buyerComp.CompanyMail ?? "";
                                if (string.IsNullOrWhiteSpace(buyerCompanyName))
                                {
                                    buyerCompanyName = buyerComp.CompanyName ?? buyerComp.CompanyOfficial ?? "";
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(buyerEmail))
                        {
                            var emailItems = (order.OrderItems ?? new List<OrderItem>()).Select((oi, idx) => new SoldProductItemEmailDto
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

                            string supplierName = !string.IsNullOrWhiteSpace(order.SupplierCompanyName) ? order.SupplierCompanyName : currentCompanyName;

                            if (targetLevel == 2)
                            {
                                // 1. Ürün Hazırlanıyor E-postası
                                await _emailService.SendOrderPreparingEmailAsync(
                                    toEmail: buyerEmail,
                                    buyerCompanyName: buyerCompanyName,
                                    supplierCompanyName: supplierName,
                                    orderNumber: order.OrderNumber,
                                    orderDate: order.OrderDate,
                                    grandTotal: order.GrandTotal,
                                    items: emailItems);
                            }
                            else if (targetLevel == 3)
                            {
                                // 2. Paket Kargolandı E-postası
                                await _emailService.SendOrderShippedEmailAsync(
                                    toEmail: buyerEmail,
                                    buyerCompanyName: buyerCompanyName,
                                    supplierCompanyName: supplierName,
                                    orderNumber: order.OrderNumber,
                                    orderDate: order.OrderDate,
                                    grandTotal: order.GrandTotal,
                                    shippingCarrier: order.ShippingCarrier ?? input.ShippingCarrier ?? "Kargo / Lojistik",
                                    trackingNumber: order.TrackingNumber ?? input.TrackingNumber ?? "",
                                    items: emailItems);
                            }
                            else if (targetLevel == 4)
                            {
                                // 3. Ürün Teslim Edildi E-postası
                                await _emailService.SendOrderDeliveredEmailAsync(
                                    toEmail: buyerEmail,
                                    buyerCompanyName: buyerCompanyName,
                                    supplierCompanyName: supplierName,
                                    orderNumber: order.OrderNumber,
                                    orderDate: order.OrderDate,
                                    grandTotal: order.GrandTotal,
                                    shippingCarrier: order.ShippingCarrier,
                                    trackingNumber: order.TrackingNumber,
                                    items: emailItems);
                            }
                        }
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine($"Kademeli sipariş e-posta gönderim hatası: {emailEx.Message}");
                    }
                }

                return Json(new
                {
                    success = true,
                    message = $"#{order.OrderNumber} numaralı sipariş durumu '{order.OrderStatus}' olarak başarıyla güncellendi.",
                    orderId = order.OrderID,
                    status = order.OrderStatus,
                    trackingNumber = order.TrackingNumber,
                    shippingCarrier = order.ShippingCarrier
                });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Sipariş güncellenirken hata oluştu: " + detail });
            }
        }

        /// <summary>
        /// Alıcı firmanın siparişi gerekçe belirterek iptal etmesi.
        /// Yalnızca sipariş 'Onaylandı' veya 'Hazırlanıyor' aşamasında iken mümkündür.
        /// Kargoya Verildi veya sonraki aşamalarda iptal KESİNLİKLE ENGELLENİR.
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CancelOrderByBuyer([FromBody] CancelOrderByBuyerInputModel input)
        {
            if (input == null || input.OrderID <= 0)
            {
                return Json(new { success = false, message = "Geçersiz sipariş ID." });
            }

            if (string.IsNullOrWhiteSpace(input.Reason))
            {
                return Json(new { success = false, message = "Lütfen siparişi iptal etme gerekçenizi belirtiniz." });
            }

            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderID == input.OrderID);

                if (order == null)
                {
                    return Json(new { success = false, message = "Sipariş veritabanında bulunamadı." });
                }

                int currentCompanyId = GetCurrentCompanyId();
                string currentEmail = GetCurrentEmail();

                bool isBuyer = (currentCompanyId > 0 && order.BuyerCompanyID == currentCompanyId) ||
                               (!string.IsNullOrWhiteSpace(currentEmail) && !string.IsNullOrWhiteSpace(order.BuyerEmail) && string.Equals(order.BuyerEmail.Trim(), currentEmail.Trim(), StringComparison.OrdinalIgnoreCase)) ||
                               IsSuperAdmin();

                if (!isBuyer)
                {
                    return Json(new { success = false, message = "Bu siparişi yalnızca siparişi veren alıcı firma iptal edebilir." });
                }

                string currentStatus = !string.IsNullOrWhiteSpace(order.OrderStatus) ? order.OrderStatus.Trim() : "Onaylandı";
                int currentLevel = GetOrderStatusLevel(currentStatus);

                // Kargoya Verildi veya sonraki aşamalarda ALICI İPTAL EDEMEZ!
                if (currentLevel >= 3)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Siparişiniz '{currentStatus}' aşamasındadır. Kargonuz yola çıktığı veya teslimat başladığı için siparişinizi iptal edemezsiniz. Ürün size ulaştığında teslim alma aşamasında itiraz/iade bildiriminde bulunabilirsiniz."
                    });
                }

                var cancelTimestamp = DateTime.UtcNow.AddHours(3).ToString("dd.MM.yyyy HH:mm");
                string cancelLog = $"[Alıcı İptal Gerekçesi ({cancelTimestamp})]: {input.Reason.Trim()}";

                order.OrderStatus = "İptal Edildi";
                order.Notes = string.IsNullOrWhiteSpace(order.Notes) ? cancelLog : $"{order.Notes}\n{cancelLog}";
                order.DisputeReason = input.Reason.Trim();

                foreach (var oi in order.OrderItems)
                {
                    oi.ItemStatus = "İptal Edildi";
                    if (oi.ItemID.HasValue && oi.ItemID.Value > 0)
                    {
                        var dbItem = await _context.Items.FirstOrDefaultAsync(i => i.ItemID == oi.ItemID.Value);
                        if (dbItem != null && dbItem.Quantity.HasValue)
                        {
                            dbItem.Quantity += oi.Quantity;
                        }
                    }
                }

                if (order.EscrowStatus == "InEscrow" || order.EscrowStatus == "PendingPayment")
                {
                    order.EscrowStatus = "Refunded";
                    order.PaymentStatus = "İade Edilecek";
                }

                await _context.SaveChangesAsync();

                // SignalR Bildirimleri
                try
                {
                    if (order.SupplierCompanyID.HasValue && order.SupplierCompanyID.Value > 0)
                    {
                        await _hubContext.Clients.Group($"company_{order.SupplierCompanyID.Value}").SendAsync("OnOrderStatusUpdatedNotification", new
                        {
                            orderId = order.OrderID,
                            orderNumber = order.OrderNumber,
                            status = "İptal Edildi",
                            trackingNumber = "",
                            message = $"⚠️ #{order.OrderNumber} numaralı sipariş alıcı firma tarafından iptal edildi. Gerekçe: {input.Reason.Trim()}"
                        });
                    }

                    await _hubContext.Clients.All.SendAsync("OnOrderStatusChanged", new
                    {
                        orderId = order.OrderID,
                        orderNumber = order.OrderNumber,
                        status = "İptal Edildi",
                        trackingNumber = "",
                        shippingCarrier = "",
                        notes = order.Notes ?? ""
                    });
                }
                catch (Exception hubEx)
                {
                    Console.WriteLine($"SignalR Buyer Cancel error: {hubEx.Message}");
                }

                return Json(new
                {
                    success = true,
                    message = $"#{order.OrderNumber} numaralı siparişiniz başarıyla iptal edildi. Havuzdaki ödemeniz kartınıza kesintisiz iade edilecektir.",
                    orderId = order.OrderID,
                    status = "İptal Edildi"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Sipariş iptal edilirken hata oluştu: " + ex.Message });
            }
        }

        /// <summary>
        /// Yeni kategori veya alt kategori oluşturur.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCategory(string categoryName, int? parentCategoryId, CancellationToken cancellationToken)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                          (Request.Headers.Accept.ToString().Contains("application/json") && !Request.Headers.Accept.ToString().Contains("text/html"));

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                var reqMsg = "Lütfen bir kategori adı girin.";
                if (isAjax) return Json(new { success = false, message = reqMsg });
                TempData["CategoryError"] = reqMsg;
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var created = await _dashboardService.CreateCategoryAsync(categoryName, parentCategoryId, cancellationToken);

                try
                {
                    await _hubContext.Clients.All.SendAsync("OnCategoryCreated", new
                    {
                        categoryId = created.CategoryID,
                        categoryName = created.CategoryName,
                        parentCategoryId = created.ParentCategoryID
                    });
                }
                catch (Exception hubEx)
                {
                    Console.WriteLine($"SignalR CreateCategory error: {hubEx.Message}");
                }

                var succMsg = parentCategoryId.HasValue && parentCategoryId.Value > 0
                    ? $"Alt kategori '{created.CategoryName}' başarıyla eklendi!" 
                    : $"Kök kategori '{created.CategoryName}' başarıyla eklendi!";

                if (isAjax)
                {
                    return Json(new
                    {
                        success = true,
                        message = succMsg,
                        categoryId = created.CategoryID,
                        categoryName = created.CategoryName,
                        parentCategoryId = created.ParentCategoryID
                    });
                }

                TempData["CategorySuccess"] = succMsg;
            }
            catch (Exception ex)
            {
                var errMsg = "Kategori oluşturulurken hata oluştu: " + ex.Message;
                if (isAjax) return Json(new { success = false, message = errMsg });
                TempData["CategoryError"] = errMsg;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
