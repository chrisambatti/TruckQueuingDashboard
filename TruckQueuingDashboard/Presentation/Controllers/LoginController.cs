using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace TruckQueuingDashboard.Presentation.Controllers
{
    public class LoginController : Controller
    {
        // Simple user store – replace with database query later
        private readonly Dictionary<string, string> _users = new()
        {
            { "admin", "Admin@123" },
            { "user1", "password1" },
            { "Mr.Bean", "Teddy" }
        };

        [HttpGet]
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Dispatcher", "Dashboard");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Username and password are required.");
                return View();
            }

            // Validate credentials (case-sensitive)
            if (_users.TryGetValue(username, out var storedPassword) && storedPassword == password)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, "User") // optional
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                return RedirectToAction("Dispatcher", "Dashboard");
            }

            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Login");
        }
    }
}