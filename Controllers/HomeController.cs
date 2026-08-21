using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using satinalmaportali2.Data;
using satinalmaportali2.Models;
using satinalmaportali2.Services;
using System.Diagnostics;

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
