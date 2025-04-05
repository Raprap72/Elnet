using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;
using System.Threading.Tasks;
using System.Linq;

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
            // Dashboard statistics
            ViewBag.TotalRooms = await _context.Rooms.CountAsync();
            ViewBag.AvailableRooms = await _context.Rooms.CountAsync(r => r.IsAvailable);
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.PendingBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Pending);
            ViewBag.ConfirmedBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Confirmed);
            
            // Today's check-ins and check-outs
            var today = System.DateTime.Today;
            ViewBag.TodayCheckIns = await _context.Bookings.CountAsync(b => b.CheckInDate.Date == today && b.Status == BookingStatus.Confirmed);
            ViewBag.TodayCheckOuts = await _context.Bookings.CountAsync(b => b.CheckOutDate.Date == today && b.Status == BookingStatus.CheckedIn);
            
            // Payment statistics
            ViewBag.CompletedPayments = await _context.Bookings.CountAsync(b => b.Payments.Any(p => p.PaymentStatus == PaymentStatus.Completed));
            ViewBag.PendingPayments = await _context.Bookings.CountAsync(b => b.Payments.Any(p => p.PaymentStatus == PaymentStatus.Pending));
            ViewBag.TotalRevenue = await _context.Payments
                .Where(p => p.PaymentStatus == PaymentStatus.Completed)
                .SumAsync(p => p.Amount);
            
            // Recent bookings for dashboard
            var recentBookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                .Include(b => b.Payments)
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .ToListAsync();
                
            return View(recentBookings);
        }
    }
} 