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
using System.Data;
using System.Data.Common;
using X.PagedList;
using Microsoft.Extensions.Logging;

namespace RoyalStayHotel.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BookingsController : AdminBaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BookingsController> _logger;

        public BookingsController(ApplicationDbContext context, ILogger<BookingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Bookings
        public async Task<IActionResult> Index(string searchString)
        {
            ViewBag.CurrentFilter = searchString;

            try
            {
                // Add detailed logging
                _logger.LogInformation($"Loading admin bookings page, search: {searchString ?? "none"}");
                
                // First check if we have any bookings at all
                var bookingCount = await _context.Bookings.CountAsync();
                _logger.LogInformation($"Total bookings in database: {bookingCount}");
                
                if (bookingCount == 0)
                {
                    // No bookings in database
                    return View(new List<Booking>());
                }
                
                // Load all bookings with related data using split queries for better performance
                var bookings = await _context.Bookings
                    .Include(b => b.Room)
                    .Include(b => b.User)
                    .Include(b => b.BookedServices)
                        .ThenInclude(bs => bs.Service)
                    .AsNoTracking()
                    .AsSplitQuery()
                    .ToListAsync();
                
                _logger.LogInformation($"Successfully loaded {bookings.Count} bookings from database");

                if (!String.IsNullOrEmpty(searchString))
                {
                    bookings = bookings.Where(b => 
                       (b.User != null && b.User.FullName != null && b.User.FullName.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                       (b.User != null && b.User.Email != null && b.User.Email.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                       (b.BookingReference != null && b.BookingReference.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                       (b.Room != null && b.Room.RoomNumber != null && b.Room.RoomNumber.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                return View(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading bookings data");
                TempData["ErrorMessage"] = "An error occurred while loading bookings data.";
                return View(new List<Booking>());
            }
        }

        // GET: Admin/Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Booking details requested with null ID");
                return NotFound();
            }

            try
            {
                _logger.LogInformation($"Loading details for booking ID: {id}");
                
                var booking = await _context.Bookings
                    .Include(b => b.Room)
                    .Include(b => b.User)
                    .Include(b => b.BookedServices)
                        .ThenInclude(bs => bs.Service)
                    .Include(b => b.Payments)
                    .AsSplitQuery()  // Use split queries for better performance with complex joins
                    .FirstOrDefaultAsync(m => m.BookingId == id);
                
                if (booking == null)
                {
                    _logger.LogWarning($"Booking with ID {id} not found");
                    return NotFound();
                }

                // Calculate payment summaries
                ViewBag.TotalPaid = booking.Payments?
                    .Where(p => p.Status == PaymentStatus.Completed)
                    .Sum(p => p.Amount) ?? 0;
                    
                ViewBag.Balance = booking.TotalPrice - ViewBag.TotalPaid;
                ViewBag.IsFullyPaid = ViewBag.Balance <= 0;
                
                _logger.LogInformation($"Successfully loaded booking {id} details. Payment status: Paid {ViewBag.TotalPaid:C2} of {booking.TotalPrice:C2}");

                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading booking details for ID {id}: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading booking details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Bookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.User)
                .Include(b => b.BookedServices)
                    .ThenInclude(bs => bs.Service)
                .FirstOrDefaultAsync(m => m.BookingId == id);
            
            if (booking == null)
            {
                return NotFound();
            }
            
            return View(booking);
        }

        // POST: Admin/Bookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BookingStatus status)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }
            
            booking.Status = status;
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Booking status updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, BookingStatus status)
        {
            try
            {
                _logger.LogInformation($"Attempting to update booking {id} status to {status}");
                
                var booking = await _context.Bookings.FindAsync(id);
                if (booking == null)
                {
                    _logger.LogWarning($"Booking with ID {id} not found");
                    return NotFound();
                }
                
                // Update the booking status
                booking.Status = status;
                
                // Save changes
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Successfully updated booking {id} status to {status}");
                TempData["SuccessMessage"] = $"Booking status updated to {status}!";
                
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating booking status: {ex.Message}");
                TempData["ErrorMessage"] = $"Failed to update status: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // GET: Admin/Bookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.User)
                .FirstOrDefaultAsync(m => m.BookingId == id);
            
            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // POST: Admin/Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookedServices)
                .FirstOrDefaultAsync(b => b.BookingId == id);
            
            if (booking == null)
            {
                return NotFound();
            }

            try
            {
                // First delete any associated booked services
                if (booking.BookedServices != null && booking.BookedServices.Any())
                {
                    _context.BookedServices.RemoveRange(booking.BookedServices);
                }
                
                // Then delete the booking itself
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Booking has been deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting booking: {ex.Message}";
                Console.WriteLine($"Error deleting booking: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
            
            return RedirectToAction(nameof(Index));
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

        // For in-memory collections (non-async)
        public static PaginatedList<T> Create(IEnumerable<T> source, int pageIndex, int pageSize)
        {
            var count = source.Count();
            var items = source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }

        // For EF Core IQueryable (async)
        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync();
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
} 