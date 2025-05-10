using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;
using System.Linq;
using System.Threading.Tasks;

namespace RoyalStayHotel.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get all application settings
            var settings = await _context.SiteSettings.ToListAsync();
            
            // Group settings by category
            ViewBag.GeneralSettings = settings.Where(s => s.Category == "General").ToList();
            ViewBag.BookingSettings = settings.Where(s => s.Category == "Booking").ToList();
            ViewBag.PaymentSettings = settings.Where(s => s.Category == "Payment").ToList();
            ViewBag.EmailSettings = settings.Where(s => s.Category == "Email").ToList();
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(string[] keys, string[] values)
        {
            if (keys.Length != values.Length)
            {
                TempData["ErrorMessage"] = "Invalid form submission.";
                return RedirectToAction(nameof(Index));
            }

            for (int i = 0; i < keys.Length; i++)
            {
                var setting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == keys[i]);
                if (setting != null)
                {
                    setting.Value = values[i];
                    _context.Update(setting);
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Settings updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ManageUsers()
        {
            // Get the list of users
            var users = await _context.Users.ToListAsync();
                
            return View(users);
        }

        public IActionResult BackupDatabase()
        {
            // This would include logic to backup the database
            TempData["InfoMessage"] = "Database backup functionality would be implemented here.";
            return View();
        }

        public IActionResult SystemLogs()
        {
            // This would display system logs
            return View();
        }
    }
} 