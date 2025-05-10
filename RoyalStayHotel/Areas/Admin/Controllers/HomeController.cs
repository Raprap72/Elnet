using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;
using System.Linq;
using System.Threading.Tasks;

namespace RoyalStayHotel.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public IActionResult Index()
        {
            // Simple view that doesn't require authentication
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId.HasValue)
            {
                // User is logged in, show welcome message
                ViewBag.Message = "Welcome to the Admin Dashboard!";
                return View();
            }
            
            // User is not logged in, redirect to login
            return RedirectToAction("Login", "Account");
        }
        
        public IActionResult Login()
        {
            // If already logged in, redirect to dashboard
            if (IsAdminAuthenticated())
            {
                return RedirectToAction("Index", "Dashboard");
            }
            
            return View();
        }
        
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Username and password are required.");
                return View();
            }
            
            var admin = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.Password == password && u.UserType == UserType.Admin);
                
            if (admin != null)
            {
                // In a real app, use proper authentication with ASP.NET Identity
                HttpContext.Session.SetInt32("AdminId", admin.UserId);
                HttpContext.Session.SetString("AdminName", admin.FullName);
                
                // Redirect to dashboard
                return RedirectToAction("Index", "Dashboard");
            }
            
            ModelState.AddModelError("", "Invalid login attempt. Please check your username and password.");
            return View();
        }
        
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
        
        // Helper method to check if admin is authenticated
        private bool IsAdminAuthenticated()
        {
            return HttpContext.Session.GetInt32("AdminId").HasValue;
        }
    }
}