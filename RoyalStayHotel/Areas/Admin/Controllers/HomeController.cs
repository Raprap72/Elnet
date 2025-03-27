using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;
using System.Linq;
using System.Threading.Tasks;

namespace RoyalStayHotel.Areas.Admin.Controllers
{
    public class HomeController : AdminBaseController
    {
        private readonly ApplicationDbContext _context;
        
        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<IActionResult> Index()
        {
            // Dashboard statistics
            ViewBag.TotalRooms = await _context.Rooms.CountAsync();
            ViewBag.AvailableRooms = await _context.Rooms.CountAsync(r => r.AvailabilityStatus == AvailabilityStatus.Available);
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.PendingBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Pending);
            
            // Recent bookings for dashboard
            var recentBookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .ToListAsync();
                
            return View(recentBookings);
        }
        
        public IActionResult Login()
        {
            // If already logged in, redirect to dashboard
            if (IsAdminAuthenticated())
            {
                return RedirectToAction("Index");
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
                
                return RedirectToAction("Index");
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