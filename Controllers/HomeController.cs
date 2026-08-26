using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using satinalmaportali2.Data;
using satinalmaportali2.Hubs;
using satinalmaportali2.Models;
using satinalmaportali2.Services;
using System.Diagnostics;
using System.Security.Claims;

namespace satinalmaportali2.Controllers {
    public class HomeController : Controller {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly IHubContext<PortalHub> _hubContext;

        public HomeController(ApplicationDbContext context, EmailService emailService, IHubContext<PortalHub> hubContext) {
            _context = context;
            _emailService = emailService;
            _hubContext = hubContext;
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
                var openRequests = await _context.Requests.CountAsync(r => r.Status == "Açık" || r.Status == "Beklemede" || r.Status == "İncelemede" || string.IsNullOrEmpty(r.Status));
                var completedOffers = await _context.RequestOffers.CountAsync(o => o.Status == "Kabul Edildi" || o.Status == "Onaylandı");
                var totalItems = await _context.Items.CountAsync();

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
        public async Task<IActionResult> SubmitSellerOffer([FromBody] CreateProductListingInputModel input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.ProductName) || input.UnitPrice <= 0)
            {
                return Json(new { success = false, message = "Lütfen ürün adını ve geçerli bir birim fiyat giriniz." });
            }

            try
            {
                // 1. Oturum açmış firmanın bilgilerini tespit et
                var (currentCompanyId, currentCompanyName, comp) = await GetCurrentCompanyContextAsync();

                string finalBrand = !string.IsNullOrWhiteSpace(currentCompanyName) 
                    ? currentCompanyName.Trim() 
                    : (!string.IsNullOrWhiteSpace(input.Brand) ? input.Brand.Trim() : "Onaylı Tedarikçi");

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
                await _context.SaveChangesAsync();

                string formattedSku = $"SKU-B2B-{newItem.ItemID:D4}";

                var productPayload = new
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
                    .AsNoTracking()
                    .OrderByDescending(i => i.ItemID)
                    .Take(100)
                    .ToListAsync();

                // Kendi ürünlerini filtrele (Anasayfada kendi ürünlerini görmesin)
                var items = allItems
                    .Where(it => !IsProductOwnedByCompany(it, currentCompanyId, currentCompanyName, comp))
                    .Take(50)
                    .ToList();

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
