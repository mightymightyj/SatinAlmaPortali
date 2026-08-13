using Microsoft.AspNetCore.Mvc;
using satinalmaportali2.Models;
using System.Diagnostics;

namespace satinalmaportali2.Controllers {
    public class HomeController : Controller {
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
