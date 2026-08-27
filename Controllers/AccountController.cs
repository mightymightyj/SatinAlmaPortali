using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using satinalmaportali2.Data;
using satinalmaportali2.Helpers;
using satinalmaportali2.Models;
using satinalmaportali2.Services;
using System.Security.Claims;

namespace satinalmaportali2.Controllers {
    public class AccountController : Controller {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly ICloudinaryService _cloudinaryService;

        public AccountController(ApplicationDbContext context, EmailService emailService, ICloudinaryService cloudinaryService) {
            _context = context;
            _emailService = emailService;
            _cloudinaryService = cloudinaryService;
        }

        [HttpGet]
        public IActionResult Login(string tab = "user-login") {
            ViewData["ActiveTab"] = tab;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string userType) {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) {
                ViewData["ErrorMessage"] = "Lütfen e-posta adresinizi ve şifrenizi giriniz!";
                ViewData["ActiveTab"] = userType == "admin" ? "admin-login" : "user-login";
                return View();
            }

            try {
                // Query database via EF Core (case-insensitive email matching)
                var emailTrimmed = email.Trim().ToLower();
                var dbUser = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyMail != null && c.CompanyMail.ToLower() == emailTrimmed);

                // If not found directly, check with Punycode / IDN conversion (e.g. mehmet@yerlikayaa.com vs mehmet@xn--yerlikayaa-n9b.com)
                if (dbUser == null) {
                    try {
                        var parts = emailTrimmed.Split('@');
                        if (parts.Length == 2) {
                            var idn = new System.Globalization.IdnMapping();
                            var asciiDomain = idn.GetAscii(parts[1]);
                            var asciiEmail = $"{parts[0]}@{asciiDomain}".ToLower();
                            dbUser = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyMail != null && c.CompanyMail.ToLower() == asciiEmail);

                            if (dbUser == null) {
                                var unicodeDomain = idn.GetUnicode(parts[1]);
                                var unicodeEmail = $"{parts[0]}@{unicodeDomain}".ToLower();
                                dbUser = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyMail != null && c.CompanyMail.ToLower() == unicodeEmail);
                            }
                        }
                    } catch { }
                }

                // User not found in database
                if (dbUser == null) {
                    ViewData["ErrorMessage"] = "Girdiğiniz e-posta adresi ile kayıtlı bir hesap bulunamadı!";
                    ViewData["ActiveTab"] = userType == "admin" ? "admin-login" : "user-login";
                    return View();
                }

                // Verify password against stored hash (supporting salted, raw sha256, hex, plaintext, etc.)
                if (!PasswordHasher.VerifyPassword(password, dbUser.CompanyPassword)) {
                    ViewData["ErrorMessage"] = "Girdiğiniz şifre hatalı!";
                    ViewData["ActiveTab"] = userType == "admin" ? "admin-login" : "user-login";
                    return View();
                }

                // Auto-upgrade legacy or plaintext passwords to standard Salted SHA-256
                var standardSaltedHash = PasswordHasher.HashPassword(password.Trim());
                if (dbUser.CompanyPassword != standardSaltedHash) {
                    try {
                        dbUser.CompanyPassword = standardSaltedHash;
                        await _context.SaveChangesAsync();
                    } catch { }
                }

                string companyName = dbUser.CompanyName ?? dbUser.CompanyOfficial ?? email.Split('@')[0];
                string displayName = dbUser.CompanyOfficial ?? dbUser.CompanyName ?? email.Split('@')[0];

                bool isSuperAdminUser = dbUser.CompanyTypeID == 4 || string.Equals(dbUser.CompanyMail, "admin@tedarikportal.com", StringComparison.OrdinalIgnoreCase);
                string effectiveUserType = isSuperAdminUser ? "admin" : (userType ?? "User");
                int effectiveCompanyTypeId = isSuperAdminUser ? 4 : 3;

                var claims = new List<Claim> {
                    new Claim(ClaimTypes.NameIdentifier, dbUser.CompanyID.ToString()),
                    new Claim("CompanyID", dbUser.CompanyID.ToString()),
                    new Claim(ClaimTypes.Name, displayName),
                    new Claim(ClaimTypes.Email, email),
                    new Claim("CompanyName", companyName),
                    new Claim("UserType", effectiveUserType),
                    new Claim(ClaimTypes.Role, effectiveUserType),
                    new Claim("CompanyTypeID", effectiveCompanyTypeId.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties { IsPersistent = true };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                
                TempData["LoginSuccess"] = "Başarıyla giriş yapıldı!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex) {
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                ViewData["ErrorMessage"] = $"Veritabanı bağlantı hatası: {innerMsg}";
                ViewData["ActiveTab"] = userType == "admin" ? "admin-login" : "user-login";
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestDb() {
            try {
                await _context.Database.OpenConnectionAsync();
                
                var tables = new List<string>();
                using (var cmd = _context.Database.GetDbConnection().CreateCommand()) {
                    cmd.CommandText = "SELECT table_schema || '.' || table_name FROM information_schema.tables WHERE table_schema NOT IN ('pg_catalog', 'information_schema') AND table_type = 'BASE TABLE'";
                    using (var reader = await cmd.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            tables.Add(reader.GetString(0));
                        }
                    }
                }

                var columns = new List<string>();
                using (var cmd = _context.Database.GetDbConnection().CreateCommand()) {
                    cmd.CommandText = "SELECT table_name || '.' || column_name || ' (' || data_type || ')' FROM information_schema.columns WHERE table_schema = 'public' AND table_name ILIKE '%compan%'";
                    using (var reader = await cmd.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            columns.Add(reader.GetString(0));
                        }
                    }
                }

                int count = await _context.Companies.CountAsync();
                var companyList = await _context.Companies.Select(c => new { c.CompanyID, c.CompanyMail, c.CompanyName, c.CompanyTypeID }).Take(15).ToListAsync();
                var listText = string.Join("\n", companyList.Select(c => $"ID: {c.CompanyID} | Mail: {c.CompanyMail} | Name: {c.CompanyName} | Type: {c.CompanyTypeID}"));
                return Content($"VERİTABANI BAĞLANTISI BAŞARILI!\nKaydolmuş Firma Sayısı: {count}\n\nFirmalar:\n{listText}\n\nTablolar:\n{string.Join("\n", tables)}\n\nCompany Kolonları:\n{string.Join("\n", columns)}");
            }
            catch (Exception ex) {
                var inner = ex.InnerException?.Message ?? "";
                return Content($"VERİTABANI HATASI:\n{ex.Message}\n{inner}\n\nSTACKTRACE:\n{ex.StackTrace}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> SeedDb() {
            try {
                await DbInitializer.SeedDataAsync(_context);
                int count = await _context.Companies.CountAsync();
                return Content($"VERİLER BAŞARIYLA VERİTABANINA AKTARILDI!\nToplam Kayıtlı Firma Sayısı: {count}\n\nÖrnek Giriş Hesapları (SHA256 Hash'li):\n- Admin: admin@tedarikportal.com (Şifre: Admin123!@#)\n- Tedarikçi 1: mehmet@kayasanayi.com (Şifre: Kaya12345!)\n- Tedarikçi 2: ayse@demirlojistik.com (Şifre: Demir12345!)\n- Alıcı: ali@yilmazholding.com (Şifre: Yilmaz12345!)");
            }
            catch (Exception ex) {
                return Content($"SEEDING HATASI: {ex.Message}\n{ex.InnerException?.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> RegisterSubmit(
            string fullname, string email, string password, string confirmPassword,
            string country, string city, string district, string addressText,
            string tcno, string companyPhone, string taxOffice, string companyDetailText) {

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || password != confirmPassword) {
                ViewData["ErrorMessage"] = "Şifreler uyuşmuyor veya e-posta adresi geçersiz!";
                return View("Register");
            }

            // Strong Password Firewall Validation
            if (!PasswordHasher.IsStrongPassword(password, out string passError)) {
                ViewData["ErrorMessage"] = passError;
                return View("Register");
            }

            try {
                // Ensure connection is open
                await _context.Database.OpenConnectionAsync();

                // Check if user already exists
                var emailTrimmed = email.Trim().ToLower();
                var existingUser = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyMail != null && c.CompanyMail.ToLower() == emailTrimmed);
                if (existingUser != null) {
                    ViewData["ErrorMessage"] = "Bu e-posta adresi ile zaten kayıtlı bir hesap bulunmaktadır!";
                    return View("Register");
                }

                // ====================================
                // STEP 1: Lookup DistrictID & Save Address to Addresses table
                // ====================================
                int? districtId = null;
                if (!string.IsNullOrWhiteSpace(district)) {
                    var matchedDistrict = await _context.Districts
                        .FirstOrDefaultAsync(d => d.DistrictName == district);
                    if (matchedDistrict != null) {
                        districtId = matchedDistrict.DistrictID;
                    }
                }

                string fullAddress = $"{addressText}, {district} / {city} / {country}";

                var newAddress = new Addresses {
                    AddressText = fullAddress,
                    DistrictID = districtId
                };

                _context.Addresses.Add(newAddress);
                await _context.SaveChangesAsync();

                // ====================================
                // STEP 2: Save Company with AddressID FK (CompanyTypeID = 3: Hem Alıcı Hem Tedarikçi)
                // ====================================
                var newCompany = new Company {
                    CompanyName = fullname,
                    CompanyOfficial = fullname,
                    CompanyMail = email,
                    CompanyPassword = PasswordHasher.HashPassword(password),
                    CompanyTypeID = 3, // Otomatik olarak Hem Alıcı Hem Tedarikçi
                    MembershipTypeID = 1,
                    TCNO = tcno,
                    CompanyPhone = companyPhone,
                    TaxOffice = taxOffice,
                    CompanyDetailText = string.IsNullOrWhiteSpace(companyDetailText) ? null : companyDetailText,
                    AddressID = newAddress.AddressID
                };

                _context.Companies.Add(newCompany);
                await _context.SaveChangesAsync();

                // ====================================
                // STEP 3: Link Address back to Company
                // ====================================
                newAddress.CompanyID = newCompany.CompanyID;
                await _context.SaveChangesAsync();

                // Authenticate registered user immediately
                var claims = new List<Claim> {
                    new Claim(ClaimTypes.NameIdentifier, newCompany.CompanyID.ToString()),
                    new Claim("CompanyID", newCompany.CompanyID.ToString()),
                    new Claim(ClaimTypes.Name, fullname ?? email),
                    new Claim(ClaimTypes.Email, email),
                    new Claim("CompanyName", fullname ?? email),
                    new Claim("UserType", "User"),
                    new Claim(ClaimTypes.Role, "User"),
                    new Claim("CompanyTypeID", "3")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                TempData["RegisterSuccess"] = "Hesabınız başarıyla oluşturuldu ve veritabanına kaydedildi!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex) {
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                ViewData["ErrorMessage"] = $"Veritabanına kayıt hatası: {innerMsg}";
                return View("Register");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logout() {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private async Task<Company?> GetCurrentUserCompanyAsync()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("CompanyID");
            if (int.TryParse(idClaim, out int companyId) && companyId > 0)
            {
                var byId = await _context.Companies.FindAsync(companyId);
                if (byId != null)
                {
                    return byId;
                }
            }

            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (!string.IsNullOrWhiteSpace(email))
            {
                var emailTrimmed = email.Trim().ToLower();
                var byEmail = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyMail != null && c.CompanyMail.ToLower() == emailTrimmed);
                if (byEmail != null)
                {
                    return byEmail;
                }

                try
                {
                    var parts = emailTrimmed.Split('@');
                    if (parts.Length == 2)
                    {
                        var idn = new System.Globalization.IdnMapping();
                        var asciiDomain = idn.GetAscii(parts[1]);
                        var asciiEmail = $"{parts[0]}@{asciiDomain}".ToLower();
                        byEmail = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyMail != null && c.CompanyMail.ToLower() == asciiEmail);
                        if (byEmail != null)
                        {
                            return byEmail;
                        }

                        var unicodeDomain = idn.GetUnicode(parts[1]);
                        var unicodeEmail = $"{parts[0]}@{unicodeDomain}".ToLower();
                        byEmail = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyMail != null && c.CompanyMail.ToLower() == unicodeEmail);
                        if (byEmail != null)
                        {
                            return byEmail;
                        }
                    }
                }
                catch { }
            }

            return null;
        }

        [HttpGet]
        public async Task<IActionResult> Profile([FromQuery] string? tab = "profile") {
            var company = await GetCurrentUserCompanyAsync();
            if (company == null) {
                if (User.Identity?.IsAuthenticated != true) {
                    return RedirectToAction("Login", new { returnUrl = "/Account/Profile" });
                }
                TempData["ErrorMessage"] = "Hesap bilgileri veritabanında bulunamadı. Lütfen tekrar giriş yapınız.";
                return RedirectToAction("Login");
            }

            // Build ProfileViewModel with related data
            var viewModel = new ProfileViewModel {
                CompanyID = company.CompanyID,
                CompanyName = company.CompanyName,
                CompanyOfficial = company.CompanyOfficial,
                CompanyMail = company.CompanyMail,
                CompanyPhone = company.CompanyPhone,
                TCNO = company.TCNO,
                TaxOffice = company.TaxOffice,
                CompanyDetailText = company.CompanyDetailText,
                CompanyLogo = company.CompanyLogo,
                CompanyTypeID = company.CompanyTypeID,
                CompanyTypeName = "Kurumsal Hesap (Alıcı & Tedarikçi)",
                MembershipTypeID = company.MembershipTypeID,
                AddressID = company.AddressID,
                ActiveTab = !string.IsNullOrWhiteSpace(tab) ? tab.Trim().ToLower() : "profile"
            };

            // Load MembershipType name
            if (company.MembershipTypeID.HasValue) {
                var membershipType = await _context.MembershipTypes.FirstOrDefaultAsync(mt => mt.MembershipTypeID == company.MembershipTypeID);
                viewModel.MembershipTypeName = membershipType?.MembershipTypeName;
            }

            // Load Address and location hierarchy
            if (company.AddressID.HasValue) {
                var address = await _context.Addresses.FirstOrDefaultAsync(a => a.AddressID == company.AddressID);
                if (address != null) {
                    viewModel.AddressText = address.AddressText;
                    viewModel.DistrictID = address.DistrictID;

                    if (address.DistrictID.HasValue) {
                        var district = await _context.Districts.FirstOrDefaultAsync(d => d.DistrictID == address.DistrictID);
                        if (district != null) {
                            viewModel.DistrictName = district.DistrictName;
                            viewModel.TownID = district.TownID;

                            if (district.TownID.HasValue) {
                                var town = await _context.Towns.FirstOrDefaultAsync(t => t.TownID == district.TownID);
                                if (town != null) {
                                    viewModel.TownName = town.TownName;
                                    viewModel.CountryID = town.CountryID;

                                    if (town.CountryID.HasValue) {
                                        var country = await _context.Countries.FirstOrDefaultAsync(c => c.CountryID == town.CountryID);
                                        viewModel.CountryName = country?.CountryName;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Firmamıza Yapılan 10 Yıldızlı Değerlendirmeleri Yükle (Hem Alıcılardan Hem Tedarikçilerden)
            var allReceivedReviews = await _context.Reviews
                .Include(r => r.Order)
                .AsNoTracking()
                .Where(r => r.CompanyID == company.CompanyID && r.ReviewScore.HasValue)
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync();

            viewModel.BuyerReviewCount = allReceivedReviews.Count;
            viewModel.BuyerScore = allReceivedReviews.Any() ? Math.Round(allReceivedReviews.Average(r => r.ReviewScore!.Value), 1) : 10.0;
            viewModel.OverallScore = viewModel.BuyerScore;
            viewModel.ReceivedReviews = allReceivedReviews.Select(r => new ReviewDetailItemDto
            {
                ReviewID = r.ReviewID,
                ReviewScore = r.ReviewScore ?? 10.0,
                DeliveryScore = r.DeliveryScore,
                CommunicationScore = r.CommunicationScore,
                ReviewText = r.ReviewText,
                ReviewDate = r.ReviewDate,
                ReviewerName = !string.IsNullOrWhiteSpace(r.ReviewerName) ? r.ReviewerName : (r.ReviewType == "BuyerToSeller" ? "Doğrulanmış Kurumsal Alıcı" : "Onaylı Tedarikçi"),
                OrderNumber = r.Order?.OrderNumber,
                ReviewType = r.ReviewType ?? "BuyerToSeller"
            }).ToList();

            // Kullanıcının Geçmiş Satın Alma Siparişlerini (Alışlarım) Yükle
            var buyerOrders = await _context.Orders
                .Include(o => o.OrderItems)
                .AsNoTracking()
                .Where(o => (o.BuyerCompanyID.HasValue && o.BuyerCompanyID.Value == company.CompanyID) ||
                            (!string.IsNullOrWhiteSpace(o.BuyerEmail) && company.CompanyMail != null && o.BuyerEmail.ToLower() == company.CompanyMail.ToLower()) ||
                            (!string.IsNullOrWhiteSpace(o.BuyerCompanyName) && company.CompanyName != null && o.BuyerCompanyName.ToLower() == company.CompanyName.ToLower()))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var allCompanies = await _context.Companies
                .AsNoTracking()
                .Select(c => new { c.CompanyID, c.CompanyName, c.CompanyOfficial })
                .ToListAsync();

            foreach (var o in buyerOrders)
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

            viewModel.MyOrders = buyerOrders;

            // Kullanıcının Satış Geçmişini (Gelen Satış Siparişleri - Tedarikçi Olarak) Yükle
            var salesOrders = await _context.Orders
                .Include(o => o.OrderItems)
                .AsNoTracking()
                .Where(o => (o.SupplierCompanyID.HasValue && o.SupplierCompanyID.Value == company.CompanyID) ||
                            (!string.IsNullOrWhiteSpace(o.SupplierCompanyName) && company.CompanyName != null && o.SupplierCompanyName.ToLower() == company.CompanyName.ToLower()) ||
                            (o.OrderItems.Any(oi => (oi.SupplierCompanyID.HasValue && oi.SupplierCompanyID.Value == company.CompanyID) ||
                                                    (company.CompanyName != null && oi.SupplierCompanyName != null && oi.SupplierCompanyName.ToLower() == company.CompanyName.ToLower()))))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            viewModel.MySalesOrders = salesOrders;

            // Önceden Alınan Ürünler Listesi (Ürün veya ItemID bazında gruplama)
            var allItemsWithOrders = buyerOrders
                .SelectMany(o => o.OrderItems.Select(oi => new { Item = oi, Order = o }))
                .ToList();

            var groupedItems = allItemsWithOrders
                .GroupBy(x => !string.IsNullOrWhiteSpace(x.Item.ProductName) ? x.Item.ProductName.Trim().ToLower() : (x.Item.ItemID?.ToString() ?? "unknown"))
                .ToList();

            var purchasedList = new List<PurchasedProductDto>();
            foreach (var g in groupedItems)
            {
                var latestEntry = g.OrderByDescending(x => x.Order.OrderDate).First();
                purchasedList.Add(new PurchasedProductDto
                {
                    ItemID = latestEntry.Item.ItemID,
                    ProductName = latestEntry.Item.ProductName,
                    Sku = latestEntry.Item.Sku,
                    CategoryName = latestEntry.Item.CategoryName ?? "Endüstriyel",
                    UnitPrice = latestEntry.Item.UnitPrice,
                    TotalQuantityPurchased = g.Sum(x => x.Item.Quantity),
                    LastPurchaseDate = latestEntry.Order.OrderDate,
                    SupplierCompanyName = latestEntry.Item.SupplierCompanyName ?? latestEntry.Order.SupplierCompanyName ?? "Onaylı Tedarikçi",
                    SupplierCompanyID = latestEntry.Item.SupplierCompanyID ?? latestEntry.Order.SupplierCompanyID,
                    LastOrderID = latestEntry.Order.OrderID,
                    LastOrderNumber = latestEntry.Order.OrderNumber
                });
            }

            viewModel.PurchasedProducts = purchasedList.OrderByDescending(p => p.LastPurchaseDate).ToList();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(
            string? companyOfficial,
            string? companyName,
            string? companyMail,
            string? companyPhone,
            string? tcno,
            string? taxOffice,
            string? countryName,
            string? townName,
            string? districtName,
            string? addressText,
            string? companyDetailText)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                          (Request.Headers.Accept.ToString().Contains("application/json") && !Request.Headers.Accept.ToString().Contains("text/html"));

            var user = await GetCurrentUserCompanyAsync();
            if (user == null)
            {
                var unauthMsg = "Oturum açmış kullanıcı hesabı bulunamadı. Lütfen tekrar giriş yapınız.";
                if (isAjax) return Json(new { success = false, message = unauthMsg });
                TempData["ProfileError"] = unauthMsg;
                return RedirectToAction("Login");
            }

            if (string.IsNullOrWhiteSpace(companyOfficial) || string.IsNullOrWhiteSpace(companyName) || string.IsNullOrWhiteSpace(companyMail))
            {
                var reqMsg = "Lütfen zorunlu alanları (Firma Yetkilisi, Firma Adı, E-posta) doldurunuz.";
                if (isAjax) return Json(new { success = false, message = reqMsg });
                TempData["ProfileError"] = reqMsg;
                return RedirectToAction("Profile");
            }

            try
            {
                var newMailTrimmed = companyMail.Trim().ToLower();
                var currentMailTrimmed = (user.CompanyMail ?? "").Trim().ToLower();
                // E-posta değiştiyse, başka bir hesapta kullanımda olup olmadığını kontrol et
                if (newMailTrimmed != currentMailTrimmed)
                {
                    var emailExists = await _context.Companies.AnyAsync(c => c.CompanyID != user.CompanyID && c.CompanyMail != null && c.CompanyMail.ToLower() == newMailTrimmed);
                    if (emailExists)
                    {
                        var existMsg = "Bu e-posta adresi başka bir firma veya kullanıcı tarafından kullanılmaktadır.";
                        if (isAjax) return Json(new { success = false, message = existMsg });
                        TempData["ProfileError"] = existMsg;
                        return RedirectToAction("Profile");
                    }
                }

                // Kişi ve Firma Bilgilerini Güncelle (CompanyTypeID süper admin ise 4, diğer tüm kullanıcılarda 3)
                user.CompanyOfficial = companyOfficial.Trim();
                user.CompanyName = companyName.Trim();
                user.CompanyTypeID = (user.CompanyTypeID == 4 || string.Equals(user.CompanyMail, "admin@tedarikportal.com", StringComparison.OrdinalIgnoreCase)) ? 4 : 3;
                user.CompanyMail = newMailTrimmed;
                user.CompanyPhone = string.IsNullOrWhiteSpace(companyPhone) ? null : companyPhone.Trim();
                user.TCNO = string.IsNullOrWhiteSpace(tcno) ? null : tcno.Trim();
                user.TaxOffice = string.IsNullOrWhiteSpace(taxOffice) ? null : taxOffice.Trim();
                user.CompanyDetailText = string.IsNullOrWhiteSpace(companyDetailText) ? null : companyDetailText.Trim();

                // Adres Bilgilerini Güncelle / Oluştur
                Addresses? address = null;
                if (user.AddressID.HasValue)
                {
                    address = await _context.Addresses.FirstOrDefaultAsync(a => a.AddressID == user.AddressID.Value);
                }

                int? districtId = null;
                if (!string.IsNullOrWhiteSpace(districtName))
                {
                    var matchedDistrict = await _context.Districts.FirstOrDefaultAsync(d => d.DistrictName == districtName.Trim());
                    districtId = matchedDistrict?.DistrictID;
                }

                string cleanAddressText = string.IsNullOrWhiteSpace(addressText) ? "" : addressText.Trim();
                if (string.IsNullOrWhiteSpace(cleanAddressText) && (!string.IsNullOrWhiteSpace(districtName) || !string.IsNullOrWhiteSpace(townName) || !string.IsNullOrWhiteSpace(countryName)))
                {
                    cleanAddressText = $"{districtName} / {townName} / {countryName}".Trim(' ', '/');
                }

                if (address == null)
                {
                    if (!string.IsNullOrWhiteSpace(cleanAddressText) || districtId.HasValue)
                    {
                        address = new Addresses
                        {
                            AddressText = cleanAddressText,
                            DistrictID = districtId,
                            CompanyID = user.CompanyID
                        };
                        _context.Addresses.Add(address);
                        await _context.SaveChangesAsync();
                        user.AddressID = address.AddressID;
                    }
                }
                else
                {
                    address.AddressText = cleanAddressText;
                    if (districtId.HasValue)
                    {
                        address.DistrictID = districtId;
                    }
                }

                await _context.SaveChangesAsync();

                // Kullanıcı Claims ve Auth Cookie Bilgilerini Yenile
                var userType = User.FindFirst("UserType")?.Value ?? "User";
                string displayName = user.CompanyOfficial ?? user.CompanyName ?? user.CompanyMail;
                string compName = user.CompanyName ?? user.CompanyOfficial ?? user.CompanyMail;

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.CompanyID.ToString()),
                    new Claim("CompanyID", user.CompanyID.ToString()),
                    new Claim(ClaimTypes.Name, displayName),
                    new Claim(ClaimTypes.Email, user.CompanyMail),
                    new Claim("CompanyName", compName),
                    new Claim("UserType", userType),
                    new Claim(ClaimTypes.Role, userType),
                    new Claim("CompanyTypeID", (user.CompanyTypeID ?? 1).ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties { IsPersistent = true };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                var successMsg = "Profil ve kişi bilgileriniz başarıyla güncellendi!";
                if (isAjax)
                {
                    return Json(new
                    {
                        success = true,
                        message = successMsg,
                        official = user.CompanyOfficial,
                        company = user.CompanyName,
                        email = user.CompanyMail,
                        phone = user.CompanyPhone,
                        address = cleanAddressText
                    });
                }

                TempData["ProfileSuccess"] = successMsg;
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                var errMsg = $"Bilgiler güncellenirken bir hata oluştu: {innerMsg}";
                if (isAjax) return Json(new { success = false, message = errMsg });
                TempData["ProfileError"] = errMsg;
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> RequestPasswordReset() {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                          (Request.Headers.Accept.ToString().Contains("application/json") && !Request.Headers.Accept.ToString().Contains("text/html"));

            var user = await GetCurrentUserCompanyAsync();
            if (user == null) {
                var unauthMsg = "Oturum açmış kullanıcı hesabı bulunamadı.";
                if (isAjax) return Json(new { success = false, message = unauthMsg });
                TempData["ProfileError"] = unauthMsg;
                return RedirectToAction("Login");
            }

            var email = user.CompanyMail;
            if (string.IsNullOrEmpty(email)) {
                var mailErr = "Kullanıcıya ait geçerli bir e-posta adresi bulunamadı.";
                if (isAjax) return Json(new { success = false, message = mailErr });
                TempData["ProfileError"] = mailErr;
                return RedirectToAction("Profile");
            }

            try {
                // Generate reset token
                var token = Guid.NewGuid().ToString("N");
                user.ResetToken = token;
                user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);
                await _context.SaveChangesAsync();

                // Build reset link
                var resetLink = Url.Action("ResetPassword", "Account",
                    new { token = token, email = email }, Request.Scheme);

                // Send email via Resend API
                var sent = await _emailService.SendPasswordResetEmailAsync(email, resetLink!);

                if (sent) {
                    var succMsg = $"Şifre sıfırlama bağlantısı kayıtlı e-posta adresinize ({email}) başarıyla gönderildi. Lütfen gelen kutunuzu kontrol ediniz.";
                    if (isAjax) return Json(new { success = true, message = succMsg });
                    TempData["ProfileSuccess"] = succMsg;
                } else {
                    var failMsg = "E-posta gönderimi başarısız oldu. Lütfen daha sonra tekrar deneyiniz.";
                    if (isAjax) return Json(new { success = false, message = failMsg });
                    TempData["ProfileError"] = failMsg;
                }
            }
            catch (Exception ex) {
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                var errMsg = $"Şifre sıfırlama talebi oluşturulurken bir hata oluştu: {innerMsg}";
                if (isAjax) return Json(new { success = false, message = errMsg });
                TempData["ProfileError"] = errMsg;
            }

            return RedirectToAction("Profile");
        }

        [HttpGet]
        public IActionResult Register() {
            return View();
        }
        
        [HttpGet]
        public IActionResult ForgotPassword() {
            return RedirectToAction("Login", new { tab = "forgot-password" });
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email) {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                          (Request.Headers.Accept.ToString().Contains("application/json") && !Request.Headers.Accept.ToString().Contains("text/html"));

            if (string.IsNullOrWhiteSpace(email)) {
                var err = "Lütfen e-posta adresinizi giriniz.";
                if (isAjax) return Json(new { success = false, message = err });
                TempData["ForgotError"] = err;
                return RedirectToAction("Login", new { tab = "forgot-password" });
            }

            try {
                var emailTrimmed = email.Trim().ToLower();
                var user = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyMail != null && c.CompanyMail.ToLower() == emailTrimmed);

                if (user == null) {
                    var err = "Bu e-posta adresi ile kayıtlı bir hesap bulunamadı.";
                    if (isAjax) return Json(new { success = false, message = err });
                    TempData["ForgotError"] = err;
                    return RedirectToAction("Login", new { tab = "forgot-password" });
                }

                // Generate reset token
                var token = Guid.NewGuid().ToString("N");
                user.ResetToken = token;
                user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);
                await _context.SaveChangesAsync();

                // Build reset link
                var resetLink = Url.Action("ResetPassword", "Account",
                    new { token = token, email = email }, Request.Scheme);

                // Send email via Resend API
                var sent = await _emailService.SendPasswordResetEmailAsync(email, resetLink!);

                if (sent) {
                    var msg = "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi. Lütfen gelen kutunuzu kontrol ediniz.";
                    if (isAjax) return Json(new { success = true, message = msg });
                    TempData["ForgotSuccess"] = msg;
                } else {
                    var err = "E-posta gönderilemedi. Lütfen daha sonra tekrar deneyiniz.";
                    if (isAjax) return Json(new { success = false, message = err });
                    TempData["ForgotError"] = err;
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"ForgotPassword Error: {ex.Message}");
                var err = "Bir hata oluştu. Lütfen daha sonra tekrar deneyiniz.";
                if (isAjax) return Json(new { success = false, message = err });
                TempData["ForgotError"] = err;
            }

            return RedirectToAction("Login", new { tab = "forgot-password" });
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token, string email) {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email)) {
                TempData["ErrorMessage"] = "Geçersiz şifre sıfırlama bağlantısı.";
                return RedirectToAction("Login");
            }

            var emailTrimmed = email.Trim().ToLower();
            var user = await _context.Companies.FirstOrDefaultAsync(
                c => c.CompanyMail != null && c.CompanyMail.ToLower() == emailTrimmed && c.ResetToken == token);

            if (user == null || user.ResetTokenExpiry == null || user.ResetTokenExpiry < DateTime.UtcNow) {
                TempData["ErrorMessage"] = "Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş. Lütfen yeni bir talep oluşturunuz.";
                return RedirectToAction("Login", new { tab = "forgot-password" });
            }

            ViewData["Token"] = token;
            ViewData["Email"] = email;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPasswordSubmit(string token, string email, string password, string confirmPassword) {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email)) {
                TempData["ErrorMessage"] = "Geçersiz istek.";
                return RedirectToAction("Login");
            }

            if (string.IsNullOrWhiteSpace(password) || password != confirmPassword) {
                ViewData["ResetError"] = "Şifreler uyuşmuyor.";
                ViewData["Token"] = token;
                ViewData["Email"] = email;
                return View("ResetPassword");
            }

            if (!PasswordHasher.IsStrongPassword(password, out string passError)) {
                ViewData["ResetError"] = passError;
                ViewData["Token"] = token;
                ViewData["Email"] = email;
                return View("ResetPassword");
            }

            var emailTrimmed = email.Trim().ToLower();
            var user = await _context.Companies.FirstOrDefaultAsync(
                c => c.CompanyMail != null && c.CompanyMail.ToLower() == emailTrimmed && c.ResetToken == token);

            if (user == null || user.ResetTokenExpiry == null || user.ResetTokenExpiry < DateTime.UtcNow) {
                TempData["ErrorMessage"] = "Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş.";
                return RedirectToAction("Login", new { tab = "forgot-password" });
            }

            // Update password
            user.CompanyPassword = PasswordHasher.HashPassword(password);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            await _context.SaveChangesAsync();

            TempData["LoginSuccess"] = "Şifreniz başarıyla güncellendi. Yeni şifrenizle giriş yapabilirsiniz.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePhoto(IFormFile? profilePhoto) {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                          (Request.Headers.Accept.ToString().Contains("application/json") && !Request.Headers.Accept.ToString().Contains("text/html"));

            var user = await GetCurrentUserCompanyAsync();
            if (user == null) {
                if (isAjax) {
                    return Json(new { success = false, message = "Lütfen önce oturum açınız." });
                }
                return RedirectToAction("Login");
            }

            // Single file validation
            if (profilePhoto == null || profilePhoto.Length == 0) {
                var msg = "Lütfen tek bir görsel dosyası seçiniz.";
                if (isAjax) {
                    return Json(new { success = false, message = msg });
                }
                TempData["ProfileError"] = msg;
                return RedirectToAction("Profile");
            }

            // Format validation
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var extension = Path.GetExtension(profilePhoto.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension)) {
                var msg = $"Geçersiz dosya formatı ({extension}). Sadece JPG, PNG, WEBP veya GIF formatında tek bir görsel seçebilirsiniz.";
                if (isAjax) {
                    return Json(new { success = false, message = msg });
                }
                TempData["ProfileError"] = msg;
                return RedirectToAction("Profile");
            }

            // Size validation (Max 5 MB)
            const long maxFileSize = 5 * 1024 * 1024;
            if (profilePhoto.Length > maxFileSize) {
                var msg = $"Dosya boyutu çok büyük ({Math.Round(profilePhoto.Length / (1024.0 * 1024.0), 2)} MB). Maksimum izin verilen boyut: 5 MB.";
                if (isAjax) {
                    return Json(new { success = false, message = msg });
                }
                TempData["ProfileError"] = msg;
                return RedirectToAction("Profile");
            }

            try {
                // Delete previous image from Cloudinary if one exists
                if (!string.IsNullOrWhiteSpace(user.CompanyLogo)) {
                    try {
                        await _cloudinaryService.DeleteImageAsync(user.CompanyLogo);
                    }
                    catch (Exception deleteEx) {
                        // Non-critical, continue with upload
                        Console.WriteLine($"Eski profil görseli Cloudinary'den silinirken uyarı: {deleteEx.Message}");
                    }
                }

                // Upload to Cloudinary under satinalmaportali/profiles folder with 500px width limit
                var uploadResult = await _cloudinaryService.UploadImageAsync(
                    profilePhoto,
                    folder: "satinalmaportali/profiles",
                    width: 500
                );

                if (!uploadResult.Success || string.IsNullOrWhiteSpace(uploadResult.SecureUrl)) {
                    var msg = uploadResult.Error ?? "Görsel Cloudinary sunucularına yüklenemedi.";
                    if (isAjax) {
                        return Json(new { success = false, message = msg });
                    }
                    TempData["ProfileError"] = msg;
                    return RedirectToAction("Profile");
                }

                // Update database entity with Cloudinary Secure URL
                user.CompanyLogo = uploadResult.SecureUrl;
                await _context.SaveChangesAsync();

                var successMsg = "Profil fotoğrafınız Cloudinary'ye başarıyla yüklendi ve profilinize kaydedildi!";
                if (isAjax) {
                    return Json(new { success = true, message = successMsg, imageUrl = uploadResult.SecureUrl });
                }

                TempData["ProfileSuccess"] = successMsg;
                return RedirectToAction("Profile");
            }
            catch (Exception ex) {
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                var errorMsg = $"Profil fotoğrafı yükleme sırasında bir hata oluştu: {innerMsg}";
                if (isAjax) {
                    return Json(new { success = false, message = errorMsg });
                }
                TempData["ProfileError"] = errorMsg;
                return RedirectToAction("Profile");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProfilePhoto() {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                          (Request.Headers.Accept.ToString().Contains("application/json") && !Request.Headers.Accept.ToString().Contains("text/html"));

            var user = await GetCurrentUserCompanyAsync();
            if (user == null) {
                if (isAjax) {
                    return Json(new { success = false, message = "Lütfen önce oturum açınız." });
                }
                return RedirectToAction("Login");
            }

            try {
                if (!string.IsNullOrWhiteSpace(user.CompanyLogo)) {
                    // Delete from Cloudinary
                    await _cloudinaryService.DeleteImageAsync(user.CompanyLogo);

                    // Clear database field
                    user.CompanyLogo = null;
                    await _context.SaveChangesAsync();

                    var successMsg = "Profil fotoğrafınız başarıyla kaldırıldı.";
                    if (isAjax) {
                        return Json(new { success = true, message = successMsg });
                    }
                    TempData["ProfileSuccess"] = successMsg;
                } else {
                    var msg = "Kaldırılacak bir profil fotoğrafı bulunmamaktadır.";
                    if (isAjax) {
                        return Json(new { success = false, message = msg });
                    }
                    TempData["ProfileSuccess"] = msg;
                }
            }
            catch (Exception ex) {
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                var errorMsg = $"Profil fotoğrafı silinirken hata oluştu: {innerMsg}";
                if (isAjax) {
                    return Json(new { success = false, message = errorMsg });
                }
                TempData["ProfileError"] = errorMsg;
            }

            return RedirectToAction("Profile");
        }
    }
}
