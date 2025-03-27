using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;

namespace RoyalStayHotel.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class RoomController : AdminBaseController
    {
        private readonly ApplicationDbContext _context;

        public RoomController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Room
        public async Task<IActionResult> Index()
        {
            var rooms = await _context.Rooms.ToListAsync();
            return View(rooms);
        }

        // GET: Admin/Room/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(m => m.RoomId == id);

            if (room == null)
            {
                return NotFound();
            }

            // Get booking history for this room
            ViewBag.BookingHistory = await _context.Bookings
                .Where(b => b.RoomId == id)
                .Include(b => b.User)
                .OrderByDescending(b => b.CheckInDate)
                .Take(5)
                .ToListAsync();

            return View(room);
        }

        // GET: Admin/Room/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Room/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoomNumber,RoomType,Description,Price,Capacity,ImageUrl,IsAvailable")] Room room)
        {
            if (ModelState.IsValid)
            {
                // Check if room number already exists
                var existingRoom = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomNumber == room.RoomNumber);
                if (existingRoom != null)
                {
                    ModelState.AddModelError("RoomNumber", "A room with this number already exists.");
                    return View(room);
                }

                _context.Add(room);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Room created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(room);
        }

        // GET: Admin/Room/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }
            return View(room);
        }

        // POST: Admin/Room/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RoomId,RoomNumber,RoomType,Description,Price,Capacity,ImageUrl,IsAvailable")] Room room)
        {
            if (id != room.RoomId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if room number already exists (excluding current room)
                    var existingRoom = await _context.Rooms
                        .FirstOrDefaultAsync(r => r.RoomNumber == room.RoomNumber && r.RoomId != room.RoomId);
                        
                    if (existingRoom != null)
                    {
                        ModelState.AddModelError("RoomNumber", "A room with this number already exists.");
                        return View(room);
                    }

                    _context.Update(room);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Room updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoomExists(room.RoomId))
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
            return View(room);
        }

        // POST: Admin/Room/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            
            // Check if room has bookings
            var hasBookings = await _context.Bookings.AnyAsync(b => b.RoomId == id);
            if (hasBookings)
            {
                TempData["ErrorMessage"] = "Cannot delete room as it has existing bookings.";
                return RedirectToAction(nameof(Index));
            }
            
            if (room != null)
            {
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Room deleted successfully!";
            }
            
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Room/ToggleAvailability/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailability(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            
            if (room == null)
            {
                return NotFound();
            }
            
            // Check if room has active bookings before marking as unavailable
            if (room.IsAvailable)
            {
                var hasActiveBookings = await _context.Bookings
                    .AnyAsync(b => b.RoomId == id && 
                                   b.Status != BookingStatus.Cancelled && 
                                   b.Status != BookingStatus.CheckedOut &&
                                   b.CheckOutDate >= DateTime.Now);
                                   
                if (hasActiveBookings)
                {
                    TempData["WarningMessage"] = "This room has active bookings. Please reassign or cancel these bookings before marking the room as unavailable.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }
            
            // Toggle availability
            room.IsAvailable = !room.IsAvailable;
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = room.IsAvailable 
                ? "Room marked as available." 
                : "Room marked as unavailable.";
                
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Admin/Room/RoomAvailability
        public async Task<IActionResult> RoomAvailability()
        {
            var rooms = await _context.Rooms.ToListAsync();
            var availableRooms = rooms.Count(r => r.IsAvailable);
            var occupiedRooms = rooms.Count - availableRooms;

            ViewBag.AvailableRooms = availableRooms;
            ViewBag.OccupiedRooms = occupiedRooms;
            ViewBag.TotalRooms = rooms.Count;

            // Get room type counts for the bar chart
            var roomTypeCount = rooms
                .GroupBy(r => r.RoomType)
                .Select(g => new { RoomType = g.Key, Count = g.Count() })
                .OrderBy(x => x.RoomType)
                .ToList();

            // Prepare data for the bar chart
            var roomTypes = new List<string>();
            var roomCounts = new List<int>();

            foreach (var item in roomTypeCount)
            {
                roomTypes.Add(item.RoomType.ToString());
                roomCounts.Add(item.Count);
            }

            ViewBag.RoomTypes = roomTypes;
            ViewBag.RoomCounts = roomCounts;

            return View();
        }

        // GET: Admin/Room/GetRoomDetails
        [HttpGet]
        public async Task<IActionResult> GetRoomDetails(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            
            if (room == null)
            {
                return NotFound();
            }
            
            var roomDetails = new
            {
                roomId = room.RoomId,
                roomNumber = room.RoomNumber,
                roomType = room.RoomType,
                description = room.Description,
                price = room.Price,
                capacity = room.Capacity,
                isAvailable = room.IsAvailable,
                imageUrl = room.ImageUrl
            };
            
            return Json(roomDetails);
        }

        private bool RoomExists(int id)
        {
            return _context.Rooms.Any(e => e.RoomId == id);
        }
    }
} 