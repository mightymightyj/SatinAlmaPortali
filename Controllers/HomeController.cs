using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using satinalmaportali2.Data;
using satinalmaportali2.Hubs;
using satinalmaportali2.Models;
using satinalmaportali2.Services;
using satinalmaportali2.Helpers;
using System.Diagnostics;
using System.Security.Claims;

namespace satinalmaportali2.Controllers {
    public class HomeController : Controller {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly IHubContext<PortalHub> _hubContext;
        private readonly ICloudinaryService _cloudinaryService;

        public HomeController(
            ApplicationDbContext context, 
            EmailService emailService, 
            IHubContext<PortalHub> hubContext,
            ICloudinaryService cloudinaryService) {
            _context = context;
            _emailService = emailService;
            _hubContext = hubContext;
            _cloudinaryService = cloudinaryService;
        }

        /// <summary>
        /// Cloudinary üzerinden ürün görseli yükleme endpoint'i (AJAX).
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

        public async Task<IActionResult> Index() {
            try {
                var dbCategories = await _context.Category.AsNoTracking().ToListAsync();
                var catMap = dbCategories.ToDictionary(c => c.CategoryID, c => c.CategoryName ?? "");

                var tree = BuildCategoryHierarchyRecursive(dbCategories, null, "", 0, catMap);

                // Yetim / geçersiz parent ID'ye sahip kategorileri de güvenle ağaca bağla
                var visitedIds = new HashSet<int>();
                void CollectVisited(List<HierarchicalCategoryItem> nodes) {
                    foreach (var n in nodes) {
                        visitedIds.Add(n.CategoryID);
                        CollectVisited(n.Children);
                    }
                }
                CollectVisited(tree);

                var orphans = dbCategories.Where(c => !visitedIds.Contains(c.CategoryID)).ToList();
                foreach (var orphan in orphans) {
                    var orphanNode = new HierarchicalCategoryItem {
                        CategoryID = orphan.CategoryID,
                        CategoryName = orphan.CategoryName ?? "Kategori #" + orphan.CategoryID,
                        ParentCategoryID = orphan.ParentCategoryID,
                        BreadcrumbPath = orphan.CategoryName ?? "",
                        FormattedName = "📁 " + (orphan.CategoryName ?? "Kategori #" + orphan.CategoryID),
                        Level = 0
                    };
                    orphanNode.Children = BuildCategoryHierarchyRecursive(dbCategories, orphan.CategoryID, orphan.CategoryName ?? "", 1, catMap);
                    orphanNode.HasChildren = orphanNode.Children.Count > 0;
                    tree.Add(orphanNode);
                }

                var flatDropdownList = new List<HierarchicalCategoryItem>();
                FlattenHierarchy(tree, flatDropdownList);

                // Gerçek veritabanı istatistikleri
                var totalBuyers = await _context.Companies.CountAsync(c => c.CompanyTypeID == 2 || c.CompanyTypeID == 3);
                var totalSuppliers = await _context.Companies.CountAsync(c => c.CompanyTypeID == 1 || c.CompanyTypeID == 3);
                var openRequests = await _context.Requests.CountAsync(r => r.Status != "Kaldırıldı" && (r.Status == "Açık" || r.Status == "Beklemede" || r.Status == "İncelemede" || string.IsNullOrEmpty(r.Status)));
                var completedOffers = await _context.RequestOffers.CountAsync(o => o.Status == "Kabul Edildi" || o.Status == "Onaylandı");
                var totalItems = await _context.Items.CountAsync(i => !i.IsDeleted);

                ViewBag.TotalBuyers = totalBuyers;
                ViewBag.TotalSuppliers = totalSuppliers;
                ViewBag.OpenRequestsCount = openRequests;
                ViewBag.CompletedOffersCount = completedOffers;
                ViewBag.TotalItemsCount = totalItems;

                ViewBag.CategoryTree = tree;
                ViewBag.HierarchicalCategories = flatDropdownList;
                ViewBag.Categories = dbCategories;
            }
            catch (Exception ex) {
                Console.WriteLine("HomeController Index Category Load Error: " + ex.Message);
                ViewBag.CategoryTree = new List<HierarchicalCategoryItem>();
                ViewBag.HierarchicalCategories = new List<HierarchicalCategoryItem>();
                ViewBag.Categories = new List<Category>();
                ViewBag.TotalBuyers = 0;
                ViewBag.TotalSuppliers = 0;
                ViewBag.OpenRequestsCount = 0;
                ViewBag.CompletedOffersCount = 0;
                ViewBag.TotalItemsCount = 0;
            }
            return View();
        }

        [HttpGet]
        [Route("Home/GetCategoriesJson")]
        public async Task<IActionResult> GetCategoriesJson() {
            try {
                var dbCategories = await _context.Category.AsNoTracking().ToListAsync();
                var catMap = dbCategories.ToDictionary(c => c.CategoryID, c => c.CategoryName ?? "");
                var tree = BuildCategoryHierarchyRecursive(dbCategories, null, "", 0, catMap);
                var flatList = new List<HierarchicalCategoryItem>();
                FlattenHierarchy(tree, flatList);
                return Json(new { success = true, tree = tree, flat = flatList });
            }
            catch (Exception ex) {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private static List<HierarchicalCategoryItem> BuildCategoryHierarchyRecursive(
            List<Category> allCategories, 
            int? parentId, 
            string parentPath, 
            int level,
            Dictionary<int, string> catMap) 
        {
            var children = allCategories
                .Where(c => parentId == null 
                    ? (!c.ParentCategoryID.HasValue || c.ParentCategoryID.Value == 0)
                    : (c.ParentCategoryID == parentId.Value))
                .OrderBy(c => c.CategoryName)
                .ToList();

            var result = new List<HierarchicalCategoryItem>();
            foreach (var c in children) {
                var catName = c.CategoryName ?? ("Kategori #" + c.CategoryID);
                var currentPath = string.IsNullOrEmpty(parentPath) ? catName : $"{parentPath} > {catName}";

                string prefix = level switch {
                    0 => "📁 ",
                    1 => "  ↳ 📂 ",
                    2 => "    ↳ 🏷️ ",
                    3 => "      ↳ 🔹 ",
                    _ => new string(' ', level * 2) + "↳ • "
                };

                var node = new HierarchicalCategoryItem {
                    CategoryID = c.CategoryID,
                    CategoryName = catName,
                    ParentCategoryID = c.ParentCategoryID,
                    ParentCategoryName = c.ParentCategoryID.HasValue && catMap.TryGetValue(c.ParentCategoryID.Value, out var pName) ? pName : null,
                    BreadcrumbPath = currentPath,
                    FormattedName = prefix + catName,
                    Level = level
                };

                node.Children = BuildCategoryHierarchyRecursive(allCategories, c.CategoryID, currentPath, level + 1, catMap);
                node.HasChildren = node.Children.Count > 0;
                result.Add(node);
            }
            return result;
        }

        private static void FlattenHierarchy(List<HierarchicalCategoryItem> nodes, List<HierarchicalCategoryItem> flatList) {
            foreach (var node in nodes) {
                flatList.Add(node);
                if (node.Children.Count > 0) {
                    FlattenHierarchy(node.Children, flatList);
                }
            }
        }

        public IActionResult Privacy() {
            return View();
        }

        public IActionResult Kurumsal() {
            return View();
        }

        public IActionResult UrunKategorileri() {
            return View();
        }

        public IActionResult NedenPortal() {
            return View();
        }

        public async Task<IActionResult> Fiyatlandirma() {
            var (currentCompanyId, currentCompanyName, comp) = await GetCurrentCompanyContextAsync();
            int currentTierId = comp?.MembershipTypeID ?? MembershipConstants.TierBedava;
            string currentTierName = MembershipConstants.GetTierName(currentTierId);

            var viewModel = new PricingPageViewModel
            {
                IsAuthenticated = User?.Identity?.IsAuthenticated == true && currentCompanyId > 0,
                CurrentCompanyId = currentCompanyId,
                CurrentCompanyName = currentCompanyName,
                CurrentCompanyEmail = comp?.CompanyMail ?? User?.FindFirst(ClaimTypes.Email)?.Value ?? "",
                CurrentMembershipTypeId = currentTierId,
                CurrentMembershipTypeName = currentTierName,
                Plans = new List<MembershipPlanDto>
                {
                    new MembershipPlanDto
                    {
                        TierId = MembershipConstants.TierBedava,
                        Name = MembershipConstants.NameBedava,
                        Badge = "Başlangıç",
                        PriceMonthly = MembershipConstants.BedavaPrice,
                        Description = "Yeni başlayan işletmeler ve temel satın alma/satış operasyonları için.",
                        MaxRequests = MembershipConstants.BedavaMaxRequests,
                        MaxProducts = MembershipConstants.BedavaMaxProducts,
                        MaxImagesPerProduct = MembershipConstants.BedavaMaxImagesPerProduct,
                        IsCurrentPlan = currentTierId == MembershipConstants.TierBedava,
                        Highlights = new List<string>
                        {
                            "Aynı anda 10 adet aktif talep hakkı",
                            "Aynı anda 10 adet ürün satış ilanı",
                            "Her ürün için 3 görsel hakkı",
                            "Standart B2B tedarikçi pazar yeri erişimi",
                            "Temel teklif alma ve teklif verme",
                            "E-posta desteği"
                        }
                    },
                    new MembershipPlanDto
                    {
                        TierId = MembershipConstants.TierPro,
                        Name = MembershipConstants.NamePro,
                        Badge = "En Çok Tercih Edilen",
                        PriceMonthly = MembershipConstants.ProPrice,
                        Description = "Büyüyen ve satış/tedarik hacmini hızlandırmak isteyen aktif işletmeler için.",
                        MaxRequests = MembershipConstants.ProMaxRequests,
                        MaxProducts = MembershipConstants.ProMaxProducts,
                        MaxImagesPerProduct = MembershipConstants.ProMaxImagesPerProduct,
                        IsCurrentPlan = currentTierId == MembershipConstants.TierPro,
                        Highlights = new List<string>
                        {
                            "Aynı anda 100 adet aktif talep hakkı",
                            "Aynı anda 100 adet ürün satış ilanı",
                            "Her ürün için 20 görsel hakkı",
                            "B2B Vitrinde öne çıkarılan ürünler",
                            "Gelişmiş teklif değerlendirme araçları",
                            "7/24 Öncelikli destek"
                        }
                    },
                    new MembershipPlanDto
                    {
                        TierId = MembershipConstants.TierKurumsal,
                        Name = MembershipConstants.NameKurumsal,
                        Badge = "Maksimum Güç",
                        PriceMonthly = MembershipConstants.KurumsalPrice,
                        Description = "Büyük ölçekli üretici, distribütör ve yüksek hacimli satın alma yapan kurumlar için.",
                        MaxRequests = MembershipConstants.KurumsalMaxRequests,
                        MaxProducts = MembershipConstants.KurumsalMaxProducts,
                        MaxImagesPerProduct = MembershipConstants.KurumsalMaxImagesPerProduct,
                        IsCurrentPlan = currentTierId == MembershipConstants.TierKurumsal,
                        Highlights = new List<string>
                        {
                            "Sınırsız aktif talep oluşturma",
                            "Sınırsız ürün satışı ilanı listeleme",
                            "Her ürün için sınırsız görsel ekleme",
                            "Onaylı Kurumsal Tedarikçi rozeti",
                            "Öncelikli arama ve vitrin listelemesi",
                            "7/24 VIP Müşteri Yöneticisi & SLA garantisi"
                        }
                    }
                }
            };

            return View(viewModel);
        }

        public IActionResult MedyaDuyurular() {
            return View();
        }

        public IActionResult Iletisim() {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> IletisimSubmit(string adSoyad, string eposta, string telefon, string konu, string mesaj) {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                          (Request.Headers.Accept.ToString().Contains("application/json") && !Request.Headers.Accept.ToString().Contains("text/html"));

            if (string.IsNullOrWhiteSpace(adSoyad) || string.IsNullOrWhiteSpace(eposta) || string.IsNullOrWhiteSpace(mesaj)) {
                var reqMsg = "Lütfen zorunlu alanları (Ad Soyad, E-posta, Mesaj) eksiksiz doldurunuz.";
                if (isAjax) return Json(new { success = false, message = reqMsg });
                TempData["IletisimError"] = reqMsg;
                return RedirectToAction("Iletisim");
            }

            try {
                // 1. EF Core ile Mesajı Veritabanına (Forms tablosu) Kaydet
                var names = adSoyad.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string firstName = names.Length > 0 ? names[0] : adSoyad;
                string lastName = names.Length > 1 ? string.Join(" ", names.Skip(1)) : "";

                var formRecord = new Forms {
                    Name = firstName,
                    Surname = lastName,
                    Email = eposta.Trim(),
                    Phone = string.IsNullOrWhiteSpace(telefon) ? null : telefon.Trim(),
                    Topic = string.IsNullOrWhiteSpace(konu) ? "Genel" : konu,
                    FormText = mesaj.Trim(),
                    FormDate = DateTime.UtcNow
                };

                _context.Forms.Add(formRecord);
                await _context.SaveChangesAsync();

                var succMsg = "Mesajınız başarıyla sistemimize kaydedildi. En kısa sürede sizinle iletişime geçeceğiz.";
                if (isAjax) return Json(new { success = true, message = succMsg });
                TempData["IletisimSuccess"] = succMsg;
            }
            catch (Exception ex) {
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine($"IletisimSubmit Error: {innerMsg}");
                var errMsg = "Mesajınız gönderilirken bir hata oluştu. Lütfen daha sonra tekrar deneyiniz.";
                if (isAjax) return Json(new { success = false, message = errMsg });
                TempData["IletisimError"] = errMsg;
            }

            return RedirectToAction("Iletisim");
        }

        private async Task<(int companyId, string companyName, Company? company)> GetCurrentCompanyContextAsync()
        {
            string? currentCompanyName = User.FindFirst("CompanyName")?.Value;
            string? idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("CompanyID");
            int currentCompanyId = 0;
            if (!string.IsNullOrEmpty(idClaim) && int.TryParse(idClaim, out int cid))
            {
                currentCompanyId = cid;
            }

            Company? comp = null;
            if (currentCompanyId > 0)
            {
                comp = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId);
                if (string.IsNullOrWhiteSpace(currentCompanyName))
                {
                    currentCompanyName = comp?.CompanyName ?? comp?.CompanyOfficial;
                }
            }

            return (currentCompanyId, currentCompanyName ?? string.Empty, comp);
        }

        public static bool IsProductOwnedByCompany(ItemsModel item, int currentCompanyId, string currentCompanyName, Company? comp)
        {
            if (currentCompanyId <= 0 && string.IsNullOrWhiteSpace(currentCompanyName))
                return false;

            // 1. Properties içindeki FirmaID kontrolü
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

            // 2. Brand alanı üzerinden firma adı / yetkili / e-posta eşleşmesi
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
        /// Tedarikçinin vitrine doğrudan ürün/portföy ilanı eklemesini sağlayan AJAX endpoint'i
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SubmitSellerOffer([FromForm] CreateProductListingInputModel input, CancellationToken cancellationToken)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.ProductName) || input.UnitPrice <= 0)
            {
                return Json(new { success = false, message = "Lütfen ürün adını ve geçerli bir birim fiyat giriniz." });
            }

            try
            {
                // 1. Oturum açmış firmanın bilgilerini tespit et
                var (currentCompanyId, currentCompanyName, comp) = await GetCurrentCompanyContextAsync();

                if (currentCompanyId <= 0 || User?.Identity?.IsAuthenticated != true || comp == null)
                {
                    return Json(new 
                    { 
                        success = false, 
                        requireLogin = true, 
                        message = "Ürün satışa sunabilmek için lütfen kurumsal satıcı hesabınızla giriş yapınız veya yeni bir firma hesabı oluşturunuz." 
                    });
                }

                // 2. iyzico Alt Üye İşyeri (SubMerchant) hesap açılışı kontrolü
                bool hasIyzicoSubMerchant = comp.IsIyzicoApproved || !string.IsNullOrWhiteSpace(comp.IyzicoSubMerchantKey);
                if (!hasIyzicoSubMerchant)
                {
                    return Json(new
                    {
                        success = false,
                        requireIyzico = true,
                        message = "Ürün satışa koyabilmek ve ödemelerinizi güvence havuzundan tahsil edebilmek için öncelikle iyzico Satıcı (Alt Üye İşyeri) kaydınızı tamamlamanız gerekmektedir."
                    });
                }

                // 2.5 Üyelik Seviyesi Kontrolleri (Ürün Satış Limiti ve Görsel Limiti)
                int memTypeId = comp.MembershipTypeID ?? MembershipConstants.TierBedava;
                int maxProducts = MembershipConstants.GetMaxProducts(memTypeId);
                if (maxProducts < int.MaxValue)
                {
                    int currentProductCount = await _context.Items
                        .Where(i => !i.IsDeleted && ((i.Properties != null && (i.Properties.Contains($"FirmaID: {currentCompanyId}") || i.Properties.Contains($"FirmaID:{currentCompanyId}")))
                                 || (!string.IsNullOrEmpty(currentCompanyName) && i.Brand == currentCompanyName)))
                        .CountAsync(cancellationToken);

                    if (currentProductCount >= maxProducts)
                    {
                        string tierName = MembershipConstants.GetTierName(memTypeId);
                        return Json(new
                        {
                            success = false,
                            limitExceeded = true,
                            message = $"{tierName} üyelik planınızda aynı anda en fazla {maxProducts} adet ürün satış ilanı verebilirsiniz. (Mevcut ilanınız: {currentProductCount}). Yeni ürün satışı eklemek için eski ilanlarınızı kaldırabilir veya üyeliğinizi Pro / Kurumsal plana yükseltebilirsiniz."
                        });
                    }
                }

                int maxImages = MembershipConstants.GetMaxImagesPerProduct(memTypeId);
                if (input.ImageFiles != null && input.ImageFiles.Any())
                {
                    if (maxImages < int.MaxValue && input.ImageFiles.Count > maxImages)
                    {
                        string tierName = MembershipConstants.GetTierName(memTypeId);
                        return Json(new
                        {
                            success = false,
                            message = $"{tierName} üyelik planınızda ürün başına en fazla {maxImages} resim ekleyebilirsiniz. Seçtiğiniz resim sayısı: {input.ImageFiles.Count}. Lütfen en fazla {maxImages} adet resim seçiniz veya planınızı yükseltiniz."
                        });
                    }
                }

                string finalBrand = !string.IsNullOrWhiteSpace(currentCompanyName) 
                    ? currentCompanyName.Trim() 
                    : (!string.IsNullOrWhiteSpace(input.Brand) ? input.Brand.Trim() : "Onaylı Tedarikçi");

                // 3. Kategori ID Çözümleme
                int? finalCatId = input.CategoryID;
                string finalCatName = input.CategoryName ?? "Endüstriyel Ürünler";

                if ((!finalCatId.HasValue || finalCatId.Value <= 0) && !string.IsNullOrWhiteSpace(input.CategoryName))
                {
                    var matchedCat = await _context.Category
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.CategoryName != null && c.CategoryName.ToLower() == input.CategoryName.Trim().ToLower());
                    if (matchedCat != null)
                    {
                        finalCatId = matchedCat.CategoryID;
                        finalCatName = matchedCat.CategoryName ?? finalCatName;
                    }
                    else
                    {
                        finalCatId = null;
                    }
                }
                else if (finalCatId.HasValue && finalCatId.Value > 0)
                {
                    var matchedCat = await _context.Category.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryID == finalCatId.Value);
                    if (matchedCat != null)
                    {
                        if (!string.IsNullOrWhiteSpace(matchedCat.CategoryName))
                            finalCatName = matchedCat.CategoryName;
                    }
                    else
                    {
                        finalCatId = null;
                    }
                }
                else
                {
                    finalCatId = null;
                }

                // 3. Stok adedini sayıya dönüştür
                int parsedQty = 100;
                if (!string.IsNullOrWhiteSpace(input.StockQuantity))
                {
                    var digitsOnly = new string(input.StockQuantity.Where(char.IsDigit).ToArray());
                    if (int.TryParse(digitsOnly, out int qtyVal) && qtyVal > 0)
                    {
                        parsedQty = qtyVal;
                    }
                }

                string propText = currentCompanyId > 0 ? $"FirmaID: {currentCompanyId}" : "";
                if (!string.IsNullOrWhiteSpace(input.Properties))
                {
                    propText = string.IsNullOrEmpty(propText) ? input.Properties.Trim() : $"{propText} | {input.Properties.Trim()}";
                }
                else
                {
                    propText = string.IsNullOrEmpty(propText) ? $"Kategori: {finalCatName}" : $"{propText} | Kategori: {finalCatName}";
                }

                // 4. Veritabanına (items tablosu) yeni ürün kaydını ekle
                var newItem = new ItemsModel
                {
                    ItemName = input.ProductName.Trim(),
                    ItemPrice = (double)input.UnitPrice,
                    Quantity = parsedQty,
                    CategoryID = finalCatId,
                    Brand = finalBrand,
                    ItemDescryption = !string.IsNullOrWhiteSpace(input.ItemDescription) ? input.ItemDescription.Trim() : null,
                    Properties = propText
                };

                _context.Items.Add(newItem);
                await _context.SaveChangesAsync(cancellationToken);

                // 5. Görselleri Cloudinary'ye yükle ve itemimages tablosuna kaydet
                var allImageUrls = new List<string>();
                string mainImageUrl = "/images/default-product.svg";

                if (input.ImageFiles != null && input.ImageFiles.Any())
                {
                    // Üyelik planına göre görsel sınırı (Bedava: 3, Pro: 20, Kurumsal: Sınırsız/50)
                    var filesToProcess = input.ImageFiles.Take(maxImages < int.MaxValue ? maxImages : 50).ToList();
                    var uploadedCloudinaryList = new List<ItemImage>();

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
                                bool isMain = (i == input.MainImageIndex);
                                if (isMain) mainImageUrl = uploadResult.SecureUrl;

                                var itemImg = new ItemImage
                                {
                                    ItemID = newItem.ItemID,
                                    ImageUrl = uploadResult.SecureUrl,
                                    PublicId = uploadResult.PublicId,
                                    IsMain = isMain,
                                    DisplayOrder = i,
                                    CreatedAt = DateTime.UtcNow
                                };
                                uploadedCloudinaryList.Add(itemImg);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Cloudinary upload error on SubmitSellerOffer: {ex.Message}");
                        }
                    }

                    if (uploadedCloudinaryList.Any())
                    {
                        if (!uploadedCloudinaryList.Any(x => x.IsMain))
                        {
                            uploadedCloudinaryList[0].IsMain = true;
                            mainImageUrl = uploadedCloudinaryList[0].ImageUrl;
                        }

                        _context.ItemImages.AddRange(uploadedCloudinaryList);
                        await _context.SaveChangesAsync(cancellationToken);

                        allImageUrls = uploadedCloudinaryList
                            .OrderByDescending(img => img.IsMain)
                            .ThenBy(img => img.DisplayOrder)
                            .Select(img => img.ImageUrl)
                            .ToList();
                    }
                }
                else if (input.Images != null && input.Images.Any())
                {
                    bool hasMainFlag = input.Images.Any(img => img.IsMain);
                    for (int i = 0; i < input.Images.Count; i++)
                    {
                        var imgInput = input.Images[i];
                        bool isMain = hasMainFlag ? imgInput.IsMain : (i == 0);
                        if (isMain) mainImageUrl = imgInput.ImageUrl;

                        var itemImg = new ItemImage
                        {
                            ItemID = newItem.ItemID,
                            ImageUrl = imgInput.ImageUrl,
                            PublicId = imgInput.PublicId,
                            IsMain = isMain,
                            DisplayOrder = imgInput.DisplayOrder > 0 ? imgInput.DisplayOrder : i,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.ItemImages.Add(itemImg);
                    }
                    await _context.SaveChangesAsync(cancellationToken);

                    allImageUrls = input.Images
                        .OrderByDescending(img => img.IsMain)
                        .ThenBy(img => img.DisplayOrder)
                        .Select(img => img.ImageUrl)
                        .ToList();
                }
                else if (!string.IsNullOrWhiteSpace(input.ImageUrl))
                {
                    mainImageUrl = input.ImageUrl.Trim();
                    allImageUrls.Add(mainImageUrl);

                    _context.ItemImages.Add(new ItemImage
                    {
                        ItemID = newItem.ItemID,
                        ImageUrl = mainImageUrl,
                        IsMain = true,
                        DisplayOrder = 0,
                        CreatedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    allImageUrls.Add(mainImageUrl);
                }

                // Geriye dönük uyumluluk için Properties alanına ana resmi de iliştir
                if (!newItem.Properties.Contains("Resim:") && mainImageUrl != "/images/default-product.svg")
                {
                    newItem.Properties = $"{newItem.Properties} | Resim: {mainImageUrl}";
                    await _context.SaveChangesAsync(cancellationToken);
                }

                string formattedSku = $"SKU-B2B-{newItem.ItemID:D4}";

                // Firmanın gerçek değerlendirme puanı ve sayısını hesapla
                var compReviews = await _context.Reviews
                    .AsNoTracking()
                    .Where(r => r.CompanyID == currentCompanyId && r.ReviewScore.HasValue && r.ReviewType == "BuyerToSeller")
                    .ToListAsync();

                double realSellerScore = compReviews.Any() ? Math.Round(compReviews.Average(r => r.ReviewScore!.Value), 1) : 10.0;
                int realReviewCount = compReviews.Count;

                var productPayload = new
                {
                    id = newItem.ItemID,
                    sku = formattedSku,
                    title = newItem.ItemName,
                    brand = finalBrand,
                    seller = finalBrand,
                    sellerScore = realSellerScore,
                    sellerReviewCount = realReviewCount,
                    sellerBadge = realReviewCount > 0 ? $"Onaylı Satıcı ({realReviewCount} Değerlendirme)" : "Yeni Onaylı Satıcı",
                    sellerLocation = "Türkiye",
                    category = finalCatName.ToLowerInvariant().Contains("metal") ? "metal" : 
                               finalCatName.ToLowerInvariant().Contains("it") || finalCatName.ToLowerInvariant().Contains("bilişim") ? "it" :
                               finalCatName.ToLowerInvariant().Contains("kimya") ? "chemical" : "hardware",
                    categoryName = finalCatName,
                    price = (decimal)(newItem.ItemPrice ?? 0),
                    oldPrice = Math.Round((decimal)(newItem.ItemPrice ?? 0) * 1.18m, 0),
                    discount = 15,
                    moq = "1 Adet",
                    unit = "Adet",
                    taxText = "+ %20 KDV",
                    stockStatus = $"{newItem.Quantity ?? 100} Adet Hazır Stok",
                    leadTime = "1-2 İş Günü",
                    tierPricing = $"1-4 Adet: {(decimal)(newItem.ItemPrice ?? 0):N0} ₺ | 5+ Adet: {Math.Round((decimal)(newItem.ItemPrice ?? 0) * 0.92m, 0):N0} ₺",
                    image = mainImageUrl,
                    images = allImageUrls,
                    badge = "Doğrudan Üreticiden",
                    badgeType = "manufacturer",
                    sellerCompanyId = currentCompanyId,
                    sellerCompanyName = currentCompanyName
                };

                // Asenkron SignalR yayını (Tüm bağlı alıcılara ve vitrine anında düşsün)
                try
                {
                    await _hubContext.Clients.Group("group_global").SendAsync("OnProductListed", productPayload);
                }
                catch (Exception hubEx)
                {
                    Console.WriteLine($"SignalR OnProductListed error: {hubEx.Message}");
                }

                return Json(new
                {
                    success = true,
                    message = $"🏢 \"{newItem.ItemName}\" ürün ilanınız [{finalCatName}] kategorisinde başarıyla yayına alındı.",
                    item = productPayload
                });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine($"SubmitSellerOffer Error: {detail}");
                return Json(new { success = false, message = "İlan kaydedilirken bir hata oluştu: " + detail });
            }
        }

        /// <summary>
        /// B2B Vitrindeki ürün için kurumsal teklif talebi (RFQ) oluşturma endpoint'i (AJAX)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SubmitQuoteRequest([FromBody] SubmitVitrinQuoteInputModel input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.BuyerCompany) || input.Quantity <= 0)
            {
                return Json(new { success = false, message = "Lütfen firma ünvanı ve geçerli bir miktar giriniz." });
            }

            try
            {
                var (currentCompanyId, currentCompanyName, comp) = await GetCurrentCompanyContextAsync();

                string prodName = "Kurumsal Vitrin Ürünü";
                Company? supplierCompany = null;

                if (input.ProductID.HasValue && input.ProductID.Value > 0)
                {
                    var item = await _context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.ItemID == input.ProductID.Value);
                    if (item != null)
                    {
                        // Kendi ürününe teklif isteme kontrolü
                        if (IsProductOwnedByCompany(item, currentCompanyId, currentCompanyName, comp))
                        {
                            return Json(new { success = false, message = "Kendi satışa koyduğunuz ürün için teklif talebinde bulunamazsınız." });
                        }

                        if (!string.IsNullOrWhiteSpace(item.ItemName))
                        {
                            prodName = item.ItemName;
                        }

                        // Tedarikçi firmayı tespit et
                        if (!string.IsNullOrWhiteSpace(item.Brand))
                        {
                            supplierCompany = await _context.Companies.AsNoTracking()
                                .FirstOrDefaultAsync(c => c.CompanyName != null && c.CompanyName.ToLower() == item.Brand.Trim().ToLower());
                        }
                    }
                }

                var notesList = new List<string>();
                if (!string.IsNullOrWhiteSpace(input.BuyerTax)) notesList.Add($"VKN/VD: {input.BuyerTax.Trim()}");
                if (!string.IsNullOrWhiteSpace(input.PaymentTerms)) notesList.Add($"Ödeme Koşulu: {input.PaymentTerms.Trim()}");
                if (!string.IsNullOrWhiteSpace(input.DeliveryCity)) notesList.Add($"Teslimat: {input.DeliveryCity.Trim()}");
                if (input.SampleRequested) notesList.Add("Numune Talebi: Evet");
                if (!string.IsNullOrWhiteSpace(input.Notes)) notesList.Add($"Açıklama: {input.Notes.Trim()}");

                string finalNotes = string.Join(" | ", notesList);

                var newRequest = new Requests
                {
                    RequestDate = DateTime.UtcNow,
                    SenderID = currentCompanyId > 0 ? currentCompanyId : null,
                    BuyerInfo = input.BuyerCompany.Trim(),
                    ProductName = prodName,
                    Quantity = input.Quantity,
                    RequestType = "B2B Vitrin RFQ",
                    EstimatedBudget = input.TargetPrice.HasValue ? input.TargetPrice.Value * input.Quantity : null,
                    TargetDeliveryDate = DateTime.UtcNow.AddDays(15),
                    Status = "Beklemede",
                    Notes = finalNotes
                };

                _context.Requests.Add(newRequest);
                await _context.SaveChangesAsync();

                string rfqCode = $"RFQ-{newRequest.RequestID:D4}";

                // Asenkron SignalR & E-posta Bildirimleri
                try
                {
                    var rfqPayload = new
                    {
                        requestId = newRequest.RequestID,
                        requestCode = rfqCode,
                        productName = prodName,
                        quantity = input.Quantity,
                        buyerInfo = input.BuyerCompany.Trim(),
                        targetPrice = input.TargetPrice,
                        targetPriceFormatted = input.TargetPrice.HasValue ? $"{input.TargetPrice.Value:N0} ₺" : "Belirtilmedi",
                        paymentTerms = input.PaymentTerms ?? "",
                        deliveryCity = input.DeliveryCity ?? "",
                        requestDate = DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm"),
                        notes = finalNotes
                    };

                    if (supplierCompany != null && supplierCompany.CompanyID > 0)
                    {
                        await _hubContext.Clients.Group($"company_{supplierCompany.CompanyID}").SendAsync("OnRfqCreated", rfqPayload);

                        // Asenkron e-posta bildirimi
                        if (!string.IsNullOrWhiteSpace(supplierCompany.CompanyMail))
                        {
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    string subject = $"📋 Yeni Teklif Talebi (RFQ) Geldi: {rfqCode} - {prodName}";
                                    string body = $@"<h3>Sayın {supplierCompany.CompanyName ?? supplierCompany.CompanyOfficial},</h3>
<p>Portaldaki ürününüz için yeni bir kurumsal teklif talebi (RFQ) oluşturuldu.</p>
<ul>
    <li><strong>Talep Kodu:</strong> {rfqCode}</li>
    <li><strong>Alıcı Firma:</strong> {input.BuyerCompany}</li>
    <li><strong>Ürün:</strong> {prodName}</li>
    <li><strong>İstenen Miktar:</strong> {input.Quantity} Adet</li>
    <li><strong>Hedef Fiyat:</strong> {(input.TargetPrice.HasValue ? input.TargetPrice.Value.ToString("N0") + " ₺" : "Belirtilmedi")}</li>
    <li><strong>Ödeme Koşulu:</strong> {input.PaymentTerms}</li>
    <li><strong>Teslimat İli:</strong> {input.DeliveryCity}</li>
</ul>
<p>Talebi incelemek ve teklif vermek için yönetim panelinize giriş yapabilirsiniz.</p>";
                                    await _emailService.SendEmailAsync(supplierCompany.CompanyMail, subject, body);
                                }
                                catch (Exception mailEx)
                                {
                                    Console.WriteLine($"RFQ Email Error: {mailEx.Message}");
                                }
                            });
                        }
                    }

                    await _hubContext.Clients.Group("role_admin").SendAsync("OnRfqCreated", rfqPayload);
                }
                catch (Exception hubEx)
                {
                    Console.WriteLine($"SignalR SubmitQuoteRequest error: {hubEx.Message}");
                }

                return Json(new
                {
                    success = true,
                    requestId = newRequest.RequestID,
                    rfqCode = rfqCode,
                    message = $"📋 Sayın {input.BuyerCompany}, {input.Quantity} adet için {rfqCode} numaralı resmi RFQ teklif talebiniz başarıyla iletildi."
                });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine($"SubmitQuoteRequest Error: {detail}");
                return Json(new { success = false, message = "Teklif talebi iletilirken hata oluştu: " + detail });
            }
        }

        /// <summary>
        /// Veritabanındaki kurumsal ilan ve ürünleri JSON formatında vitrin için listeler.
        /// Oturum açmış firma kendi satışa sunduğu ürünleri anasayfa vitrininde görmez.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetVitrinProductsJson()
        {
            try
            {
                var (currentCompanyId, currentCompanyName, comp) = await GetCurrentCompanyContextAsync();

                var allItems = await _context.Items
                    .Include(i => i.Images)
                    .AsNoTracking()
                    .Where(i => !i.IsDeleted)
                    .OrderByDescending(i => i.ItemID)
                    .Take(100)
                    .ToListAsync();

                // Tüm ürünleri listele (Kendi ürünlerini filtrelemeden göster, böylece satıcı eklediği ürünü görebilir)
                var items = allItems
                    .Take(50)
                    .ToList();

                var allDbCategories = await _context.Category.AsNoTracking().ToListAsync();
                var catDict = allDbCategories.ToDictionary(c => c.CategoryID, c => c);

                string GetBreadcrumbPath(int? cId)
                {
                    if (!cId.HasValue || !catDict.TryGetValue(cId.Value, out var current)) return "Endüstriyel Ürünler";
                    var parts = new List<string>();
                    var visited = new HashSet<int>();
                    var curr = current;
                    while (curr != null && visited.Add(curr.CategoryID))
                    {
                        parts.Insert(0, curr.CategoryName ?? ("Kategori #" + curr.CategoryID));
                        if (curr.ParentCategoryID.HasValue && curr.ParentCategoryID.Value > 0 && catDict.TryGetValue(curr.ParentCategoryID.Value, out var p))
                        {
                            curr = p;
                        }
                        else
                        {
                            break;
                        }
                    }
                    return string.Join(" > ", parts);
                }

                var allCompanies = await _context.Companies
                    .AsNoTracking()
                    .Select(c => new { c.CompanyID, c.CompanyName, c.CompanyOfficial, c.CompanyMail, c.AddressID })
                    .ToListAsync();

                // Tüm satıcı değerlendirmelerini çekip firma bazlı puan ve yorum sayılarını hesapla
                var allReviews = await _context.Reviews
                    .AsNoTracking()
                    .Where(r => r.ReviewScore.HasValue && r.CompanyID.HasValue && r.ReviewType == "BuyerToSeller")
                    .ToListAsync();

                var reviewStats = allReviews
                    .GroupBy(r => r.CompanyID!.Value)
                    .ToDictionary(
                        g => g.Key,
                        g => new
                        {
                            Count = g.Count(),
                            AverageScore = Math.Round(g.Average(r => r.ReviewScore!.Value), 1)
                        });

                var allAddresses = await _context.Addresses
                    .AsNoTracking()
                    .Select(a => new { a.AddressID, a.DistrictID })
                    .ToListAsync();

                var allDistricts = await _context.Districts
                    .AsNoTracking()
                    .Select(d => new { d.DistrictID, d.DistrictName, d.TownID })
                    .ToListAsync();

                var allTowns = await _context.Towns
                    .AsNoTracking()
                    .Select(t => new { t.TownID, t.TownName })
                    .ToListAsync();

                var list = items.Select(it =>
                {
                    string catName = it.CategoryID.HasValue && catDict.TryGetValue(it.CategoryID.Value, out var cN) ? (cN.CategoryName ?? "Endüstriyel Ürünler") : "Endüstriyel Ürünler";
                    string catPath = it.CategoryID.HasValue ? GetBreadcrumbPath(it.CategoryID.Value) : catName;
                    decimal price = (decimal)(it.ItemPrice ?? 0);

                    int sellerCompId = 1;
                    string sellerName = !string.IsNullOrWhiteSpace(it.Brand) ? it.Brand.Trim() : "Onaylı Tedarikçi";

                    if (!string.IsNullOrWhiteSpace(it.Properties))
                    {
                        var prop = it.Properties;
                        if (prop.Contains("FirmaID:") || prop.Contains("CompanyID:"))
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(prop, @"(?:FirmaID|CompanyID)\s*:\s*(\d+)");
                            if (match.Success && int.TryParse(match.Groups[1].Value, out int pid))
                            {
                                sellerCompId = pid;
                            }
                        }
                    }

                    if (sellerCompId == 1 && !string.IsNullOrWhiteSpace(it.Brand))
                    {
                        var matchedComp = allCompanies.FirstOrDefault(c =>
                            (!string.IsNullOrWhiteSpace(c.CompanyName) && string.Equals(c.CompanyName.Trim(), it.Brand.Trim(), StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(c.CompanyOfficial) && string.Equals(c.CompanyOfficial.Trim(), it.Brand.Trim(), StringComparison.OrdinalIgnoreCase)));
                        if (matchedComp != null)
                        {
                            sellerCompId = matchedComp.CompanyID;
                            sellerName = matchedComp.CompanyName ?? sellerName;
                        }
                    }

                    // Gerçek değerlendirme puanı ve yorum sayısı
                    double sellerScore = 10.0;
                    int sellerReviewCount = 0;
                    if (reviewStats.TryGetValue(sellerCompId, out var stats))
                    {
                        sellerScore = stats.AverageScore;
                        sellerReviewCount = stats.Count;
                    }

                    // Gerçek tedarikçi lokasyonunu çözümle
                    string sellerLocation = "Türkiye";
                    var compObj = allCompanies.FirstOrDefault(c => c.CompanyID == sellerCompId);
                    if (compObj != null && compObj.AddressID.HasValue)
                    {
                        var addrObj = allAddresses.FirstOrDefault(a => a.AddressID == compObj.AddressID.Value);
                        if (addrObj != null && addrObj.DistrictID.HasValue)
                        {
                            var distObj = allDistricts.FirstOrDefault(d => d.DistrictID == addrObj.DistrictID.Value);
                            if (distObj != null)
                            {
                                var townObj = distObj.TownID.HasValue ? allTowns.FirstOrDefault(t => t.TownID == distObj.TownID.Value) : null;
                                sellerLocation = townObj != null ? $"{townObj.TownName} / {distObj.DistrictName}" : distObj.DistrictName ?? "Türkiye";
                            }
                        }
                    }

                    int rawStock = it.Quantity ?? 0;
                    bool isOutOfStock = rawStock <= 0;
                    string stockText = isOutOfStock ? "Tükendi (Stokta Yok)" : $"{rawStock} Adet Hazır Stok";

                    bool isOwnProduct = IsProductOwnedByCompany(it, currentCompanyId, currentCompanyName, comp);

                    // Ürünün özel görsel URL'lerini çek (ItemImages tablosu veya Properties)
                    var itemImages = it.Images != null && it.Images.Any()
                        ? it.Images.OrderByDescending(x => x.IsMain).ThenBy(x => x.DisplayOrder).Select(x => x.ImageUrl).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
                        : new List<string>();

                    if (!itemImages.Any() && !string.IsNullOrWhiteSpace(it.Properties))
                    {
                        var imgMatch = System.Text.RegularExpressions.Regex.Match(it.Properties, @"(?:Resim|Image|ImageUrl|Gorsel)\s*:\s*(https?://[^\s\|;,]+|/[^\s\|;,]+)");
                        if (imgMatch.Success)
                        {
                            itemImages.Add(imgMatch.Groups[1].Value.Trim());
                        }
                    }

                    if (!itemImages.Any())
                    {
                        itemImages.Add("/images/default-product.svg");
                    }

                    string itemImage = itemImages.First();

                    return new
                    {
                        id = it.ItemID,
                        sku = $"SKU-B2B-{it.ItemID:D4}",
                        title = it.ItemName ?? "Kurumsal Ürün",
                        brand = !string.IsNullOrWhiteSpace(it.Brand) ? it.Brand : sellerName,
                        seller = sellerName,
                        sellerCompanyId = sellerCompId,
                        sellerScore = sellerScore,
                        sellerReviewCount = sellerReviewCount,
                        sellerBadge = sellerReviewCount > 0 ? $"Onaylı Satıcı ({sellerReviewCount} Değerlendirme)" : "Yeni Onaylı Satıcı",
                        sellerLocation = sellerLocation,
                        categoryId = it.CategoryID ?? 0,
                        categoryName = catName,
                        categoryPath = catPath,
                        category = catName.ToLowerInvariant().Contains("metal") ? "metal" : 
                                   catName.ToLowerInvariant().Contains("it") || catName.ToLowerInvariant().Contains("bilişim") ? "it" :
                                   catName.ToLowerInvariant().Contains("kimya") ? "chemical" : "hardware",
                        price = price,
                        oldPrice = Math.Round(price * 1.15m, 0),
                        discount = 15,
                        moq = "1 Adet",
                        unit = "Adet",
                        taxText = "+ %20 KDV",
                        stock = rawStock,
                        isOutOfStock = isOutOfStock,
                        stockStatus = stockText,
                        leadTime = "1-2 İş Günü",
                        tierPricing = $"1-4 Adet: {price:N0} ₺ | 5+ Adet: {Math.Round(price * 0.92m, 0):N0} ₺",
                        image = itemImage,
                        images = itemImages,
                        badge = "Doğrudan Üreticiden",
                        badgeType = "manufacturer",
                        isOwnProduct = isOwnProduct
                    };
                }).ToList();

                return Json(new { success = true, products = list });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Bağımsız Ürün Detay Sayfası (Product Detail Page - PDP).
        /// Doğrudan ürün ID'si ile rota eşleşir: /Product/{id}
        /// </summary>
        [HttpGet("/Product/{id:int}")]
        [HttpGet("/Home/ProductDetail/{id:int}")]
        public async Task<IActionResult> ProductDetail(int id)
        {
            if (id <= 0) return RedirectToAction("Index", "Home");

            var item = await _context.Items
                .Include(i => i.Images)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.ItemID == id);
            if (item == null || item.IsDeleted)
            {
                TempData["ErrorMessage"] = "Aradığınız ürün bulunamadı veya satıştan kaldırılmış olabilir.";
                return RedirectToAction("Index", "Home");
            }

            var (currentCompanyId, currentCompanyName, comp) = await GetCurrentCompanyContextAsync();
            bool isOwner = IsProductOwnedByCompany(item, currentCompanyId, currentCompanyName, comp);

            // Kategori Bilgisi
            string categoryName = "Endüstriyel Ürünler";
            if (item.CategoryID.HasValue && item.CategoryID.Value > 0)
            {
                var cat = await _context.Category.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryID == item.CategoryID.Value);
                if (cat != null && !string.IsNullOrWhiteSpace(cat.CategoryName))
                {
                    categoryName = cat.CategoryName;
                }
            }

            // Tedarikçi Firma ve Özellik Çözümleme
            int supplierCompanyId = 1;
            string supplierName = !string.IsNullOrWhiteSpace(item.Brand) ? item.Brand.Trim() : "Onaylı Tedarikçi";
            string supplierOfficial = "";
            string supplierLocation = "Türkiye";
            string imageUrl = "/images/default-product.svg";
            var specs = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(item.Properties))
            {
                var prop = item.Properties;
                if (prop.Contains("FirmaID:") || prop.Contains("CompanyID:"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(prop, @"(?:FirmaID|CompanyID)\s*:\s*(\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int pid))
                    {
                        supplierCompanyId = pid;
                    }
                }

                var imgMatch = System.Text.RegularExpressions.Regex.Match(prop, @"(?:Resim|Image|ImageUrl|Gorsel)\s*:\s*(https?://[^\s\|;,]+|/[^\s\|;,]+)");
                if (imgMatch.Success)
                {
                    imageUrl = imgMatch.Groups[1].Value.Trim();
                }

                // Özellikleri parçala (key:value)
                var rawPairs = prop.Split(new[] { ';', '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var pair in rawPairs)
                {
                    var parts = pair.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        string k = parts[0].Trim();
                        string v = parts[1].Trim();
                        if (!string.IsNullOrWhiteSpace(k) && !string.IsNullOrWhiteSpace(v))
                        {
                            if (!k.Equals("FirmaID", StringComparison.OrdinalIgnoreCase) &&
                                !k.Equals("CompanyID", StringComparison.OrdinalIgnoreCase) &&
                                !k.Equals("Resim", StringComparison.OrdinalIgnoreCase) &&
                                !k.Equals("Image", StringComparison.OrdinalIgnoreCase) &&
                                !k.Equals("ImageUrl", StringComparison.OrdinalIgnoreCase) &&
                                !k.Equals("Gorsel", StringComparison.OrdinalIgnoreCase))
                            {
                                specs[k] = v;
                            }
                        }
                    }
                }
            }

            // Firma nesnesini getir
            Company? supplierComp = null;
            if (supplierCompanyId > 1)
            {
                supplierComp = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == supplierCompanyId);
            }
            if (supplierComp == null && !string.IsNullOrWhiteSpace(item.Brand))
            {
                supplierComp = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c =>
                    (!string.IsNullOrWhiteSpace(c.CompanyName) && c.CompanyName.ToLower() == item.Brand.Trim().ToLower()) ||
                    (!string.IsNullOrWhiteSpace(c.CompanyOfficial) && c.CompanyOfficial.ToLower() == item.Brand.Trim().ToLower()));
            }

            if (supplierComp != null)
            {
                supplierCompanyId = supplierComp.CompanyID;
                supplierName = supplierComp.CompanyName ?? supplierName;
                supplierOfficial = supplierComp.CompanyOfficial ?? "";

                if (supplierComp.AddressID.HasValue)
                {
                    var addrObj = await _context.Addresses.AsNoTracking().FirstOrDefaultAsync(a => a.AddressID == supplierComp.AddressID.Value);
                    if (addrObj != null && addrObj.DistrictID.HasValue)
                    {
                        var distObj = await _context.Districts.AsNoTracking().FirstOrDefaultAsync(d => d.DistrictID == addrObj.DistrictID.Value);
                        if (distObj != null)
                        {
                            var townObj = distObj.TownID.HasValue ? await _context.Towns.AsNoTracking().FirstOrDefaultAsync(t => t.TownID == distObj.TownID.Value) : null;
                            supplierLocation = townObj != null ? $"{townObj.TownName} / {distObj.DistrictName}" : distObj.DistrictName ?? "Türkiye";
                        }
                    }
                }
            }

            // Tedarikçi Değerlendirme Puanı
            double supplierScore = 9.8;
            int supplierReviewCount = 0;
            var reviews = await _context.Reviews
                .AsNoTracking()
                .Where(r => r.ReviewScore.HasValue && r.CompanyID == supplierCompanyId && r.ReviewType == "BuyerToSeller")
                .ToListAsync();

            if (reviews.Any())
            {
                supplierScore = Math.Round(reviews.Average(r => r.ReviewScore!.Value), 1);
                supplierReviewCount = reviews.Count;
            }

            // Benzer / İlgili Ürünler (Aynı kategoriden veya diğer ürünlerden 4 adet)
            var relatedDbItems = await _context.Items
                .AsNoTracking()
                .Where(i => !i.IsDeleted && i.ItemID != item.ItemID && (item.CategoryID == null || i.CategoryID == item.CategoryID))
                .OrderByDescending(i => i.ItemID)
                .Take(4)
                .ToListAsync();

            if (relatedDbItems.Count < 4)
            {
                var fallbackItems = await _context.Items
                    .AsNoTracking()
                    .Where(i => !i.IsDeleted && i.ItemID != item.ItemID && !relatedDbItems.Select(r => r.ItemID).Contains(i.ItemID))
                    .OrderByDescending(i => i.ItemID)
                    .Take(4 - relatedDbItems.Count)
                    .ToListAsync();
                relatedDbItems.AddRange(fallbackItems);
            }

            var relatedList = relatedDbItems.Select(r =>
            {
                string rImg = "/images/default-product.svg";
                if (!string.IsNullOrWhiteSpace(r.Properties))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(r.Properties, @"(?:Resim|Image|ImageUrl|Gorsel)\s*:\s*(https?://[^\s\|;,]+|/[^\s\|;,]+)");
                    if (m.Success) rImg = m.Groups[1].Value.Trim();
                }

                return new RelatedProductCardDto
                {
                    ItemID = r.ItemID,
                    Title = r.ItemName ?? "Endüstriyel Ürün",
                    Brand = !string.IsNullOrWhiteSpace(r.Brand) ? r.Brand : "Tedarikçi",
                    UnitPrice = (decimal)(r.ItemPrice ?? 0),
                    ImageUrl = rImg,
                    StockQuantity = r.Quantity ?? 0
                };
            }).ToList();

            var pdpImages = item.Images != null && item.Images.Any()
                ? item.Images.OrderByDescending(x => x.IsMain).ThenBy(x => x.DisplayOrder).Select(x => new ProductImageDto
                {
                    ImageID = x.ImageID,
                    ImageUrl = x.ImageUrl,
                    PublicId = x.PublicId,
                    IsMain = x.IsMain,
                    DisplayOrder = x.DisplayOrder
                }).ToList()
                : new List<ProductImageDto>();

            if (!pdpImages.Any())
            {
                pdpImages.Add(new ProductImageDto
                {
                    ImageUrl = !string.IsNullOrWhiteSpace(imageUrl) ? imageUrl : "/images/default-product.svg",
                    IsMain = true,
                    DisplayOrder = 0
                });
            }

            string mainPdpImg = pdpImages.FirstOrDefault(x => x.IsMain)?.ImageUrl ?? pdpImages.First().ImageUrl;

            var viewModel = new ProductDetailPageViewModel
            {
                ItemID = item.ItemID,
                Title = item.ItemName ?? "Endüstriyel Malzeme",
                Sku = !string.IsNullOrWhiteSpace(item.ItemDescryption) && item.ItemDescryption.StartsWith("SKU-") ? item.ItemDescryption : $"SKU-{item.ItemID:D5}",
                Brand = supplierName,
                UnitPrice = (decimal)(item.ItemPrice ?? 0),
                StockQuantity = item.Quantity ?? 0,
                CategoryName = categoryName,
                Description = item.ItemDescryption ?? $"{item.ItemName} endüstriyel standartlara ve fabrika normlarına uygun olarak üretilmiş olup kurumsal satınalma ve tedarik süreçleriniz için kullanıma hazırdır.",
                ImageUrl = mainPdpImg,
                Images = pdpImages,
                Specifications = specs,
                SupplierCompanyID = supplierCompanyId,
                SupplierCompanyName = supplierName,
                SupplierOfficial = supplierOfficial,
                SupplierLocation = supplierLocation,
                SupplierScore = supplierScore,
                SupplierReviewCount = supplierReviewCount,
                IsOwner = isOwner,
                RelatedProducts = relatedList
            };

            ViewData["Title"] = $"{viewModel.Title} - Tedarik Portalı";
            return View(viewModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class HierarchicalCategoryItem {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int? ParentCategoryID { get; set; }
        public string? ParentCategoryName { get; set; }
        public string FormattedName { get; set; } = string.Empty;
        public string BreadcrumbPath { get; set; } = string.Empty;
        public int Level { get; set; }
        public bool HasChildren { get; set; }
        public List<HierarchicalCategoryItem> Children { get; set; } = new();
    }

    public class CreateProductListingInputModel {
        public string ProductName { get; set; } = string.Empty;
        public int? CategoryID { get; set; }
        public string? CategoryName { get; set; }
        public decimal UnitPrice { get; set; }
        public string? StockQuantity { get; set; }
        public string? ItemDescription { get; set; }
        public string? Brand { get; set; }
        public string? Properties { get; set; }
        public string? ImageUrl { get; set; }
        public List<IFormFile>? ImageFiles { get; set; }
        public int MainImageIndex { get; set; } = 0;
        public List<ProductImageInputModel>? Images { get; set; }
    }

    public class ProductImageInputModel {
        public string ImageUrl { get; set; } = string.Empty;
        public string? PublicId { get; set; }
        public bool IsMain { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class SubmitVitrinQuoteInputModel {
        public int? ProductID { get; set; }
        public string BuyerCompany { get; set; } = string.Empty;
        public string? BuyerTax { get; set; }
        public int Quantity { get; set; }
        public decimal? TargetPrice { get; set; }
        public string? PaymentTerms { get; set; }
        public string? DeliveryCity { get; set; }
        public string? Notes { get; set; }
        public bool SampleRequested { get; set; }
    }
}
