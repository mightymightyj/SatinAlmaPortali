using Microsoft.AspNetCore.Mvc;

namespace satinalmaportali2.Controllers
{
    public class CartController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Kurumsal Satınalma Sepetim";
            return View();
        }
    }
}
