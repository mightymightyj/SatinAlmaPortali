using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using satinalmaportali2.Data;
using satinalmaportali2.Models;
using satinalmaportali2.Services;
using System.Diagnostics;
using System.Security.Claims;

namespace satinalmaportali2.Controllers {
    public class HomeController : Controller {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public HomeController(ApplicationDbContext context, EmailService emailService) {
            _context = context;
            _emailService = emailService;
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

                ViewBag.CategoryTree = tree;
                ViewBag.HierarchicalCategories = flatDropdownList;
                ViewBag.Categories = dbCategories;
            }
            catch (Exception ex) {
                Console.WriteLine("HomeController Index Category Load Error: " + ex.Message);
                ViewBag.CategoryTree = new List<HierarchicalCategoryItem>();
                ViewBag.HierarchicalCategories = new List<HierarchicalCategoryItem>();
                ViewBag.Categories = new List<Category>();
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

        public IActionResult Fiyatlandirma() {
            return View();
        }

        public IActionResult MedyaDuyurular() {
            return View();
        }

        public IActionResult Iletisim() {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> IletisimSubmit(string adSoyad, string eposta, string telefon, string konu, string mesaj) {
            if (string.IsNullOrWhiteSpace(adSoyad) || string.IsNullOrWhiteSpace(eposta) || string.IsNullOrWhiteSpace(mesaj)) {
                TempData["IletisimError"] = "Lütfen zorunlu alanları (Ad Soyad, E-posta, Mesaj) eksiksiz doldurunuz.";
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

                TempData["IletisimSuccess"] = "Mesajınız başarıyla sistemimize kaydedildi. En kısa sürede sizinle iletişime geçeceğiz.";
            }
            catch (Exception ex) {
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine($"IletisimSubmit Error: {innerMsg}");
                TempData["IletisimError"] = "Mesajınız gönderilirken bir hata oluştu. Lütfen daha sonra tekrar deneyiniz.";
            }

            return RedirectToAction("Iletisim");
        }

        /// <summary>
        /// Tedarikçi olarak kurumsal ürün ilanı ve toptan satış listeleme endpoint'i (AJAX)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SubmitSellerOffer([FromBody] CreateProductListingInputModel input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.ProductName) || input.UnitPrice <= 0)
            {
                return Json(new { success = false, message = "Lütfen ürün adını ve geçerli bir birim fiyatı eksiksiz doldurunuz." });
            }

            try
            {
                // 1. Oturum açmış firmanın bilgilerini tespit et
                string? currentCompanyName = User.FindFirst("CompanyName")?.Value;
                string? idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("CompanyID");
                int currentCompanyId = 0;
                if (!string.IsNullOrEmpty(idClaim) && int.TryParse(idClaim, out int cid))
                {
                    currentCompanyId = cid;
                }

                if (currentCompanyId > 0 && string.IsNullOrWhiteSpace(currentCompanyName))
                {
                    var comp = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == currentCompanyId);
                    currentCompanyName = comp?.CompanyName ?? comp?.CompanyOfficial;
                }

                string finalBrand = !string.IsNullOrWhiteSpace(input.Brand) 
                    ? input.Brand.Trim() 
                    : (!string.IsNullOrWhiteSpace(currentCompanyName) ? currentCompanyName.Trim() : "Onaylı Tedarikçi");

                // 2. Kategori ID Çözümleme
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

                // 4. Veritabanına (items tablosu) yeni ürün kaydını ekle
                var newItem = new ItemsModel
                {
                    ItemName = input.ProductName.Trim(),
                    ItemPrice = (double)input.UnitPrice,
                    Quantity = parsedQty,
                    CategoryID = finalCatId,
                    Brand = finalBrand,
                    ItemDescryption = !string.IsNullOrWhiteSpace(input.ItemDescription) ? input.ItemDescription.Trim() : null,
                    Properties = !string.IsNullOrWhiteSpace(input.Properties) ? input.Properties.Trim() : $"Kategori: {finalCatName}"
                };

                _context.Items.Add(newItem);
                await _context.SaveChangesAsync();

                string formattedSku = $"SKU-B2B-{newItem.ItemID:D4}";

                return Json(new
                {
                    success = true,
                    message = $"🏢 \"{newItem.ItemName}\" ürün ilanınız [{finalCatName}] kategorisinde başarıyla yayına alındı.",
                    item = new
                    {
                        id = newItem.ItemID,
                        sku = formattedSku,
                        title = newItem.ItemName,
                        brand = finalBrand,
                        seller = finalBrand,
                        sellerScore = 9.9,
                        sellerBadge = "Yeni İlan / Onaylı",
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
                        image = !string.IsNullOrWhiteSpace(input.ImageUrl) 
                            ? input.ImageUrl 
                            : "https://images.unsplash.com/photo-1581091226825-a6a2a5aee158?w=500&auto=format&fit=crop&q=80",
                        badge = "Doğrudan Üreticiden",
                        badgeType = "manufacturer"
                    }
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
                string? idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("CompanyID");
                int currentCompanyId = 0;
                if (!string.IsNullOrEmpty(idClaim) && int.TryParse(idClaim, out int cid))
                {
                    currentCompanyId = cid;
                }

                if (currentCompanyId > 0)
                {
                    var compExists = await _context.Companies.AnyAsync(c => c.CompanyID == currentCompanyId);
                    if (!compExists)
                    {
                        currentCompanyId = 0;
                    }
                }

                string prodName = "Kurumsal Vitrin Ürünü";
                if (input.ProductID.HasValue && input.ProductID.Value > 0)
                {
                    var item = await _context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.ItemID == input.ProductID.Value);
                    if (item != null && !string.IsNullOrWhiteSpace(item.ItemName))
                    {
                        prodName = item.ItemName;
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
        /// Veritabanındaki kurumsal ilan ve ürünleri JSON formatında vitrin için listeler
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetVitrinProductsJson()
        {
            try
            {
                var items = await _context.Items
                    .AsNoTracking()
                    .OrderByDescending(i => i.ItemID)
                    .Take(50)
                    .ToListAsync();

                var catIds = items.Where(i => i.CategoryID.HasValue).Select(i => i.CategoryID!.Value).Distinct().ToList();
                var catMap = await _context.Category
                    .Where(c => catIds.Contains(c.CategoryID))
                    .ToDictionaryAsync(c => c.CategoryID, c => c.CategoryName ?? "Genel");

                var list = items.Select(it =>
                {
                    string catName = it.CategoryID.HasValue && catMap.TryGetValue(it.CategoryID.Value, out var cN) ? cN : "Endüstriyel Ürünler";
                    decimal price = (decimal)(it.ItemPrice ?? 0);
                    return new
                    {
                        id = it.ItemID,
                        sku = $"SKU-B2B-{it.ItemID:D4}",
                        title = it.ItemName ?? "Kurumsal Ürün",
                        brand = !string.IsNullOrWhiteSpace(it.Brand) ? it.Brand : "Tedarikçi Portalı",
                        seller = !string.IsNullOrWhiteSpace(it.Brand) ? it.Brand : "Onaylı Tedarikçi",
                        sellerScore = 9.8,
                        sellerBadge = "Onaylı Tedarikçi",
                        sellerLocation = "Türkiye",
                        category = catName.ToLowerInvariant().Contains("metal") ? "metal" : 
                                   catName.ToLowerInvariant().Contains("it") || catName.ToLowerInvariant().Contains("bilişim") ? "it" :
                                   catName.ToLowerInvariant().Contains("kimya") ? "chemical" : "hardware",
                        categoryName = catName,
                        price = price,
                        oldPrice = Math.Round(price * 1.15m, 0),
                        discount = 15,
                        moq = "1 Adet",
                        unit = "Adet",
                        taxText = "+ %20 KDV",
                        stockStatus = $"{it.Quantity ?? 50} Adet Hazır Stok",
                        leadTime = "1-2 İş Günü",
                        tierPricing = $"1-4 Adet: {price:N0} ₺ | 5+ Adet: {Math.Round(price * 0.92m, 0):N0} ₺",
                        image = "https://images.unsplash.com/photo-1581091226825-a6a2a5aee158?w=500&auto=format&fit=crop&q=80",
                        badge = "Doğrudan Üreticiden",
                        badgeType = "manufacturer"
                    };
                }).ToList();

                return Json(new { success = true, products = list });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
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
}
