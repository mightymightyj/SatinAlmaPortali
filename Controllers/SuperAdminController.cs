using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using satinalmaportali2.Data;
using satinalmaportali2.Helpers;
using satinalmaportali2.Models;
using System.Security.Claims;

namespace satinalmaportali2.Controllers
{
    /// <summary>
    /// Süper Admin Yönetim Merkezi Denetleyicisi.
    /// Yalnızca firma türü 'Süper Admin' (CompanyTypeID = 4) veya 'admin' rolüne sahip hesaplar erişebilir.
    /// Standart kullanıcı arayüzlerinde doğrudan link/buton bulunmaz, gizli ve güvenli URL üzerinden çalışır.
    /// </summary>
    [Authorize]
    [Route("SuperAdmin")]
    [Route("Admin")]
    public class SuperAdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SuperAdminController(ApplicationDbContext context)
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

        /// <summary>
        /// Aktif kullanıcının Süper Admin yetkisini 3 katmanlı doğrular (Claims, DB Firma Türü, E-posta).
        /// </summary>
        private async Task<bool> CheckIsSuperAdminAsync()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return false;
            }

            var userType = User.FindFirst("UserType")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;
            var compTypeClaim = User.FindFirst("CompanyTypeID")?.Value;

            if (string.Equals(userType, "admin", StringComparison.OrdinalIgnoreCase) || compTypeClaim == "4")
            {
                return true;
            }

            int companyId = GetCurrentCompanyId();
            if (companyId > 0)
            {
                var company = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == companyId);
                if (company != null)
                {
                    if (company.CompanyTypeID == 4 || string.Equals(company.CompanyMail, "admin@tedarikportal.com", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Süper Admin Ana Kontrol Paneli (Genel Bakış, Kategoriler, Firmalar, Ayarlar)
        /// </summary>
        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index(string? tab = "overview", string? search = null, int? filterType = null)
        {
            if (!await CheckIsSuperAdminAsync())
            {
                TempData["ErrorMessage"] = "Bu yönetim paneline yalnızca Süper Admin hesapları erişebilir.";
                return RedirectToAction("Index", "Home");
            }

            var viewModel = new SuperAdminDashboardViewModel
            {
                ActiveTab = string.IsNullOrWhiteSpace(tab) ? "overview" : tab.ToLowerInvariant(),
                SearchQuery = search,
                FilterCompanyType = filterType
            };

            try
            {
                // 1. Firma Türleri ve Üyelik Türleri
                viewModel.AvailableCompanyTypes = await _context.CompanyTypes.AsNoTracking().OrderBy(ct => ct.CompanyTypeID).ToListAsync();
                viewModel.AvailableMembershipTypes = await _context.MembershipTypes.AsNoTracking().OrderBy(mt => mt.MembershipTypeID).ToListAsync();

                var typeMap = viewModel.AvailableCompanyTypes.ToDictionary(ct => ct.CompanyTypeID ?? 0, ct => ct.CompanyTypeName ?? "Bilinmiyor");
                var memberMap = viewModel.AvailableMembershipTypes.ToDictionary(mt => mt.MembershipTypeID ?? 0, mt => mt.MembershipTypeName ?? "Standart");

                // 2. Kategoriler
                var dbCategories = await _context.Category.AsNoTracking().ToListAsync();
                var catMap = dbCategories.ToDictionary(c => c.CategoryID, c => c.CategoryName ?? "");

                // Talep sayımlarını kategori bazlı çek
                var requestsByCat = await _context.Requests.AsNoTracking()
                    .Where(r => r.CategoryID.HasValue)
                    .GroupBy(r => r.CategoryID!.Value)
                    .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

                viewModel.Categories = dbCategories.Select(c => new CategoryAdminDto
                {
                    CategoryID = c.CategoryID,
                    CategoryName = c.CategoryName ?? "İsimsiz Kategori",
                    ParentCategoryID = c.ParentCategoryID,
                    ParentCategoryName = c.ParentCategoryID.HasValue && catMap.TryGetValue(c.ParentCategoryID.Value, out var pName) ? pName : null,
                    SubCategoryCount = dbCategories.Count(sc => sc.ParentCategoryID == c.CategoryID),
                    LinkedRequestsCount = requestsByCat.TryGetValue(c.CategoryID, out var reqCount) ? reqCount : 0
                }).OrderBy(c => c.ParentCategoryID.HasValue).ThenBy(c => c.CategoryName).ToList();

                viewModel.AvailableParentCategories = dbCategories.Where(c => !c.ParentCategoryID.HasValue || c.ParentCategoryID.Value == 0).OrderBy(c => c.CategoryName).ToList();

                // 3. Firmalar
                var dbCompanies = await _context.Companies.AsNoTracking().OrderByDescending(c => c.CompanyID).ToListAsync();
                var dbAddresses = await _context.Addresses.AsNoTracking().ToDictionaryAsync(a => a.AddressID, a => a.AddressText ?? "");

                var requestsBySender = await _context.Requests.AsNoTracking()
                    .Where(r => r.SenderID.HasValue)
                    .GroupBy(r => r.SenderID!.Value)
                    .Select(g => new { SenderID = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.SenderID, x => x.Count);

                var offersBySupplier = await _context.RequestOffers.AsNoTracking()
                    .GroupBy(o => o.SupplierID)
                    .Select(g => new { SupplierID = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.SupplierID, x => x.Count);

                viewModel.Companies = dbCompanies.Select(c => new CompanyAdminDto
                {
                    CompanyID = c.CompanyID,
                    CompanyName = c.CompanyName ?? "İsimsiz Firma",
                    CompanyOfficial = c.CompanyOfficial ?? "-",
                    CompanyMail = c.CompanyMail ?? "-",
                    CompanyPhone = c.CompanyPhone,
                    TCNO = c.TCNO,
                    TaxOffice = c.TaxOffice,
                    CompanyTypeID = c.CompanyTypeID,
                    CompanyTypeName = c.CompanyTypeID.HasValue && typeMap.TryGetValue(c.CompanyTypeID.Value, out var tName) ? tName : "Belirtilmemiş",
                    MembershipTypeID = c.MembershipTypeID,
                    MembershipTypeName = c.MembershipTypeID.HasValue && memberMap.TryGetValue(c.MembershipTypeID.Value, out var mName) ? mName : "Standart",
                    CompanyLogo = c.CompanyLogo,
                    CompanyDetailText = c.CompanyDetailText,
                    AddressText = c.AddressID.HasValue && dbAddresses.TryGetValue(c.AddressID.Value, out var addr) ? addr : null,
                    RequestCount = requestsBySender.TryGetValue(c.CompanyID, out var rCount) ? rCount : 0,
                    OfferCount = offersBySupplier.TryGetValue(c.CompanyID, out var oCount) ? oCount : 0
                }).ToList();

                // 4. İstatistikler
                var dbRequests = await _context.Requests.AsNoTracking().ToListAsync();
                var dbOffers = await _context.RequestOffers.AsNoTracking().ToListAsync();

                viewModel.Stats = new SuperAdminStatsDto
                {
                    TotalCompanies = dbCompanies.Count,
                    SupplierCount = dbCompanies.Count(c => c.CompanyTypeID == 1),
                    BuyerCount = dbCompanies.Count(c => c.CompanyTypeID == 2),
                    BothCount = dbCompanies.Count(c => c.CompanyTypeID == 3),
                    SuperAdminCount = dbCompanies.Count(c => c.CompanyTypeID == 4 || string.Equals(c.CompanyMail, "admin@tedarikportal.com", StringComparison.OrdinalIgnoreCase)),

                    TotalCategories = dbCategories.Count,
                    RootCategoriesCount = dbCategories.Count(c => !c.ParentCategoryID.HasValue || c.ParentCategoryID.Value == 0),
                    SubCategoriesCount = dbCategories.Count(c => c.ParentCategoryID.HasValue && c.ParentCategoryID.Value > 0),

                    TotalRequests = dbRequests.Count,
                    TotalOffers = dbOffers.Count,
                    TotalEstimatedBudget = dbRequests.Sum(r => r.EstimatedBudget ?? 0),
                    TotalOfferedVolume = dbOffers.Sum(o => o.OfferedPrice)
                };

                // Filtreleme (Kategoriler & Firmalar)
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var q = search.Trim().ToLowerInvariant();
                    if (viewModel.ActiveTab == "categories")
                    {
                        viewModel.Categories = viewModel.Categories.Where(c => c.CategoryName.ToLowerInvariant().Contains(q) || (c.ParentCategoryName != null && c.ParentCategoryName.ToLowerInvariant().Contains(q))).ToList();
                    }
                    else if (viewModel.ActiveTab == "companies")
                    {
                        viewModel.Companies = viewModel.Companies.Where(c => c.CompanyName.ToLowerInvariant().Contains(q) || c.CompanyOfficial.ToLowerInvariant().Contains(q) || c.CompanyMail.ToLowerInvariant().Contains(q) || (c.TCNO != null && c.TCNO.Contains(q))).ToList();
                    }
                }

                if (filterType.HasValue && filterType.Value > 0)
                {
                    viewModel.Companies = viewModel.Companies.Where(c => c.CompanyTypeID == filterType.Value).ToList();
                }
            }
            catch (Exception ex)
            {
                TempData["AdminError"] = "Veriler yüklenirken hata oluştu: " + ex.Message;
            }

            return View(viewModel);
        }

        // =========================================================================
        // KATEGORİ YÖNETİMİ ACTIONS
        // =========================================================================

        [HttpPost]
        [Route("CreateCategory")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string categoryName, int? parentCategoryId)
        {
            if (!await CheckIsSuperAdminAsync()) return Forbid();

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["AdminError"] = "Kategori adı boş bırakılamaz.";
                return RedirectToAction(nameof(Index), new { tab = "categories" });
            }

            try
            {
                var newCat = new Category
                {
                    CategoryName = categoryName.Trim(),
                    ParentCategoryID = (parentCategoryId.HasValue && parentCategoryId.Value > 0) ? parentCategoryId.Value : null
                };

                _context.Category.Add(newCat);
                await _context.SaveChangesAsync();

                TempData["AdminSuccess"] = $"'{newCat.CategoryName}' kategorisi başarıyla eklendi!";
            }
            catch (Exception ex)
            {
                TempData["AdminError"] = "Kategori ekleme hatası: " + ex.Message;
            }

            return RedirectToAction(nameof(Index), new { tab = "categories" });
        }

        [HttpPost]
        [Route("UpdateCategory")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCategory(EditCategoryInputModel model)
        {
            if (!await CheckIsSuperAdminAsync()) return Forbid();

            if (!ModelState.IsValid || model.CategoryID <= 0)
            {
                TempData["AdminError"] = "Geçersiz kategori bilgisi.";
                return RedirectToAction(nameof(Index), new { tab = "categories" });
            }

            try
            {
                var category = await _context.Category.FirstOrDefaultAsync(c => c.CategoryID == model.CategoryID);
                if (category == null)
                {
                    TempData["AdminError"] = "Güncellenecek kategori bulunamadı.";
                    return RedirectToAction(nameof(Index), new { tab = "categories" });
                }

                if (model.ParentCategoryID == model.CategoryID)
                {
                    TempData["AdminError"] = "Bir kategori kendisinin üst kategorisi olamaz.";
                    return RedirectToAction(nameof(Index), new { tab = "categories" });
                }

                category.CategoryName = model.CategoryName.Trim();
                category.ParentCategoryID = (model.ParentCategoryID.HasValue && model.ParentCategoryID.Value > 0) ? model.ParentCategoryID.Value : null;

                await _context.SaveChangesAsync();
                TempData["AdminSuccess"] = $"'{category.CategoryName}' kategorisi başarıyla güncellendi!";
            }
            catch (Exception ex)
            {
                TempData["AdminError"] = "Kategori güncelleme hatası: " + ex.Message;
            }

            return RedirectToAction(nameof(Index), new { tab = "categories" });
        }

        [HttpPost]
        [Route("DeleteCategory")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int categoryId)
        {
            if (!await CheckIsSuperAdminAsync()) return Forbid();

            try
            {
                var category = await _context.Category.FirstOrDefaultAsync(c => c.CategoryID == categoryId);
                if (category == null)
                {
                    TempData["AdminError"] = "Silinecek kategori bulunamadı.";
                    return RedirectToAction(nameof(Index), new { tab = "categories" });
                }

                // Alt kategorileri kontrol et
                var subCats = await _context.Category.Where(c => c.ParentCategoryID == categoryId).ToListAsync();
                foreach (var sc in subCats)
                {
                    sc.ParentCategoryID = null; // Alt kategorileri ana kategoriye çek
                }

                _context.Category.Remove(category);
                await _context.SaveChangesAsync();

                TempData["AdminSuccess"] = $"'{category.CategoryName}' kategorisi başarıyla silindi.";
            }
            catch (Exception ex)
            {
                TempData["AdminError"] = "Kategori silinirken hata oluştu: " + ex.Message;
            }

            return RedirectToAction(nameof(Index), new { tab = "categories" });
        }

        [HttpGet]
        [Route("GetCategory/{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            if (!await CheckIsSuperAdminAsync()) return Forbid();

            var category = await _context.Category.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryID == id);
            if (category == null) return NotFound(new { success = false, message = "Kategori bulunamadı." });

            return Json(new
            {
                success = true,
                categoryId = category.CategoryID,
                categoryName = category.CategoryName,
                parentCategoryId = category.ParentCategoryID
            });
        }

        // =========================================================================
        // FİRMA VE FİRMA AYARLARI ACTIONS
        // =========================================================================

        [HttpPost]
        [Route("UpdateCompany")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCompany(EditCompanyInputModel model)
        {
            if (!await CheckIsSuperAdminAsync()) return Forbid();

            if (!ModelState.IsValid || model.CompanyID <= 0)
            {
                TempData["AdminError"] = "Lütfen tüm zorunlu firma bilgilerini eksiksiz doldurun.";
                return RedirectToAction(nameof(Index), new { tab = "companies" });
            }

            try
            {
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyID == model.CompanyID);
                if (company == null)
                {
                    TempData["AdminError"] = "Güncellenecek firma bulunamadı.";
                    return RedirectToAction(nameof(Index), new { tab = "companies" });
                }

                // E-posta çakışma kontrolü
                var emailTrim = model.CompanyMail.Trim().ToLowerInvariant();
                var emailExists = await _context.Companies.AnyAsync(c => c.CompanyID != model.CompanyID && c.CompanyMail != null && c.CompanyMail.ToLower() == emailTrim);
                if (emailExists)
                {
                    TempData["AdminError"] = "Bu e-posta adresi başka bir firma tarafından kullanılmaktadır.";
                    return RedirectToAction(nameof(Index), new { tab = "companies" });
                }

                company.CompanyName = model.CompanyName.Trim();
                company.CompanyOfficial = model.CompanyOfficial.Trim();
                company.CompanyMail = emailTrim;
                company.CompanyPhone = string.IsNullOrWhiteSpace(model.CompanyPhone) ? null : model.CompanyPhone.Trim();
                company.TCNO = string.IsNullOrWhiteSpace(model.TCNO) ? null : model.TCNO.Trim();
                company.TaxOffice = string.IsNullOrWhiteSpace(model.TaxOffice) ? null : model.TaxOffice.Trim();
                company.CompanyTypeID = model.CompanyTypeID ?? company.CompanyTypeID ?? 1;
                company.MembershipTypeID = model.MembershipTypeID ?? company.MembershipTypeID ?? 1;
                company.CompanyDetailText = string.IsNullOrWhiteSpace(model.CompanyDetailText) ? null : model.CompanyDetailText.Trim();

                // Adres Güncelleme
                if (!string.IsNullOrWhiteSpace(model.AddressText))
                {
                    if (company.AddressID.HasValue)
                    {
                        var addr = await _context.Addresses.FirstOrDefaultAsync(a => a.AddressID == company.AddressID.Value);
                        if (addr != null)
                        {
                            addr.AddressText = model.AddressText.Trim();
                        }
                    }
                    else
                    {
                        var newAddr = new Addresses
                        {
                            AddressText = model.AddressText.Trim(),
                            CompanyID = company.CompanyID
                        };
                        _context.Addresses.Add(newAddr);
                        await _context.SaveChangesAsync();
                        company.AddressID = newAddr.AddressID;
                    }
                }

                await _context.SaveChangesAsync();
                TempData["AdminSuccess"] = $"'{company.CompanyName}' firması ve tüm ayarları başarıyla güncellendi!";
            }
            catch (Exception ex)
            {
                TempData["AdminError"] = "Firma güncelleme hatası: " + ex.Message;
            }

            return RedirectToAction(nameof(Index), new { tab = "companies" });
        }

        [HttpPost]
        [Route("ResetCompanyPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetCompanyPassword(ResetPasswordInputModel model)
        {
            if (!await CheckIsSuperAdminAsync()) return Forbid();

            if (model.CompanyID <= 0 || string.IsNullOrWhiteSpace(model.NewPassword) || model.NewPassword.Length < 6)
            {
                TempData["AdminError"] = "Şifre en az 6 karakter olmalıdır.";
                return RedirectToAction(nameof(Index), new { tab = "companies" });
            }

            try
            {
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyID == model.CompanyID);
                if (company == null)
                {
                    TempData["AdminError"] = "Firma bulunamadı.";
                    return RedirectToAction(nameof(Index), new { tab = "companies" });
                }

                company.CompanyPassword = PasswordHasher.HashPassword(model.NewPassword.Trim());
                await _context.SaveChangesAsync();

                TempData["AdminSuccess"] = $"'{company.CompanyName}' firmasının şifresi başarıyla sıfırlandı!";
            }
            catch (Exception ex)
            {
                TempData["AdminError"] = "Şifre sıfırlama hatası: " + ex.Message;
            }

            return RedirectToAction(nameof(Index), new { tab = "companies" });
        }

        [HttpPost]
        [Route("QuickChangeCompanyType")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickChangeCompanyType(int companyId, int companyTypeId)
        {
            if (!await CheckIsSuperAdminAsync()) return Forbid();

            try
            {
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyID == companyId);
                if (company == null)
                {
                    TempData["AdminError"] = "Firma bulunamadı.";
                    return RedirectToAction(nameof(Index), new { tab = "companies" });
                }

                company.CompanyTypeID = companyTypeId;
                await _context.SaveChangesAsync();

                TempData["AdminSuccess"] = $"'{company.CompanyName}' firmasının türü güncellendi!";
            }
            catch (Exception ex)
            {
                TempData["AdminError"] = "Firma türü değiştirme hatası: " + ex.Message;
            }

            return RedirectToAction(nameof(Index), new { tab = "companies" });
        }

        [HttpPost]
        [Route("DeleteCompany")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCompany(int companyId)
        {
            if (!await CheckIsSuperAdminAsync()) return Forbid();

            int currentCompId = GetCurrentCompanyId();
            if (companyId == currentCompId)
            {
                TempData["AdminError"] = "Kendi aktif oturum hesabınızı silemezsiniz!";
                return RedirectToAction(nameof(Index), new { tab = "companies" });
            }

            try
            {
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyID == companyId);
                if (company == null)
                {
                    TempData["AdminError"] = "Silinecek firma bulunamadı.";
                    return RedirectToAction(nameof(Index), new { tab = "companies" });
                }

                _context.Companies.Remove(company);
                await _context.SaveChangesAsync();

                TempData["AdminSuccess"] = $"'{company.CompanyName}' firması kalıcı olarak silindi.";
            }
            catch (Exception ex)
            {
                TempData["AdminError"] = "Firma silinirken hata oluştu: " + ex.Message;
            }

            return RedirectToAction(nameof(Index), new { tab = "companies" });
        }

        [HttpGet]
        [Route("GetCompany/{id}")]
        public async Task<IActionResult> GetCompany(int id)
        {
            if (!await CheckIsSuperAdminAsync()) return Forbid();

            var company = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyID == id);
            if (company == null) return NotFound(new { success = false, message = "Firma bulunamadı." });

            string addressText = "";
            if (company.AddressID.HasValue)
            {
                var addr = await _context.Addresses.AsNoTracking().FirstOrDefaultAsync(a => a.AddressID == company.AddressID.Value);
                addressText = addr?.AddressText ?? "";
            }

            return Json(new
            {
                success = true,
                companyId = company.CompanyID,
                companyName = company.CompanyName,
                companyOfficial = company.CompanyOfficial,
                companyMail = company.CompanyMail,
                companyPhone = company.CompanyPhone,
                tcno = company.TCNO,
                taxOffice = company.TaxOffice,
                companyTypeId = company.CompanyTypeID,
                membershipTypeId = company.MembershipTypeID,
                companyDetailText = company.CompanyDetailText,
                addressText = addressText
            });
        }

        // =========================================================================
        // SİSTEM ARAÇLARI ACTIONS
        // =========================================================================

        [HttpPost]
        [Route("SeedSystemDefaults")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeedSystemDefaults()
        {
            if (!await CheckIsSuperAdminAsync()) return Forbid();

            try
            {
                await DbInitializer.SeedDataAsync(_context);
                TempData["AdminSuccess"] = "Sistem varsayılan verileri ve tür tabloları başarıyla senkronize edildi!";
            }
            catch (Exception ex)
            {
                TempData["AdminError"] = "Senkronizasyon hatası: " + ex.Message;
            }

            return RedirectToAction(nameof(Index), new { tab = "settings" });
        }
    }
}
