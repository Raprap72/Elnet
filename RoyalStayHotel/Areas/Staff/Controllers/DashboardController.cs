using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;
using RoyalStayHotel.Models.ViewModels;

namespace RoyalStayHotel.Areas.Staff.Controllers
{
    public class DashboardController : StaffBaseController
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var staffId = GetCurrentStaffId();
            var today = DateTime.Today;

            var viewModel = new StaffDashboardViewModel
            {
                // Get today's housekeeping tasks
                HousekeepingTasks = await _context.HousekeepingTasks
                    .Include(h => h.Room)
                    .Where(h => h.StaffId == staffId && 
                           h.Status != HousekeepingTaskStatus.Completed)
                    .OrderBy(h => h.Priority)
                    .Take(5)
                    .ToListAsync(),

                // Get pending maintenance requests
                MaintenanceRequests = await _context.MaintenanceRequests
                    .Include(m => m.Room)
                    .Where(m => m.Status == MaintenanceRequestStatus.Reported)
                    .OrderBy(m => m.Priority)
                    .Take(5)
                    .ToListAsync(),

                // Get today's check-ins
                TodayCheckIns = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Room)
                    .Where(b => b.CheckInDate.Date == today)
                    .OrderBy(b => b.CheckInDate)
                    .ToListAsync(),

                // Get today's check-outs
                TodayCheckOuts = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Room)
                    .Where(b => b.CheckOutDate.Date == today)
                    .OrderBy(b => b.CheckOutDate)
                    .ToListAsync(),

                // Get urgent notifications
                UrgentNotifications = await _context.Notifications
                    .Where(n => n.Priority == NotificationPriority.High && 
                           !n.IsRead && 
                           (n.UserId == null || n.UserId == staffId))
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .ToListAsync(),

                // Quick stats
                Stats = new DashboardStats
                {
                    PendingHousekeepingTasks = await _context.HousekeepingTasks
                        .CountAsync(h => h.Status == HousekeepingTaskStatus.Pending),
                    PendingMaintenanceRequests = await _context.MaintenanceRequests
                        .CountAsync(m => m.Status == MaintenanceRequestStatus.Reported),
                    TotalCheckInsToday = await _context.Bookings
                        .CountAsync(b => b.CheckInDate.Date == today),
                    TotalCheckOutsToday = await _context.Bookings
                        .CountAsync(b => b.CheckOutDate.Date == today),
                    AvailableRooms = await _context.Rooms
                        .CountAsync(r => r.AvailabilityStatus == AvailabilityStatus.Available)
                }
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationAsRead(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
} 