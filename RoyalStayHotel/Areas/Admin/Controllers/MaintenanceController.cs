using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

namespace RoyalStayHotel.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MaintenanceController : AdminBaseController
    {
        private readonly ApplicationDbContext _context;

        public MaintenanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Maintenance
        public async Task<IActionResult> Index(string searchString, string filterStatus, string filterType, int? priorityFilter)
        {
            var maintenanceRequests = _context.MaintenanceRequests
                .Include(m => m.Room)
                .Include(m => m.ReportedBy)
                .Include(m => m.AssignedTechnician)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                maintenanceRequests = maintenanceRequests.Where(m => 
                    m.Title.Contains(searchString) ||
                    m.Description.Contains(searchString) ||
                    m.Room.RoomNumber.Contains(searchString) ||
                    (m.ReportedBy != null && m.ReportedBy.FullName.Contains(searchString)) ||
                    (m.AssignedTechnician != null && m.AssignedTechnician.FullName.Contains(searchString)));
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(filterStatus) && Enum.TryParse<MaintenanceRequestStatus>(filterStatus, out var status))
            {
                maintenanceRequests = maintenanceRequests.Where(m => m.Status == status);
            }

            // Apply type filter
            if (!string.IsNullOrEmpty(filterType) && Enum.TryParse<MaintenanceIssueType>(filterType, out var type))
            {
                maintenanceRequests = maintenanceRequests.Where(m => m.IssueType == type);
            }

            // Apply priority filter
            if (priorityFilter.HasValue)
            {
                maintenanceRequests = maintenanceRequests.Where(m => m.Priority == priorityFilter.Value);
            }

            // Populate the ViewBag with filter options
            ViewBag.Statuses = Enum.GetValues(typeof(MaintenanceRequestStatus))
                .Cast<MaintenanceRequestStatus>()
                .Select(s => new SelectListItem
                {
                    Value = s.ToString(),
                    Text = s.ToString(),
                    Selected = s.ToString() == filterStatus
                });

            ViewBag.Types = Enum.GetValues(typeof(MaintenanceIssueType))
                .Cast<MaintenanceIssueType>()
                .Select(t => new SelectListItem
                {
                    Value = t.ToString(),
                    Text = t.ToString(),
                    Selected = t.ToString() == filterType
                });

            ViewBag.Priorities = new SelectListItem[]
            {
                new SelectListItem { Value = "1", Text = "Critical", Selected = priorityFilter == 1 },
                new SelectListItem { Value = "2", Text = "High", Selected = priorityFilter == 2 },
                new SelectListItem { Value = "3", Text = "Medium", Selected = priorityFilter == 3 },
                new SelectListItem { Value = "4", Text = "Low", Selected = priorityFilter == 4 }
            };

            // Get request counts for dashboard stats
            ViewBag.ReportedRequests = await _context.MaintenanceRequests.CountAsync(m => m.Status == MaintenanceRequestStatus.Reported);
            ViewBag.InProgressRequests = await _context.MaintenanceRequests.CountAsync(m => m.Status == MaintenanceRequestStatus.InProgress);
            ViewBag.CompletedRequests = await _context.MaintenanceRequests.CountAsync(m => m.Status == MaintenanceRequestStatus.Completed);
            ViewBag.TotalRequests = await _context.MaintenanceRequests.CountAsync();

            // Sort by priority and report date
            maintenanceRequests = maintenanceRequests.OrderBy(m => m.Priority).ThenByDescending(m => m.ReportedAt);

            return View(await maintenanceRequests.ToListAsync());
        }

        // GET: Admin/Maintenance/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Try to find the request by its primary identifier
            var maintenanceRequest = await _context.MaintenanceRequests
                .Include(m => m.Room)
                .Include(m => m.ReportedBy)
                .Include(m => m.AssignedTechnician)
                .FirstOrDefaultAsync(m => m.Id == id);

            // If not found, try with RequestId
            if (maintenanceRequest == null)
            {
                maintenanceRequest = await _context.MaintenanceRequests
                    .Include(m => m.Room)
                    .Include(m => m.ReportedBy)
                    .Include(m => m.AssignedTechnician)
                    .FirstOrDefaultAsync(m => m.RequestId == id);
            }

            if (maintenanceRequest == null)
            {
                // Log the error for debugging
                TempData["ErrorMessage"] = $"Maintenance request with ID {id} not found";
                return RedirectToAction(nameof(Index));
            }
            
            // Add technicians for the assignment dropdown
            ViewBag.Technicians = new SelectList(_context.Users
                .Where(u => u.UserType == UserType.Staff)
                .ToList(), "UserId", "FullName");

            return View(maintenanceRequest);
        }

        // GET: Admin/Maintenance/Create
        public IActionResult Create()
        {
            // Populate room dropdown
            ViewBag.RoomId = new SelectList(_context.Rooms, "RoomId", "RoomNumber");
            
            // Populate reported by dropdown
            ViewBag.ReportedById = new SelectList(_context.Users, "UserId", "FullName");
            
            // Populate technician dropdown (only staff users)
            ViewBag.TechnicianId = new SelectList(_context.Users.Where(u => u.UserType == UserType.Staff), "UserId", "FullName");
            
            // Populate issue type dropdown
            ViewBag.IssueTypes = new SelectList(Enum.GetValues(typeof(MaintenanceIssueType)));
            
            // Populate priority dropdown
            ViewBag.Priorities = new SelectList(new[] 
            {
                new { Value = 1, Text = "Critical" },
                new { Value = 2, Text = "High" },
                new { Value = 3, Text = "Medium" },
                new { Value = 4, Text = "Low" }
            }, "Value", "Text");
            
            return View();
        }

        // POST: Admin/Maintenance/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoomId,ReportedById,TechnicianId,Title,Description,IssueType,Priority,ScheduledFor")] MaintenanceRequest maintenanceRequest)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    maintenanceRequest.ReportedAt = DateTime.Now;
                    
                    // If technician is assigned, update status to scheduled
                    if (maintenanceRequest.TechnicianId.HasValue)
                    {
                        maintenanceRequest.Status = MaintenanceRequestStatus.Scheduled;
                    }
                    else
                    {
                        maintenanceRequest.Status = MaintenanceRequestStatus.Reported;
                    }
                    
                    _context.Add(maintenanceRequest);
                    await _context.SaveChangesAsync();
                    
                    // Update room status to Maintenance if it's a critical priority
                    if (maintenanceRequest.Priority == 1)
                    {
                        var room = await _context.Rooms.FindAsync(maintenanceRequest.RoomId);
                        if (room != null)
                        {
                            room.AvailabilityStatus = AvailabilityStatus.Maintenance;
                            await _context.SaveChangesAsync();
                        }
                    }
                    
                    TempData["SuccessMessage"] = "Maintenance request created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error creating maintenance request: " + ex.Message);
                }
            }
            
            // If model is invalid, repopulate dropdowns
            ViewBag.RoomId = new SelectList(_context.Rooms, "RoomId", "RoomNumber", maintenanceRequest.RoomId);
            ViewBag.ReportedById = new SelectList(_context.Users, "UserId", "FullName", maintenanceRequest.ReportedById);
            ViewBag.TechnicianId = new SelectList(_context.Users.Where(u => u.UserType == UserType.Staff), "UserId", "FullName", maintenanceRequest.TechnicianId);
            ViewBag.IssueTypes = new SelectList(Enum.GetValues(typeof(MaintenanceIssueType)), maintenanceRequest.IssueType);
            ViewBag.Priorities = new SelectList(new[] 
            {
                new { Value = 1, Text = "Critical" },
                new { Value = 2, Text = "High" },
                new { Value = 3, Text = "Medium" },
                new { Value = 4, Text = "Low" }
            }, "Value", "Text", maintenanceRequest.Priority);
            
            return View(maintenanceRequest);
        }

        // GET: Admin/Maintenance/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Try to find the request by Id first, then by RequestId if needed
            var maintenanceRequest = await _context.MaintenanceRequests.FindAsync(id) ??
                                    await _context.MaintenanceRequests.FirstOrDefaultAsync(m => m.RequestId == id);
                                    
            if (maintenanceRequest == null)
            {
                TempData["ErrorMessage"] = $"Maintenance request with ID {id} not found";
                return RedirectToAction(nameof(Index));
            }
            
            // Populate dropdowns
            ViewBag.RoomId = new SelectList(_context.Rooms, "RoomId", "RoomNumber", maintenanceRequest.RoomId);
            ViewBag.ReportedById = new SelectList(_context.Users, "UserId", "FullName", maintenanceRequest.ReportedById);
            ViewBag.TechnicianId = new SelectList(_context.Users.Where(u => u.UserType == UserType.Staff), "UserId", "FullName", maintenanceRequest.TechnicianId);
            
            // Issue types and other enums
            ViewBag.IssueTypes = new SelectList(Enum.GetValues(typeof(MaintenanceIssueType)), maintenanceRequest.IssueType);
            ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(MaintenanceRequestStatus)), maintenanceRequest.Status);
            
            // Priority options
            ViewBag.Priorities = new SelectList(new[] 
            {
                new { Value = 1, Text = "Critical" },
                new { Value = 2, Text = "High" },
                new { Value = 3, Text = "Medium" },
                new { Value = 4, Text = "Low" }
            }, "Value", "Text", maintenanceRequest.Priority);
            
            return View(maintenanceRequest);
        }

        // POST: Admin/Maintenance/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,RequestId,RoomId,ReportedById,TechnicianId,Title,Description,Status,IssueType,Priority,ReportedAt,ScheduledFor,CompletedAt,Resolution,CostOfRepair,Notes")] MaintenanceRequest maintenanceRequest)
        {
            if (id != maintenanceRequest.Id)
            {
                // Try to see if the id matches RequestId instead
                if (id != maintenanceRequest.RequestId)
                {
                    TempData["ErrorMessage"] = "Invalid maintenance request ID";
                    return RedirectToAction(nameof(Index));
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the original status to check for status changes
                    var originalRequest = await _context.MaintenanceRequests
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.Id == maintenanceRequest.Id);
                    
                    var statusChanged = originalRequest != null && originalRequest.Status != maintenanceRequest.Status;
                    var wasCompletedBefore = originalRequest != null && originalRequest.Status == MaintenanceRequestStatus.Completed;
                    var isCompletedNow = maintenanceRequest.Status == MaintenanceRequestStatus.Completed;
                    
                    // If status changed to Completed, set completion date if not already set
                    if (statusChanged && isCompletedNow && !maintenanceRequest.CompletedAt.HasValue)
                    {
                        maintenanceRequest.CompletedAt = DateTime.Now;
                    }
                    
                    // Update the entity
                    _context.Update(maintenanceRequest);
                    
                    // Handle room availability status
                    if (maintenanceRequest.Priority == 1)
                    {
                        var room = await _context.Rooms.FindAsync(maintenanceRequest.RoomId);
                        if (room != null)
                        {
                            // Only update room status if status changed to completed
                            if (statusChanged && isCompletedNow)
                            {
                                room.AvailabilityStatus = AvailabilityStatus.Available;
                            }
                            // Or if it was changed from completed to something else
                            else if (statusChanged && wasCompletedBefore && !isCompletedNow)
                            {
                                room.AvailabilityStatus = AvailabilityStatus.Maintenance;
                            }
                        }
                    }
                    
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Maintenance request updated successfully";
                    return RedirectToAction(nameof(Details), new { id = maintenanceRequest.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MaintenanceRequestExists(maintenanceRequest.Id))
                    {
                        TempData["ErrorMessage"] = "Maintenance request not found";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Concurrency conflict. The record was modified by another user.";
                        return RedirectToAction(nameof(Edit), new { id = maintenanceRequest.Id });
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Error updating maintenance request: " + ex.Message;
                }
            }
            
            // If we got this far, something failed, redisplay form
            ViewBag.RoomId = new SelectList(_context.Rooms, "RoomId", "RoomNumber", maintenanceRequest.RoomId);
            ViewBag.ReportedById = new SelectList(_context.Users, "UserId", "FullName", maintenanceRequest.ReportedById);
            ViewBag.TechnicianId = new SelectList(_context.Users.Where(u => u.UserType == UserType.Staff), "UserId", "FullName", maintenanceRequest.TechnicianId);
            ViewBag.IssueTypes = new SelectList(Enum.GetValues(typeof(MaintenanceIssueType)), maintenanceRequest.IssueType);
            ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(MaintenanceRequestStatus)), maintenanceRequest.Status);
            
            ViewBag.Priorities = new SelectList(new[]
            {
                new { Value = 1, Text = "Critical" },
                new { Value = 2, Text = "High" },
                new { Value = 3, Text = "Medium" },
                new { Value = 4, Text = "Low" }
            }, "Value", "Text", maintenanceRequest.Priority);
            
            return View(maintenanceRequest);
        }

        // GET: Admin/Maintenance/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Try to find by Id first, then by RequestId if needed
            var maintenanceRequest = await _context.MaintenanceRequests
                .Include(m => m.Room)
                .Include(m => m.ReportedBy)
                .Include(m => m.AssignedTechnician)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (maintenanceRequest == null)
            {
                // Try with RequestId
                maintenanceRequest = await _context.MaintenanceRequests
                    .Include(m => m.Room)
                    .Include(m => m.ReportedBy)
                    .Include(m => m.AssignedTechnician)
                    .FirstOrDefaultAsync(m => m.RequestId == id);
            }

            if (maintenanceRequest == null)
            {
                TempData["ErrorMessage"] = $"Maintenance request with ID {id} not found";
                return RedirectToAction(nameof(Index));
            }

            return View(maintenanceRequest);
        }

        // POST: Admin/Maintenance/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try 
            {
                // Get room information before deletion
                int? roomId = null;
                int? priority = null;
                
                // Find the request using direct SQL to avoid tracking issues
                string sql = "SELECT Id, RoomId, Priority FROM MaintenanceRequests WHERE Id = @id OR RequestId = @id";
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@id";
                    parameter.Value = id;
                    command.Parameters.Add(parameter);
                    
                    // Ensure connection is open
                    if (command.Connection.State != System.Data.ConnectionState.Open)
                    {
                        command.Connection.Open();
                    }
                    
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            roomId = reader.GetInt32(1);
                            priority = reader.GetInt32(2);
                        }
                    }
                }
                
                if (!roomId.HasValue)
                {
                    TempData["ErrorMessage"] = "Maintenance request not found";
                    return RedirectToAction(nameof(Index));
                }
                
                // If it's a critical request, update room status
                if (priority == 1)
                {
                    // Update room availability using separate command
                    string updateRoomSql = "UPDATE Rooms SET AvailabilityStatus = 0 WHERE RoomId = @roomId";
                    using (var command = _context.Database.GetDbConnection().CreateCommand())
                    {
                        command.CommandText = updateRoomSql;
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = "@roomId";
                        parameter.Value = roomId.Value;
                        command.Parameters.Add(parameter);
                        
                        // Ensure connection is open
                        if (command.Connection.State != System.Data.ConnectionState.Open)
                        {
                            command.Connection.Open();
                        }
                        
                        await command.ExecuteNonQueryAsync();
                    }
                }
                
                // Delete the maintenance request using direct SQL
                string deleteSql = "DELETE FROM MaintenanceRequests WHERE Id = @id OR RequestId = @id";
                int rowsAffected = 0;
                
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = deleteSql;
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@id";
                    parameter.Value = id;
                    command.Parameters.Add(parameter);
                    
                    // Ensure connection is open
                    if (command.Connection.State != System.Data.ConnectionState.Open)
                    {
                        command.Connection.Open();
                    }
                    
                    rowsAffected = await command.ExecuteNonQueryAsync();
                }
                
                if (rowsAffected > 0)
                {
                    TempData["SuccessMessage"] = "Maintenance request deleted successfully";
                }
                else
                {
                    TempData["ErrorMessage"] = "The maintenance request could not be found or was already deleted";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting maintenance request: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        // POST: Admin/Maintenance/MarkComplete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkComplete(int id, string resolution, decimal? costOfRepair)
        {
            var maintenanceRequest = await _context.MaintenanceRequests.FindAsync(id);
            if (maintenanceRequest == null)
            {
                // Try finding by RequestId if not found by Id
                maintenanceRequest = await _context.MaintenanceRequests.FirstOrDefaultAsync(m => m.RequestId == id);
            }
            
            if (maintenanceRequest == null)
            {
                TempData["ErrorMessage"] = "Maintenance request not found";
                return RedirectToAction(nameof(Index));
            }
            
            try
            {
                maintenanceRequest.Status = MaintenanceRequestStatus.Completed;
                maintenanceRequest.CompletedAt = DateTime.Now;
                maintenanceRequest.Resolution = resolution;
                maintenanceRequest.CostOfRepair = costOfRepair;
                
                // Update room status back to available if this was a critical priority issue
                if (maintenanceRequest.Priority == 1)
                {
                    var room = await _context.Rooms.FindAsync(maintenanceRequest.RoomId);
                    if (room != null)
                    {
                        room.AvailabilityStatus = AvailabilityStatus.Available;
                    }
                }
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Maintenance request marked as completed";
                return RedirectToAction(nameof(Details), new { id = maintenanceRequest.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error completing maintenance request: " + ex.Message;
                return RedirectToAction(nameof(Details), new { id = maintenanceRequest.Id });
            }
        }
        
        // GET: Admin/Maintenance/ExportToExcel
        public async Task<IActionResult> ExportToExcel()
        {
            var maintenanceRequests = await _context.MaintenanceRequests
                .Include(m => m.Room)
                .Include(m => m.ReportedBy)
                .Include(m => m.AssignedTechnician)
                .OrderByDescending(m => m.ReportedAt)
                .ToListAsync();
            
            // In a real application, this would generate and return an Excel file
            // For now we'll just redirect with a message
            TempData["SuccessMessage"] = "Export functionality will be implemented soon.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Maintenance/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, MaintenanceRequestStatus status)
        {
            var maintenanceRequest = await _context.MaintenanceRequests.FindAsync(id);
            if (maintenanceRequest == null)
            {
                // Try finding by RequestId if not found by Id
                maintenanceRequest = await _context.MaintenanceRequests.FirstOrDefaultAsync(m => m.RequestId == id);
            }
            
            if (maintenanceRequest == null)
            {
                TempData["ErrorMessage"] = "Maintenance request not found";
                return RedirectToAction(nameof(Index));
            }
            
            try
            {
                maintenanceRequest.Status = status;
                
                // Set completion date if status is now Completed
                if (status == MaintenanceRequestStatus.Completed)
                {
                    maintenanceRequest.CompletedAt = DateTime.Now;
                    
                    // Update room status back to available if this was a critical priority issue
                    if (maintenanceRequest.Priority == 1)
                    {
                        var room = await _context.Rooms.FindAsync(maintenanceRequest.RoomId);
                        if (room != null)
                        {
                            room.AvailabilityStatus = AvailabilityStatus.Available;
                        }
                    }
                }
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Status updated to {status}";
                return RedirectToAction(nameof(Details), new { id = maintenanceRequest.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating status: " + ex.Message;
                return RedirectToAction(nameof(Details), new { id = maintenanceRequest.Id });
            }
        }

        // POST: Admin/Maintenance/AssignTechnician/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTechnician(int id, int technicianId)
        {
            var maintenanceRequest = await _context.MaintenanceRequests.FindAsync(id);
            if (maintenanceRequest == null)
            {
                // Try finding by RequestId if not found by Id
                maintenanceRequest = await _context.MaintenanceRequests.FirstOrDefaultAsync(m => m.RequestId == id);
            }
            
            if (maintenanceRequest == null)
            {
                TempData["ErrorMessage"] = "Maintenance request not found";
                return RedirectToAction(nameof(Index));
            }
            
            try
            {
                maintenanceRequest.TechnicianId = technicianId;
                
                // Update status to scheduled if it was reported
                if (maintenanceRequest.Status == MaintenanceRequestStatus.Reported)
                {
                    maintenanceRequest.Status = MaintenanceRequestStatus.Scheduled;
                }
                
                await _context.SaveChangesAsync();
                
                var technician = await _context.Users.FindAsync(technicianId);
                TempData["SuccessMessage"] = $"Technician {technician?.FullName ?? "Unknown"} assigned successfully";
                return RedirectToAction(nameof(Details), new { id = maintenanceRequest.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error assigning technician: " + ex.Message;
                return RedirectToAction(nameof(Details), new { id = maintenanceRequest.Id });
            }
        }

        // GET: Admin/Maintenance/ViewRequest/5 - For debugging
        public async Task<IActionResult> ViewRequest(int? id)
        {
            if (id == null)
            {
                return Content("No ID provided");
            }

            // Try all possible ways to find the maintenance request
            var requestById = await _context.MaintenanceRequests.FindAsync(id);
            var requestByRequestId = await _context.MaintenanceRequests.FirstOrDefaultAsync(m => m.RequestId == id);
            
            var result = new StringBuilder();
            result.AppendLine($"<h3>Looking for maintenance request with ID: {id}</h3>");
            result.AppendLine($"<p><strong>Found by Id:</strong> {(requestById != null ? "Yes" : "No")}</p>");
            result.AppendLine($"<p><strong>Found by RequestId:</strong> {(requestByRequestId != null ? "Yes" : "No")}</p>");
            
            if (requestById != null)
            {
                result.AppendLine("<h4>Request found by Id:</h4>");
                result.AppendLine("<ul>");
                result.AppendLine($"<li><strong>Id:</strong> {requestById.Id}</li>");
                result.AppendLine($"<li><strong>RequestId:</strong> {requestById.RequestId}</li>");
                result.AppendLine($"<li><strong>Title:</strong> {requestById.Title}</li>");
                result.AppendLine($"<li><strong>Status:</strong> {requestById.Status}</li>");
                result.AppendLine($"<li><strong>Priority:</strong> {requestById.Priority}</li>");
                result.AppendLine($"<li><strong>RoomId:</strong> {requestById.RoomId}</li>");
                result.AppendLine("</ul>");
            }
            
            if (requestByRequestId != null)
            {
                result.AppendLine("<h4>Request found by RequestId:</h4>");
                result.AppendLine("<ul>");
                result.AppendLine($"<li><strong>Id:</strong> {requestByRequestId.Id}</li>");
                result.AppendLine($"<li><strong>RequestId:</strong> {requestByRequestId.RequestId}</li>");
                result.AppendLine($"<li><strong>Title:</strong> {requestByRequestId.Title}</li>");
                result.AppendLine($"<li><strong>Status:</strong> {requestByRequestId.Status}</li>");
                result.AppendLine($"<li><strong>Priority:</strong> {requestByRequestId.Priority}</li>");
                result.AppendLine($"<li><strong>RoomId:</strong> {requestByRequestId.RoomId}</li>");
                result.AppendLine("</ul>");
            }
            
            result.AppendLine("<h3>All Maintenance Requests in DB</h3>");
            result.AppendLine("<table border='1' cellpadding='5'>");
            result.AppendLine("<tr><th>Id</th><th>RequestId</th><th>Title</th><th>Status</th><th>Room</th></tr>");
            
            var allRequests = await _context.MaintenanceRequests
                .Include(m => m.Room)
                .OrderByDescending(m => m.Id)
                .Take(10)
                .ToListAsync();
                
            foreach (var req in allRequests)
            {
                result.AppendLine("<tr>");
                result.AppendLine($"<td>{req.Id}</td>");
                result.AppendLine($"<td>{req.RequestId}</td>");
                result.AppendLine($"<td>{req.Title}</td>");
                result.AppendLine($"<td>{req.Status}</td>");
                result.AppendLine($"<td>{req.Room?.RoomNumber ?? "Unknown"}</td>");
                result.AppendLine("</tr>");
            }
            
            result.AppendLine("</table>");
            
            result.AppendLine("<p><a href='/Admin/Maintenance'>Return to maintenance list</a></p>");
            
            return Content(result.ToString(), "text/html");
        }

        private bool MaintenanceRequestExists(int id)
        {
            return _context.MaintenanceRequests.Any(e => e.Id == id);
        }
    }
} 