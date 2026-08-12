using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace satinalmaportali2.Controllers {
    public class AccountController : Controller {
        [HttpGet]
        public IActionResult Login(string tab = "user-login") {
            ViewData["ActiveTab"] = tab;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string userType) {
            if (!string.IsNullOrEmpty(email)) {
                var claims = new List<Claim> {
                    new Claim(ClaimTypes.Name, email),
                    new Claim(ClaimTypes.Email, email),
                    new Claim("UserType", userType ?? "User")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties { IsPersistent = true };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                return RedirectToAction("Index", "Home");
            }

            ViewData["ActiveTab"] = "user-login";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Logout() {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Profile() {
            if (User.Identity?.IsAuthenticated == true) {
                return View("Login");
            }
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Register() {
            return RedirectToAction("Login", new { tab = "register" });
        }

        [HttpGet]
        public IActionResult ForgotPassword() {
            return RedirectToAction("Login", new { tab = "forgot-password" });
        }
    }
}
