using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RoyalStayHotel.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class NotificationsController : AdminBaseController
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Notifications
        public async Task<IActionResult> Index(string searchString, string filterType, bool? filterRead)
        {
            var notifications = _context.Notifications
                .Include(n => n.User)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                notifications = notifications.Where(n => 
                    n.Title.Contains(searchString) ||
                    n.Message.Contains(searchString) ||
                    (n.User != null && n.User.FullName.Contains(searchString)));
            }

            // Apply type filter
            if (!string.IsNullOrEmpty(filterType) && Enum.TryParse<NotificationType>(filterType, out var type))
            {
                notifications = notifications.Where(n => n.Type == type);
            }

            // Apply read filter
            if (filterRead.HasValue)
            {
                notifications = notifications.Where(n => n.IsRead == filterRead.Value);
            }

            // Populate the ViewBag with filter options
            ViewBag.Types = Enum.GetValues(typeof(NotificationType))
                .Cast<NotificationType>()
                .Select(t => new SelectListItem
                {
                    Value = t.ToString(),
                    Text = t.ToString(),
                    Selected = t.ToString() == filterType
                });

            // Set read filter selection
            ViewBag.FilterRead = filterRead;

            // Get stats for dashboard
            ViewBag.UnreadNotifications = await _context.Notifications.CountAsync(n => !n.IsRead);
            ViewBag.TodayNotifications = await _context.Notifications.CountAsync(n => n.CreatedAt.Date == DateTime.Today);
            ViewBag.SystemNotifications = await _context.Notifications.CountAsync(n => n.Type == NotificationType.System);
            ViewBag.TotalNotifications = await _context.Notifications.CountAsync();

            // Sort by read status and creation date
            notifications = notifications.OrderBy(n => n.IsRead).ThenByDescending(n => n.CreatedAt);

            return View(await notifications.ToListAsync());
        }

        // GET: Admin/Notifications/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notification = await _context.Notifications
                .Include(n => n.User)
                .FirstOrDefaultAsync(m => m.NotificationId == id);

            if (notification == null)
            {
                return NotFound();
            }

            // Mark notification as read if it wasn't already
            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return View(notification);
        }

        // GET: Admin/Notifications/Create
        public IActionResult Create()
        {
            // Populate notification type dropdown
            ViewBag.NotificationTypes = new SelectList(Enum.GetValues(typeof(NotificationType)));
            
            // Populate priority dropdown
            ViewBag.Priorities = new SelectList(Enum.GetValues(typeof(NotificationPriority)));
            
            // Populate user dropdown for user-specific notifications
            ViewBag.Users = new SelectList(_context.Users, "UserId", "FullName");
            
            return View();
        }

        // POST: Admin/Notifications/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,Title,Message,Type,Priority,RelatedEntityType,RelatedEntityId,ActionLink")] Notification notification)
        {
            if (ModelState.IsValid)
            {
                notification.CreatedAt = DateTime.Now;
                notification.IsRead = false;
                
                _context.Add(notification);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Notification created successfully!";
                return RedirectToAction(nameof(Index));
            }
            
            // If model is invalid, repopulate dropdowns
            ViewBag.NotificationTypes = new SelectList(Enum.GetValues(typeof(NotificationType)), notification.Type);
            ViewBag.Priorities = new SelectList(Enum.GetValues(typeof(NotificationPriority)), notification.Priority);
            ViewBag.Users = new SelectList(_context.Users, "UserId", "FullName", notification.UserId);
            
            return View(notification);
        }

        // GET: Admin/Notifications/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound();
            }
            
            // Populate dropdowns
            ViewBag.NotificationTypes = new SelectList(Enum.GetValues(typeof(NotificationType)), notification.Type);
            ViewBag.Priorities = new SelectList(Enum.GetValues(typeof(NotificationPriority)), notification.Priority);
            ViewBag.Users = new SelectList(_context.Users, "UserId", "FullName", notification.UserId);
            
            return View(notification);
        }

        // POST: Admin/Notifications/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("NotificationId,UserId,Title,Message,Type,Priority,IsRead,CreatedAt,ReadAt,RelatedEntityType,RelatedEntityId,ActionLink")] Notification notification)
        {
            if (id != notification.NotificationId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // If notification is marked as read now but wasn't before
                    var originalNotification = await _context.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.NotificationId == id);
                    if (notification.IsRead && !originalNotification.IsRead)
                    {
                        notification.ReadAt = DateTime.Now;
                    }
                    else if (!notification.IsRead && originalNotification.IsRead)
                    {
                        notification.ReadAt = null;
                    }
                    
                    _context.Update(notification);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Notification updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NotificationExists(notification.NotificationId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            
            // If model is invalid, repopulate dropdowns
            ViewBag.NotificationTypes = new SelectList(Enum.GetValues(typeof(NotificationType)), notification.Type);
            ViewBag.Priorities = new SelectList(Enum.GetValues(typeof(NotificationPriority)), notification.Priority);
            ViewBag.Users = new SelectList(_context.Users, "UserId", "FullName", notification.UserId);
            
            return View(notification);
        }

        // GET: Admin/Notifications/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notification = await _context.Notifications
                .Include(n => n.User)
                .FirstOrDefaultAsync(m => m.NotificationId == id);
                
            if (notification == null)
            {
                return NotFound();
            }

            return View(notification);
        }

        // POST: Admin/Notifications/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Notification deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Notifications/MarkAsRead/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound();
            }
            
            notification.IsRead = true;
            notification.ReadAt = DateTime.Now;
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Notification marked as read!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Notifications/MarkAllAsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => !n.IsRead)
                .ToListAsync();
                
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
            }
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"{unreadNotifications.Count} notifications marked as read!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Notifications/SendSystemNotification
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendSystemNotification(string title, string message, NotificationPriority priority)
        {
            // Create a notification for all users
            var users = await _context.Users.ToListAsync();
            
            foreach (var user in users)
            {
                var notification = new Notification
                {
                    UserId = user.UserId,
                    Title = title,
                    Message = message,
                    Type = NotificationType.System,
                    Priority = priority,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };
                
                _context.Notifications.Add(notification);
            }
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"System notification sent to {users.Count} users!";
            return RedirectToAction(nameof(Index));
        }

        private bool NotificationExists(int id)
        {
            return _context.Notifications.Any(e => e.NotificationId == id);
        }
    }
} 