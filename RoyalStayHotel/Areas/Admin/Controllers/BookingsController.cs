using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace RoyalStayHotel.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BookingsController : AdminBaseController
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Bookings
        public async Task<IActionResult> Index(string searchString, string currentFilter, int? pageNumber)
        {
            // Log debug info
            System.Diagnostics.Debug.WriteLine("Loading Index page for bookings");
            
            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            var bookings = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                .Include(b => b.Payments)
                .AsQueryable();

            // Log the number of bookings found
            var bookingCount = await bookings.CountAsync();
            System.Diagnostics.Debug.WriteLine($"Total bookings in database: {bookingCount}");
            
            if (bookingCount > 0)
            {
                // Get the first booking ID for diagnostic purposes
                var firstBookingId = await bookings.FirstOrDefaultAsync();
                if (firstBookingId != null)
                {
                    System.Diagnostics.Debug.WriteLine($"First booking ID: {firstBookingId.BookingId}");
                }
            }
            
            // Filter bookings if search string provided
            if (!String.IsNullOrEmpty(searchString))
            {
                bookings = bookings.Where(b => 
                    (b.User != null && b.User.FullName.Contains(searchString)) ||
                    (b.Room != null && b.Room.RoomType.ToString().Contains(searchString)) ||
                    b.BookingId.ToString().Contains(searchString));
            }

            // Order by booking date (newest first)
            bookings = bookings.OrderByDescending(b => b.CreatedAt);

            int pageSize = 10;
            return View(await PaginatedList<Booking>.CreateAsync(bookings.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: Admin/Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Booking ID is required.";
                return RedirectToAction(nameof(Index));
            }

            // Add logging to diagnose the issue
            System.Diagnostics.Debug.WriteLine($"Looking for booking with ID: {id}");

            // Try to find booking by BookingId first
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                .Include(b => b.Payments)
                .Include(b => b.BookedServices)
                    .ThenInclude(bs => bs.Service)
                .FirstOrDefaultAsync(m => m.BookingId == id);
                
            if (booking == null)
            {
                // Try by the Id property if BookingId failed
                booking = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Room)
                    .Include(b => b.Payments)
                    .Include(b => b.BookedServices)
                        .ThenInclude(bs => bs.Service)
                    .FirstOrDefaultAsync(m => m.Id == id);
            }

            if (booking == null)
            {
                // Log more detailed error message
                System.Diagnostics.Debug.WriteLine($"Booking with ID {id} was not found in the database.");
                
                // Check if there are any bookings in the database
                var bookingCount = await _context.Bookings.CountAsync();
                System.Diagnostics.Debug.WriteLine($"Total bookings in database: {bookingCount}");
                
                if (bookingCount > 0)
                {
                    // Get the IDs of the first 10 bookings for diagnostic purposes
                    var bookingIds = await _context.Bookings.Take(10).Select(b => new { b.BookingId, b.Id, UserId = b.UserId }).ToListAsync();
                    System.Diagnostics.Debug.WriteLine($"First few booking IDs: {string.Join(", ", bookingIds.Select(b => $"BookingId: {b.BookingId}, Id: {b.Id}, UserId: {b.UserId}"))}");
                }
                
                TempData["ErrorMessage"] = $"Booking with ID {id} not found";
                return RedirectToAction(nameof(Index));
            }

            // Ensure user information is loaded
            if (booking.User == null && booking.UserId > 0)
            {
                booking.User = await _context.Users.FirstOrDefaultAsync(u => u.UserId == booking.UserId);
                if (booking.User != null)
                {
                    System.Diagnostics.Debug.WriteLine($"User information loaded for UserId: {booking.UserId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"User with ID {booking.UserId} not found in database.");
                }
            }

            // Get additional info for admin
            ViewBag.AllStatuses = Enum.GetValues(typeof(BookingStatus))
                .Cast<BookingStatus>()
                .Select(s => new SelectListItem
                {
                    Value = s.ToString(),
                    Text = s.ToString()
                }).ToList();

            return View(booking);
        }

        // GET: Admin/Bookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Booking ID is required.";
                return RedirectToAction(nameof(Index));
            }

            // Add logging to diagnose the issue
            System.Diagnostics.Debug.WriteLine($"Loading Edit view for booking with ID: {id}");

            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(m => m.BookingId == id);
                
            if (booking == null)
            {
                // Try by the Id property if BookingId failed
                booking = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Room)
                    .Include(b => b.Payments)
                    .FirstOrDefaultAsync(m => m.Id == id);
            }

            if (booking == null)
            {
                TempData["ErrorMessage"] = $"Booking with ID {id} not found.";
                return RedirectToAction(nameof(Index));
            }
            
            // Load ViewBag data for dropdowns
            ViewBag.UserId = new SelectList(_context.Users, "UserId", "FullName", booking.UserId);
            ViewBag.RoomId = new SelectList(_context.Rooms, "RoomId", "RoomNumber", booking.RoomId);
            ViewBag.BookingStatuses = new SelectList(Enum.GetValues(typeof(BookingStatus))
                .Cast<BookingStatus>()
                .Select(s => new { Value = s.ToString(), Text = s.ToString() }), 
                "Value", "Text", booking.Status);
                
            return View(booking);
        }

        // POST: Admin/Bookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BookingId,Status")] Booking booking)
        {
            System.Diagnostics.Debug.WriteLine($"Edit POST called with id: {id}, BookingId: {booking.BookingId}, Status: {booking.Status}");
            
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Invalid booking ID.";
                return RedirectToAction(nameof(Index));
            }

            // Get the actual booking from database
            var existingBooking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.User)
                .FirstOrDefaultAsync(m => m.BookingId == id || m.Id == id);
                
            if (existingBooking == null)
            {
                TempData["ErrorMessage"] = $"Booking with ID {id} not found.";
                return RedirectToAction(nameof(Index));
            }
            
            // Use the correct booking ID for the update status call
            return RedirectToAction(nameof(UpdateStatus), new { id = existingBooking.BookingId, status = booking.Status });
        }

        // GET: Admin/Bookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Booking ID is required.";
                return RedirectToAction(nameof(Index));
            }

            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(m => m.BookingId == id);
                
            if (booking == null)
            {
                TempData["ErrorMessage"] = $"Booking with ID {id} not found.";
                return RedirectToAction(nameof(Index));
            }

            // Check if booking has payments
            if (booking.Payments != null && booking.Payments.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete booking with existing payments. Please refund or void the payments first.";
                return RedirectToAction(nameof(Details), new { id = booking.BookingId });
            }

            return View(booking);
        }

        // POST: Admin/Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Payments)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(m => m.BookingId == id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction(nameof(Index));
            }

            // Double check if booking has payments
            if (booking.Payments != null && booking.Payments.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete booking with existing payments. Please refund or void the payments first.";
                return RedirectToAction(nameof(Index));
            }

            // If booking was confirmed or checked in, free up the room's availability
            if (booking.Status == BookingStatus.Confirmed || booking.Status == BookingStatus.CheckedIn)
            {
                var room = booking.Room;
                if (room != null)
                {
                    room.IsAvailable = true;
                    _context.Update(room);
                }
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Booking #{id} has been successfully deleted.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Bookings/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, BookingStatus status)
        {
            // Debug logging
            System.Diagnostics.Debug.WriteLine($"********* STARTING UpdateStatus *********");
            System.Diagnostics.Debug.WriteLine($"UpdateStatus called with id: {id}, new status: {status}");
            
            if (id <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"*** ERROR: Invalid booking ID: {id}");
                TempData["ErrorMessage"] = $"Invalid booking ID: {id}";
                return RedirectToAction(nameof(Index));
            }
            
            try 
            {
                // Direct database update using raw SQL to ensure the update happens regardless of tracking issues
                var sql = $"UPDATE Bookings SET Status = {(int)status} WHERE BookingId = {id} OR Id = {id}";
                System.Diagnostics.Debug.WriteLine($"Executing SQL: {sql}");
                
                // Execute the update and get actual rows affected
                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(sql);
                System.Diagnostics.Debug.WriteLine($"Status update complete - rows affected: {rowsAffected}");
                
                if (rowsAffected == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"*** ERROR: No rows affected by status update for ID {id}");
                    TempData["ErrorMessage"] = $"No booking found with ID {id}. Status update failed.";
                    return RedirectToAction(nameof(Index));
                }
                
                // Get the updated booking from the database
                var booking = await _context.Bookings
                    .Include(b => b.Room)
                    .Include(b => b.User)
                    .FirstOrDefaultAsync(m => m.BookingId == id || m.Id == id);
                
                if (booking == null)
                {
                    System.Diagnostics.Debug.WriteLine($"*** ERROR: Booking with ID {id} not found after status update");
                    TempData["ErrorMessage"] = "Status was updated, but couldn't retrieve the booking details.";
                    return RedirectToAction(nameof(Index));
                }
                
                System.Diagnostics.Debug.WriteLine($"Retrieved booking: ID={booking.BookingId}, Status={booking.Status}");
                
                // Update room availability based on the new status
                var room = booking.Room;
                if (room != null)
                {
                    bool shouldBeAvailable = false;
                    
                    // Determine if the room should be available
                    if (status == BookingStatus.Confirmed || status == BookingStatus.CheckedIn)
                    {
                        // If booking is confirmed or checked in, mark room as unavailable
                        shouldBeAvailable = false;
                    }
                    else if (status == BookingStatus.Declined || status == BookingStatus.Cancelled || 
                             status == BookingStatus.CheckedOut || status == BookingStatus.NoShow)
                    {
                        // If booking is declined/cancelled/checked-out/no-show, mark room as available
                        shouldBeAvailable = true;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Room availability check - Room ID: {room.RoomId}, Current: {room.IsAvailable}, Should be: {shouldBeAvailable}");
                    
                    // Update room availability if needed
                    if (room.IsAvailable != shouldBeAvailable)
                    {
                        var roomUpdateSql = $"UPDATE Rooms SET IsAvailable = '{shouldBeAvailable}' WHERE RoomId = {room.RoomId}";
                        await _context.Database.ExecuteSqlRawAsync(roomUpdateSql);
                        System.Diagnostics.Debug.WriteLine($"Room #{room.RoomNumber} availability updated to {shouldBeAvailable}");
                    }
                }
                
                // Set appropriate success message based on the status change
                var guestName = booking.User?.FullName ?? "Guest";
                string successMessage = $"Booking status for {guestName} updated to {status}";
                
                System.Diagnostics.Debug.WriteLine($"SUCCESS: {successMessage}");
                TempData["SuccessMessage"] = successMessage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"*** ERROR updating database: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"An error occurred while updating the booking status: {ex.Message}";
            }

            System.Diagnostics.Debug.WriteLine($"********* ENDING UpdateStatus *********");
            
            // Redirect back to the booking details page
            return RedirectToAction(nameof(Details), new { id = id });
        }

        // POST: Admin/Bookings/DirectUpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DirectUpdateStatus(int id, int status)
        {
            try
            {
                // Convert to BookingStatus enum
                var bookingStatus = (BookingStatus)status;
                
                // Execute direct update using ADO.NET for maximum reliability
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_context.Database.GetConnectionString()))
                {
                    connection.Open();
                    
                    // Update booking status directly
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "UPDATE Bookings SET Status = @status WHERE BookingId = @id";
                        command.Parameters.AddWithValue("@status", (int)bookingStatus);
                        command.Parameters.AddWithValue("@id", id);
                        
                        var rowsAffected = command.ExecuteNonQuery();
                        
                        if (rowsAffected == 0)
                        {
                            // Try with Id field
                            command.Parameters.Clear();
                            command.CommandText = "UPDATE Bookings SET Status = @status WHERE Id = @id";
                            command.Parameters.AddWithValue("@status", (int)bookingStatus);
                            command.Parameters.AddWithValue("@id", id);
                            rowsAffected = command.ExecuteNonQuery();
                            
                            if (rowsAffected == 0)
                            {
                                return Json(new { success = false, message = "Booking not found" });
                            }
                        }
                    }
                    
                    // Get the room details for availability update
                    int? roomId = null;
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT RoomId FROM Bookings WHERE BookingId = @id OR Id = @id";
                        command.Parameters.AddWithValue("@id", id);
                        
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                roomId = reader.IsDBNull(0) ? null : (int?)reader.GetInt32(0);
                            }
                        }
                    }
                    
                    // Update room availability if needed
                    if (roomId.HasValue)
                    {
                        bool shouldBeAvailable = false;
                        
                        // Determine if the room should be available
                        if (bookingStatus == BookingStatus.Confirmed || bookingStatus == BookingStatus.CheckedIn)
                        {
                            // Room should be unavailable
                            shouldBeAvailable = false;
                        }
                        else if (bookingStatus == BookingStatus.Declined || bookingStatus == BookingStatus.Cancelled || 
                                 bookingStatus == BookingStatus.CheckedOut || bookingStatus == BookingStatus.NoShow)
                        {
                            // Room should be available
                            shouldBeAvailable = true;
                        }
                        
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "UPDATE Rooms SET IsAvailable = @isAvailable WHERE RoomId = @roomId";
                            command.Parameters.AddWithValue("@isAvailable", shouldBeAvailable);
                            command.Parameters.AddWithValue("@roomId", roomId.Value);
                            command.ExecuteNonQuery();
                        }
                    }
                }
                
                // Set success message
                TempData["SuccessMessage"] = $"Booking status has been updated to {(BookingStatus)status}";
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error updating status: {ex.Message}" });
            }
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.BookingId == id);
        }
    }

    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);

            this.AddRange(items);
        }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync();
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
} 