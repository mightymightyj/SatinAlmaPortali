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

        public IActionResult Index() {
            return View();
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
}
