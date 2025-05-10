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
    public class HousekeepingController : AdminBaseController
    {
        private readonly ApplicationDbContext _context;

        public HousekeepingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Housekeeping
        public async Task<IActionResult> Index(string searchString, string filterStatus, string filterType)
        {
            var housekeepingTasks = _context.HousekeepingTasks
                .Include(h => h.Room)
                .Include(h => h.AssignedStaff)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                housekeepingTasks = housekeepingTasks.Where(h => 
                    h.TaskDescription.Contains(searchString) ||
                    h.Room.RoomNumber.Contains(searchString) ||
                    (h.AssignedStaff != null && h.AssignedStaff.FullName.Contains(searchString)));
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(filterStatus) && Enum.TryParse<HousekeepingTaskStatus>(filterStatus, out var status))
            {
                housekeepingTasks = housekeepingTasks.Where(h => h.Status == status);
            }

            // Apply type filter
            if (!string.IsNullOrEmpty(filterType) && Enum.TryParse<HousekeepingTaskType>(filterType, out var type))
            {
                housekeepingTasks = housekeepingTasks.Where(h => h.TaskType == type);
            }

            // Populate the ViewBag with filter options
            ViewBag.Statuses = Enum.GetValues(typeof(HousekeepingTaskStatus))
                .Cast<HousekeepingTaskStatus>()
                .Select(s => new SelectListItem
                {
                    Value = s.ToString(),
                    Text = s.ToString(),
                    Selected = s.ToString() == filterStatus
                });

            ViewBag.Types = Enum.GetValues(typeof(HousekeepingTaskType))
                .Cast<HousekeepingTaskType>()
                .Select(t => new SelectListItem
                {
                    Value = t.ToString(),
                    Text = t.ToString(),
                    Selected = t.ToString() == filterType
                });

            // Get task counts for dashboard stats
            ViewBag.PendingTasks = await _context.HousekeepingTasks.CountAsync(h => h.Status == HousekeepingTaskStatus.Pending);
            ViewBag.InProgressTasks = await _context.HousekeepingTasks.CountAsync(h => h.Status == HousekeepingTaskStatus.InProgress);
            ViewBag.CompletedTasks = await _context.HousekeepingTasks.CountAsync(h => h.Status == HousekeepingTaskStatus.Completed);
            ViewBag.TotalTasks = await _context.HousekeepingTasks.CountAsync();

            // Sort by priority and creation date
            housekeepingTasks = housekeepingTasks.OrderBy(h => h.Priority).ThenByDescending(h => h.CreatedAt);

            return View(await housekeepingTasks.ToListAsync());
        }

        // GET: Admin/Housekeeping/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var housekeepingTask = await _context.HousekeepingTasks
                .Include(h => h.Room)
                .Include(h => h.AssignedStaff)
                .FirstOrDefaultAsync(m => m.TaskId == id);

            if (housekeepingTask == null)
            {
                return NotFound();
            }

            return View(housekeepingTask);
        }

        // GET: Admin/Housekeeping/Create
        public IActionResult Create()
        {
            // Populate room dropdown
            ViewBag.RoomId = new SelectList(_context.Rooms, "RoomId", "RoomNumber");
            
            // Populate staff dropdown (only staff users)
            ViewBag.StaffId = new SelectList(_context.Users.Where(u => u.UserType == UserType.Staff), "UserId", "FullName");
            
            // Populate task type dropdown
            ViewBag.TaskTypes = new SelectList(Enum.GetValues(typeof(HousekeepingTaskType)));
            
            // Populate priority dropdown
            ViewBag.Priorities = new SelectList(new[] 
            {
                new { Value = 1, Text = "High" },
                new { Value = 2, Text = "Medium" },
                new { Value = 3, Text = "Low" }
            }, "Value", "Text");
            
            return View();
        }

        // POST: Admin/Housekeeping/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoomId,StaffId,TaskDescription,TaskType,Priority")] HousekeepingTask housekeepingTask)
        {
            if (ModelState.IsValid)
            {
                housekeepingTask.CreatedAt = DateTime.Now;
                
                // If staff is assigned, update status and assignment date
                if (housekeepingTask.StaffId.HasValue)
                {
                    housekeepingTask.Status = HousekeepingTaskStatus.Assigned;
                    housekeepingTask.AssignedAt = DateTime.Now;
                }
                else
                {
                    housekeepingTask.Status = HousekeepingTaskStatus.Pending;
                }
                
                _context.Add(housekeepingTask);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Housekeeping task created successfully!";
                return RedirectToAction(nameof(Index));
            }
            
            // If model is invalid, repopulate dropdowns
            ViewBag.RoomId = new SelectList(_context.Rooms, "RoomId", "RoomNumber", housekeepingTask.RoomId);
            ViewBag.StaffId = new SelectList(_context.Users.Where(u => u.UserType == UserType.Staff), "UserId", "FullName", housekeepingTask.StaffId);
            ViewBag.TaskTypes = new SelectList(Enum.GetValues(typeof(HousekeepingTaskType)), housekeepingTask.TaskType);
            ViewBag.Priorities = new SelectList(new[] 
            {
                new { Value = 1, Text = "High" },
                new { Value = 2, Text = "Medium" },
                new { Value = 3, Text = "Low" }
            }, "Value", "Text", housekeepingTask.Priority);
            
            return View(housekeepingTask);
        }

        // GET: Admin/Housekeeping/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var housekeepingTask = await _context.HousekeepingTasks.FindAsync(id);
            if (housekeepingTask == null)
            {
                return NotFound();
            }
            
            // Populate dropdowns
            ViewBag.RoomId = new SelectList(_context.Rooms, "RoomId", "RoomNumber", housekeepingTask.RoomId);
            ViewBag.StaffId = new SelectList(_context.Users.Where(u => u.UserType == UserType.Staff), "UserId", "FullName", housekeepingTask.StaffId);
            ViewBag.TaskTypes = new SelectList(Enum.GetValues(typeof(HousekeepingTaskType)), housekeepingTask.TaskType);
            ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(HousekeepingTaskStatus)), housekeepingTask.Status);
            ViewBag.Priorities = new SelectList(new[] 
            {
                new { Value = 1, Text = "High" },
                new { Value = 2, Text = "Medium" },
                new { Value = 3, Text = "Low" }
            }, "Value", "Text", housekeepingTask.Priority);
            
            return View(housekeepingTask);
        }

        // POST: Admin/Housekeeping/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TaskId,RoomId,StaffId,TaskDescription,TaskType,Status,Priority,Notes,CreatedAt,AssignedAt,CompletedAt")] HousekeepingTask housekeepingTask)
        {
            if (id != housekeepingTask.TaskId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // If task is completed now but wasn't before
                    if (housekeepingTask.Status == HousekeepingTaskStatus.Completed && housekeepingTask.CompletedAt == null)
                    {
                        housekeepingTask.CompletedAt = DateTime.Now;
                    }
                    
                    // If staff is assigned now but wasn't before
                    var originalTask = await _context.HousekeepingTasks.AsNoTracking().FirstOrDefaultAsync(h => h.TaskId == id);
                    if (housekeepingTask.StaffId.HasValue && (!originalTask.StaffId.HasValue || originalTask.StaffId != housekeepingTask.StaffId))
                    {
                        housekeepingTask.AssignedAt = DateTime.Now;
                        
                        // If status is still pending, update to assigned
                        if (housekeepingTask.Status == HousekeepingTaskStatus.Pending)
                        {
                            housekeepingTask.Status = HousekeepingTaskStatus.Assigned;
                        }
                    }
                    
                    _context.Update(housekeepingTask);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Housekeeping task updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HousekeepingTaskExists(housekeepingTask.TaskId))
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
            ViewBag.RoomId = new SelectList(_context.Rooms, "RoomId", "RoomNumber", housekeepingTask.RoomId);
            ViewBag.StaffId = new SelectList(_context.Users.Where(u => u.UserType == UserType.Staff), "UserId", "FullName", housekeepingTask.StaffId);
            ViewBag.TaskTypes = new SelectList(Enum.GetValues(typeof(HousekeepingTaskType)), housekeepingTask.TaskType);
            ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(HousekeepingTaskStatus)), housekeepingTask.Status);
            ViewBag.Priorities = new SelectList(new[] 
            {
                new { Value = 1, Text = "High" },
                new { Value = 2, Text = "Medium" },
                new { Value = 3, Text = "Low" }
            }, "Value", "Text", housekeepingTask.Priority);
            
            return View(housekeepingTask);
        }

        // GET: Admin/Housekeeping/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var housekeepingTask = await _context.HousekeepingTasks
                .Include(h => h.Room)
                .Include(h => h.AssignedStaff)
                .FirstOrDefaultAsync(m => m.TaskId == id);
                
            if (housekeepingTask == null)
            {
                return NotFound();
            }

            return View(housekeepingTask);
        }

        // POST: Admin/Housekeeping/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var housekeepingTask = await _context.HousekeepingTasks.FindAsync(id);
            _context.HousekeepingTasks.Remove(housekeepingTask);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Housekeeping task deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Housekeeping/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, HousekeepingTaskStatus status)
        {
            var housekeepingTask = await _context.HousekeepingTasks.FindAsync(id);
            if (housekeepingTask == null)
            {
                return NotFound();
            }
            
            housekeepingTask.Status = status;
            
            // Set completion date if status is now Completed
            if (status == HousekeepingTaskStatus.Completed)
            {
                housekeepingTask.CompletedAt = DateTime.Now;
            }
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Task status updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Housekeeping/AssignStaff/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignStaff(int id, int staffId)
        {
            var housekeepingTask = await _context.HousekeepingTasks.FindAsync(id);
            if (housekeepingTask == null)
            {
                return NotFound();
            }
            
            housekeepingTask.StaffId = staffId;
            housekeepingTask.AssignedAt = DateTime.Now;
            
            // Update status to assigned if it was pending
            if (housekeepingTask.Status == HousekeepingTaskStatus.Pending)
            {
                housekeepingTask.Status = HousekeepingTaskStatus.Assigned;
            }
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Staff assigned successfully!";
            return RedirectToAction(nameof(Index));
        }

        private bool HousekeepingTaskExists(int id)
        {
            return _context.HousekeepingTasks.Any(e => e.TaskId == id);
        }
    }
} 