using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;
using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.AspNetCore.Http;

namespace RoyalStayHotel.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : AdminBaseController
    {
        private readonly ApplicationDbContext _context;
        
        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<IActionResult> Index()
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (!adminId.HasValue)
            {
                return RedirectToAction("Login", "Account", new { area = "Admin" });
            }

            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminId && u.UserType == UserType.Admin);
            if (admin == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account", new { area = "Admin" });
            }

            var viewModel = new DashboardViewModel
            {
                AdminName = admin.FullName,
                TotalBookings = await _context.Bookings.CountAsync(),
                TotalRevenue = await _context.Payments
                    .Where(p => p.Status == PaymentStatus.Completed)
                    .SumAsync(p => p.Amount),
                AvailableRooms = await _context.Rooms.CountAsync(r => r.AvailabilityStatus == AvailabilityStatus.Available),
                PendingMaintenance = await _context.MaintenanceRequests.CountAsync(m => m.Status == MaintenanceRequestStatus.Reported),
                PendingCheckIns = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Confirmed && b.CheckInDate.Date == DateTime.Today),
                PendingPayments = await _context.Payments.CountAsync(p => p.Status == PaymentStatus.Pending),
                RecentBookings = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Room)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(5)
                    .ToListAsync(),
                RecentPayments = await _context.Payments
                    .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                    .OrderByDescending(p => p.PaymentDate)
                    .Take(5)
                    .ToListAsync(),
                MaintenanceRequests = await _context.MaintenanceRequests
                    .Include(m => m.Room)
                    .OrderByDescending(m => m.RequestDate)
                    .Take(5)
                    .ToListAsync()
            };

            return View(viewModel);
        }
    }
} 