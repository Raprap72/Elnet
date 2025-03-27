using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

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
        public async Task<IActionResult> Index(string sortOrder, string searchString, string currentFilter, int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["StatusSortParm"] = sortOrder == "Status" ? "status_desc" : "Status";
            
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
                .AsQueryable();
                           
            if (!string.IsNullOrEmpty(searchString))
            {
                bookings = bookings.Where(b => 
                    (b.User != null && b.User.FullName.Contains(searchString)) ||
                    (b.User != null && b.User.Email.Contains(searchString)) ||
                    (b.Room != null && b.Room.RoomType.ToString().Contains(searchString)) ||
                    b.Status.ToString().Contains(searchString));
            }
            
            bookings = sortOrder switch
            {
                "name_desc" => bookings.OrderByDescending(b => b.User != null ? b.User.FullName : string.Empty),
                "Date" => bookings.OrderBy(b => b.CheckInDate),
                "date_desc" => bookings.OrderByDescending(b => b.CheckInDate),
                "Status" => bookings.OrderBy(b => b.Status),
                "status_desc" => bookings.OrderByDescending(b => b.Status),
                _ => bookings.OrderBy(b => b.User != null ? b.User.FullName : string.Empty),
            };
            
            int pageSize = 10;
            return View(await PaginatedList<Booking>.CreateAsync(bookings.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: Admin/Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                .Include(b => b.BookedServices)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(m => m.BookingId == id);
                
            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // GET: Admin/Bookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(m => m.BookingId == id);
                
            if (booking == null)
            {
                return NotFound();
            }
            
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", booking.UserId);
            ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "RoomType", booking.RoomId);
            ViewData["BookingStatuses"] = Enum.GetValues(typeof(BookingStatus))
                .Cast<BookingStatus>()
                .Select(s => new SelectListItem
                {
                    Value = s.ToString(),
                    Text = s.ToString()
                }).ToList();
                
            return View(booking);
        }

        // POST: Admin/Bookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BookingId,UserId,RoomId,CheckInDate,CheckOutDate,NumberOfGuests,TotalPrice,Status,CreatedAt")] Booking booking)
        {
            if (id != booking.BookingId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                    
                    // If booking status changed to Confirmed, update room availability
                    if (booking.Status == BookingStatus.Confirmed)
                    {
                        var room = await _context.Rooms.FindAsync(booking.RoomId);
                        if (room != null)
                        {
                            room.AvailabilityStatus = AvailabilityStatus.Booked;
                            _context.Update(room);
                            await _context.SaveChangesAsync();
                        }
                    }
                    // If booking status changed to Cancelled, update room availability
                    else if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.CheckedOut)
                    {
                        var room = await _context.Rooms.FindAsync(booking.RoomId);
                        if (room != null)
                        {
                            room.AvailabilityStatus = AvailabilityStatus.Available;
                            _context.Update(room);
                            await _context.SaveChangesAsync();
                        }
                    }
                    
                    TempData["SuccessMessage"] = "Booking updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingExists(booking.BookingId))
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
            
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", booking.UserId);
            ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "RoomType", booking.RoomId);
            ViewData["BookingStatuses"] = Enum.GetValues(typeof(BookingStatus))
                .Cast<BookingStatus>()
                .Select(s => new SelectListItem
                {
                    Value = s.ToString(),
                    Text = s.ToString()
                }).ToList();
                
            return View(booking);
        }

        // GET: Admin/Bookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
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
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(m => m.BookingId == id);
                
            if (booking == null)
            {
                return NotFound();
            }
            
            // Check if there are any payments
            if (booking.Payments != null && booking.Payments.Count > 0)
            {
                TempData["ErrorMessage"] = "Cannot delete booking as it has existing payments.";
                return RedirectToAction(nameof(Index));
            }
            
            // Remove any booked services
            if (booking.BookedServices != null && booking.BookedServices.Count > 0)
            {
                _context.BookedServices.RemoveRange(booking.BookedServices);
            }
            
            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Booking deleted successfully!";
            
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Bookings/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, BookingStatus status)
        {
            var booking = await _context.Bookings.FindAsync(id);
            
            if (booking == null)
            {
                return NotFound();
            }
            
            booking.Status = status;
            
            // If status is CheckedOut or Cancelled, make the room available
            if (status == BookingStatus.CheckedOut || status == BookingStatus.Cancelled)
            {
                var room = await _context.Rooms.FindAsync(booking.RoomId);
                if (room != null && !room.IsAvailable)
                {
                    room.IsAvailable = true;
                }
            }
            // If status is CheckedIn, make sure room is marked as unavailable
            else if (status == BookingStatus.CheckedIn)
            {
                var room = await _context.Rooms.FindAsync(booking.RoomId);
                if (room != null && room.IsAvailable)
                {
                    room.IsAvailable = false;
                }
            }
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Booking status updated to {status}.";
            
            return RedirectToAction(nameof(Details), new { id });
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