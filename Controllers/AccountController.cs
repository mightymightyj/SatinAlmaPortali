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

        public AccountController(ApplicationDbContext context, EmailService emailService) {
            _context = context;
            _emailService = emailService;
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
                // Query database via EF Core
                var dbUser = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyMail == email);

                // User not found in database
                if (dbUser == null) {
                    ViewData["ErrorMessage"] = "Girdiğiniz e-posta adresi ile kayıtlı bir hesap bulunamadı!";
                    ViewData["ActiveTab"] = userType == "admin" ? "admin-login" : "user-login";
                    return View();
                }

                // Verify password against stored hash
                if (!PasswordHasher.VerifyPassword(password, dbUser.CompanyPassword)) {
                    ViewData["ErrorMessage"] = "Girdiğiniz şifre hatalı!";
                    ViewData["ActiveTab"] = userType == "admin" ? "admin-login" : "user-login";
                    return View();
                }

                string displayName = dbUser.CompanyOfficial ?? dbUser.CompanyName ?? email.Split('@')[0];

                var claims = new List<Claim> {
                    new Claim(ClaimTypes.Name, displayName),
                    new Claim(ClaimTypes.Email, email),
                    new Claim("UserType", userType ?? "User")
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
                    cmd.CommandText = "SELECT TABLE_SCHEMA + '.' + TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
                    using (var reader = await cmd.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            tables.Add(reader.GetString(0));
                        }
                    }
                }

                var columns = new List<string>();
                using (var cmd = _context.Database.GetDbConnection().CreateCommand()) {
                    cmd.CommandText = "SELECT TABLE_NAME + '.' + COLUMN_NAME + ' (' + DATA_TYPE + ')' FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME LIKE '%Company%'";
                    using (var reader = await cmd.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            columns.Add(reader.GetString(0));
                        }
                    }
                }

                int count = await _context.Companies.CountAsync();
                return Content($"VERİTABANI BAĞLANTISI BAŞARILI!\nKaydolmuş Firma Sayısı: {count}\n\nTablolar:\n{string.Join("\n", tables)}\n\nCompany Kolonları:\n{string.Join("\n", columns)}");
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
            int? companyTypeID, string tcno, string companyPhone, string taxOffice, string companyDetailText) {

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
                var existingUser = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyMail == email);
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
                // STEP 2: Save Company with AddressID FK
                // ====================================
                var newCompany = new Company {
                    CompanyName = fullname,
                    CompanyOfficial = fullname,
                    CompanyMail = email,
                    CompanyPassword = PasswordHasher.HashPassword(password),
                    CompanyTypeID = companyTypeID ?? 1,
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
                    new Claim(ClaimTypes.Name, fullname ?? email),
                    new Claim(ClaimTypes.Email, email),
                    new Claim("UserType", "User")
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

        [HttpGet]
        public IActionResult Profile() {
            return RedirectToAction("Index", "Home");
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
            if (string.IsNullOrWhiteSpace(email)) {
                TempData["ForgotError"] = "Lütfen e-posta adresinizi giriniz.";
                return RedirectToAction("Login", new { tab = "forgot-password" });
            }

            try {
                var user = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyMail == email);

                if (user == null) {
                    TempData["ForgotError"] = "Bu e-posta adresi ile kayıtlı bir hesap bulunamadı.";
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
                    TempData["ForgotSuccess"] = "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi. Lütfen gelen kutunuzu kontrol ediniz.";
                } else {
                    TempData["ForgotError"] = "E-posta gönderilemedi. Lütfen daha sonra tekrar deneyiniz.";
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"ForgotPassword Error: {ex.Message}");
                TempData["ForgotError"] = "Bir hata oluştu. Lütfen daha sonra tekrar deneyiniz.";
            }

            return RedirectToAction("Login", new { tab = "forgot-password" });
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token, string email) {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email)) {
                TempData["ErrorMessage"] = "Geçersiz şifre sıfırlama bağlantısı.";
                return RedirectToAction("Login");
            }

            var user = await _context.Companies.FirstOrDefaultAsync(
                c => c.CompanyMail == email && c.ResetToken == token);

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

            var user = await _context.Companies.FirstOrDefaultAsync(
                c => c.CompanyMail == email && c.ResetToken == token);

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
    }
}
