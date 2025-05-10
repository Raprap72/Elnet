using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;

namespace RoyalStayHotel.Areas.Staff.Controllers
{
    public class HousekeepingController : StaffBaseController
    {
        private readonly ApplicationDbContext _context;

        public HousekeepingController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var staffId = GetCurrentStaffId();
            var tasks = await _context.HousekeepingTasks
                .Include(h => h.Room)
                .Include(h => h.AssignedStaff)
                .Where(h => h.StaffId == staffId)
                .OrderByDescending(h => h.Priority)
                .ThenBy(h => h.CreatedAt)
                .ToListAsync();

            return View(tasks);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int taskId, HousekeepingTaskStatus status)
        {
            var task = await _context.HousekeepingTasks.FindAsync(taskId);
            if (task == null)
            {
                return NotFound();
            }

            // Verify the task belongs to the current staff member
            if (task.StaffId != GetCurrentStaffId())
            {
                return Forbid();
            }

            task.Status = status;
            
            if (status == HousekeepingTaskStatus.Completed)
            {
                task.CompletedAt = DateTime.Now;
                
                // Update room status if task is completed
                var room = await _context.Rooms.FindAsync(task.RoomId);
                if (room != null)
                {
                    room.AvailabilityStatus = AvailabilityStatus.Available;
                    _context.Rooms.Update(room);
                }
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
        public async Task<IActionResult> ReportMaintenance(int roomId, string issue)
        {
            var maintenanceRequest = new MaintenanceRequest
            {
                RoomId = roomId,
                Title = issue,
                Description = issue,
                Status = MaintenanceRequestStatus.Reported,
                Priority = 2, // Medium priority
                RequestDate = DateTime.Now,
                ReportedById = GetCurrentStaffId()
            };

            _context.MaintenanceRequests.Add(maintenanceRequest);

            try
            {
                await _context.SaveChangesAsync();
                
                // Create notification for maintenance staff
                var notification = new Notification
                {
                    Title = "New Maintenance Request",
                    Message = $"Room {roomId}: {issue}",
                    Type = NotificationType.Maintenance,
                    Priority = NotificationPriority.High,
                    CreatedAt = DateTime.Now,
                    RelatedEntityType = "MaintenanceRequest",
                    RelatedEntityId = maintenanceRequest.Id
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

        [HttpGet]
        public async Task<IActionResult> GetRoomDetails(int roomId)
        {
            var room = await _context.Rooms
                .Select(r => new
                {
                    r.RoomId,
                    r.RoomNumber,
                    r.RoomType,
                    r.AvailabilityStatus,
                    LastCleaned = _context.HousekeepingTasks
                        .Where(h => h.RoomId == r.RoomId && h.Status == HousekeepingTaskStatus.Completed)
                        .OrderByDescending(h => h.CompletedAt)
                        .Select(h => h.CompletedAt)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(r => r.RoomId == roomId);

            if (room == null)
            {
                return NotFound();
            }

            return Json(room);
        }
    }
} 