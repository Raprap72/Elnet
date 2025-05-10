using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace RoyalStayHotel.Areas.Staff.Controllers
{
    [Area("Staff")]
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            // TODO: Implement authentication logic here
            if (username == "staff" && password == "password")
            {
                // Simulate successful login
                return RedirectToAction("Index", "Dashboard", new { area = "Staff" });
            }
            ViewBag.Error = "Invalid username or password.";
            return View();
        }
    }
} 