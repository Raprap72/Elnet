using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;

namespace RoyalStayHotel.Areas.Staff.Controllers
{
    public class MaintenanceController : StaffBaseController
    {
        private readonly ApplicationDbContext _context;

        public MaintenanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var requests = await _context.MaintenanceRequests
                .Include(m => m.Room)
                .Include(m => m.ReportedBy)
                .Include(m => m.AssignedTechnician)
                .OrderByDescending(m => m.Priority)
                .ThenByDescending(m => m.RequestDate)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int requestId, MaintenanceRequestStatus status)
        {
            var request = await _context.MaintenanceRequests.FindAsync(requestId);
            if (request == null)
            {
                return NotFound();
            }

            request.Status = status;
            
            if (status == MaintenanceRequestStatus.Completed)
            {
                request.CompletedAt = DateTime.Now;
                
                // Create notification for housekeeping
                var notification = new Notification
                {
                    Title = "Maintenance Completed",
                    Message = $"Room {request.RoomId}: {request.Title} has been fixed",
                    Type = NotificationType.Maintenance,
                    Priority = NotificationPriority.Normal,
                    CreatedAt = DateTime.Now,
                    RelatedEntityType = "MaintenanceRequest",
                    RelatedEntityId = request.Id
                };

                _context.Notifications.Add(notification);
            }

            try
            {
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AssignTechnician(int requestId, int technicianId)
        {
            var request = await _context.MaintenanceRequests.FindAsync(requestId);
            if (request == null)
            {
                return NotFound();
            }

            request.TechnicianId = technicianId;
            request.Status = MaintenanceRequestStatus.InProgress;
            request.ScheduledFor = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
                
                // Create notification for assigned technician
                var notification = new Notification
                {
                    Title = "New Maintenance Assignment",
                    Message = $"You have been assigned to fix: {request.Title} in Room {request.RoomId}",
                    Type = NotificationType.Maintenance,
                    Priority = NotificationPriority.High,
                    CreatedAt = DateTime.Now,
                    UserId = technicianId,
                    RelatedEntityType = "MaintenanceRequest",
                    RelatedEntityId = request.Id
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateResolution(int requestId, string resolution, decimal? cost)
        {
            var request = await _context.MaintenanceRequests.FindAsync(requestId);
            if (request == null)
            {
                return NotFound();
            }

            request.Resolution = resolution;
            request.CostOfRepair = cost;
            request.CompletedAt = DateTime.Now;
            request.Status = MaintenanceRequestStatus.Completed;

            try
            {
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTechnicians()
        {
            var technicians = await _context.Users
                .Where(u => u.UserType == UserType.Staff)
                .Select(u => new { u.UserId, u.FullName })
                .ToListAsync();

            return Json(technicians);
        }
    }
} 