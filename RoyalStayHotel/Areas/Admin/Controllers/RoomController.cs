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
            
            // Load room type inventories for the integrated management
            var roomTypeInventories = await _context.RoomTypeInventories.ToListAsync();
            
            // If no inventory records exist yet, create them from the enum values
            if (!roomTypeInventories.Any())
            {
                var roomTypes = Enum.GetValues(typeof(RoomType)).Cast<RoomType>();
                foreach (var roomType in roomTypes)
                {
                    // Get count of existing rooms of this type
                    var count = await _context.Rooms.CountAsync(r => r.RoomType == roomType);
                    
                    var inventory = new RoomTypeInventory
                    {
                        RoomType = roomType,
                        TotalRooms = count,
                        Description = GetRoomTypeDescription(roomType)
                    };
                    
                    _context.RoomTypeInventories.Add(inventory);
                }
                await _context.SaveChangesAsync();
                
                // Reload the data
                roomTypeInventories = await _context.RoomTypeInventories.ToListAsync();
            }
            
            // Calculate available rooms for each type
            foreach (var inventory in roomTypeInventories)
            {
                inventory.AvailableRooms = await _context.Rooms.CountAsync(r => 
                    r.RoomType == inventory.RoomType && r.IsAvailable);
            }
            
            ViewBag.RoomTypeInventories = roomTypeInventories;
            
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
            
            // Get room type inventory information
            var roomTypeInventory = await _context.RoomTypeInventories
                .FirstOrDefaultAsync(rt => rt.RoomType == room.RoomType);
            
            if (roomTypeInventory != null)
            {
                roomTypeInventory.AvailableRooms = await _context.Rooms.CountAsync(r => 
                    r.RoomType == room.RoomType && r.IsAvailable);
                    
                ViewBag.RoomTypeInventory = roomTypeInventory;
            }

            return View(room);
        }

        // GET: Admin/Room/Create
        public async Task<IActionResult> Create()
        {
            // Get room type inventories for dropdown
            var roomTypeInventories = await _context.RoomTypeInventories.ToListAsync();
            
            // If no inventory records exist yet, create them from the enum values
            if (!roomTypeInventories.Any())
            {
                var roomTypes = Enum.GetValues(typeof(RoomType)).Cast<RoomType>();
                foreach (var roomType in roomTypes)
                {
                    // Get count of existing rooms of this type
                    var count = await _context.Rooms.CountAsync(r => r.RoomType == roomType);
                    
                    var inventory = new RoomTypeInventory
                    {
                        RoomType = roomType,
                        TotalRooms = count,
                        Description = GetRoomTypeDescription(roomType)
                    };
                    
                    _context.RoomTypeInventories.Add(inventory);
                }
                await _context.SaveChangesAsync();
                
                // Reload the data
                roomTypeInventories = await _context.RoomTypeInventories.ToListAsync();
            }
            
            // Calculate available rooms for each type
            foreach (var inventory in roomTypeInventories)
            {
                inventory.AvailableRooms = await _context.Rooms.CountAsync(r => 
                    r.RoomType == inventory.RoomType && r.IsAvailable);
            }
            
            ViewBag.RoomTypeInventories = roomTypeInventories;
            
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
                
                // Update room type inventory
                await UpdateRoomTypeInventoryCount(room.RoomType);
                
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
            
            // Check if room has active bookings
            bool hasActiveBookings = await _context.Bookings.AnyAsync(b => 
                b.RoomId == id && 
                (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn) &&
                b.CheckOutDate >= DateTime.Now);
            ViewBag.HasActiveBookings = hasActiveBookings;
            
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
                    // Get the current room to check if room type has changed
                    var currentRoom = await _context.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.RoomId == id);
                    var oldRoomType = currentRoom?.RoomType;
                    
                    _context.Update(room);
                    await _context.SaveChangesAsync();
                    
                    // Update room type inventory count if room type has changed
                    if (oldRoomType != null && oldRoomType != room.RoomType)
                    {
                        await UpdateRoomTypeInventoryCount(oldRoomType.Value);
                        await UpdateRoomTypeInventoryCount(room.RoomType);
                    }
                    
                    TempData["SuccessMessage"] = "Room updated successfully!";
                    return RedirectToAction(nameof(Index));
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
            }
            return View(room);
        }

        // GET: Admin/Room/Delete/5
        public async Task<IActionResult> Delete(int? id)
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

            // Check if the room has bookings
            bool hasBookings = await _context.Bookings.AnyAsync(b => b.RoomId == id);
            ViewBag.HasBookings = hasBookings;
            
            // Check if the room has active bookings
            bool hasActiveBookings = await _context.Bookings.AnyAsync(b => 
                b.RoomId == id && 
                (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn) &&
                b.CheckOutDate >= DateTime.Now);
            ViewBag.HasActiveBookings = hasActiveBookings;
            
            // Get booking count and last booking date
            ViewBag.BookingCount = await _context.Bookings.CountAsync(b => b.RoomId == id);
            
            var lastBooking = await _context.Bookings
                .Where(b => b.RoomId == id)
                .OrderByDescending(b => b.CheckOutDate)
                .FirstOrDefaultAsync();
            
            if (lastBooking != null)
            {
                ViewBag.LastBookingDate = lastBooking.CheckOutDate;
            }

            return View(room);
        }

        // POST: Admin/Room/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                var roomType = room.RoomType;
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();
                
                // Update room type inventory
                await UpdateRoomTypeInventoryCount(roomType);
                
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

        // GET: Admin/Room/RoomTypes
        public async Task<IActionResult> RoomTypes()
        {
            var roomTypeInventories = await _context.RoomTypeInventories.ToListAsync();
            
            // If no inventory records exist yet, create them from the enum values
            if (!roomTypeInventories.Any())
            {
                var roomTypes = Enum.GetValues(typeof(RoomType)).Cast<RoomType>();
                foreach (var roomType in roomTypes)
                {
                    // Get count of existing rooms of this type
                    var count = await _context.Rooms.CountAsync(r => r.RoomType == roomType);
                    
                    var inventory = new RoomTypeInventory
                    {
                        RoomType = roomType,
                        TotalRooms = count,
                        Description = GetRoomTypeDescription(roomType)
                    };
                    
                    _context.RoomTypeInventories.Add(inventory);
                }
                await _context.SaveChangesAsync();
                
                // Reload the data
                roomTypeInventories = await _context.RoomTypeInventories.ToListAsync();
            }
            
            // Calculate available rooms for each type
            foreach (var inventory in roomTypeInventories)
            {
                inventory.AvailableRooms = await _context.Rooms.CountAsync(r => 
                    r.RoomType == inventory.RoomType && r.IsAvailable);
            }
            
            return View(roomTypeInventories);
        }

        // GET: Admin/Room/EditRoomType/5
        public async Task<IActionResult> EditRoomType(int id)
        {
            var roomTypeInventory = await _context.RoomTypeInventories.FindAsync(id);
            if (roomTypeInventory == null)
            {
                return NotFound();
            }
            
            // Calculate available and occupied rooms
            roomTypeInventory.AvailableRooms = await _context.Rooms.CountAsync(r => 
                r.RoomType == roomTypeInventory.RoomType && r.IsAvailable);
            
            return View(roomTypeInventory);
        }

        // POST: Admin/Room/EditRoomType/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoomType(int id, [Bind("Id,RoomType,TotalRooms,Description")] RoomTypeInventory roomTypeInventory)
        {
            if (id != roomTypeInventory.Id)
            {
                return NotFound();
            }
            
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(roomTypeInventory);
                    await _context.SaveChangesAsync();
                    
                    // Update actual rooms based on inventory
                    await SyncRoomsWithInventory(roomTypeInventory.RoomType, roomTypeInventory.TotalRooms);
                    
                    TempData["SuccessMessage"] = "Room type updated successfully";
                    return RedirectToAction(nameof(RoomTypes));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoomTypeInventoryExists(roomTypeInventory.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            
            return View(roomTypeInventory);
        }

        // Helper method to get room type description
        private string GetRoomTypeDescription(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Standard => "Basic room with essential amenities, perfect for budget travelers",
                RoomType.Deluxe => "Spacious room with premium amenities and city views",
                RoomType.DeluxeSuite => "Luxury suite with separate living area and premium services",
                RoomType.ExecutiveDeluxe => "Executive-level accommodation with exclusive lounge access",
                RoomType.Presidential => "Our finest suite with maximum space, luxury amenities and butler service",
                _ => "Standard hotel room"
            };
        }

        private bool RoomTypeInventoryExists(int id)
        {
            return _context.RoomTypeInventories.Any(e => e.Id == id);
        }

        private bool RoomExists(int id)
        {
            return _context.Rooms.Any(e => e.RoomId == id);
        }

        // POST: Admin/Room/UpdateRoomTypeInventory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRoomTypeInventory(int id, int totalRooms)
        {
            try
            {
                var inventory = await _context.RoomTypeInventories.FindAsync(id);
                if (inventory == null)
                {
                    return Json(new { success = false, message = "Room type inventory not found" });
                }
                
                // Calculate available rooms (to determine if we can reduce the total count)
                var availableRooms = await _context.Rooms.CountAsync(r => 
                    r.RoomType == inventory.RoomType && r.IsAvailable);
                var occupiedRooms = await _context.Rooms.CountAsync(r => 
                    r.RoomType == inventory.RoomType && !r.IsAvailable);
                    
                // Ensure we don't set total rooms less than occupied rooms
                if (totalRooms < occupiedRooms)
                {
                    return Json(new { 
                        success = false, 
                        message = $"Cannot reduce total rooms below the number of currently occupied rooms ({occupiedRooms})" 
                    });
                }
                
                inventory.TotalRooms = totalRooms;
                await _context.SaveChangesAsync();
                
                // Sync actual rooms in the database
                await SyncRoomsWithInventory(inventory.RoomType, totalRooms);
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Helper method to update room type inventory count
        private async Task UpdateRoomTypeInventoryCount(RoomType roomType)
        {
            var inventory = await _context.RoomTypeInventories
                .FirstOrDefaultAsync(r => r.RoomType == roomType);
            
            if (inventory == null)
            {
                // Create inventory if it doesn't exist
                inventory = new RoomTypeInventory
                {
                    RoomType = roomType,
                    TotalRooms = 0,
                    Description = GetRoomTypeDescription(roomType)
                };
                _context.RoomTypeInventories.Add(inventory);
            }
            
            // Count actual rooms of this type
            var actualRoomCount = await _context.Rooms.CountAsync(r => r.RoomType == roomType);
            
            // Update the inventory count
            if (inventory.TotalRooms < actualRoomCount)
            {
                inventory.TotalRooms = actualRoomCount;
                await _context.SaveChangesAsync();
            }
        }

        // Helper method to sync rooms in the database with inventory
        private async Task SyncRoomsWithInventory(RoomType roomType, int totalRooms)
        {
            // Get current rooms of this type
            var existingRooms = await _context.Rooms
                .Where(r => r.RoomType == roomType)
                .ToListAsync();
            
            int currentCount = existingRooms.Count;
            
            // If we need to add more rooms
            if (totalRooms > currentCount)
            {
                int roomsToAdd = totalRooms - currentCount;
                for (int i = 0; i < roomsToAdd; i++)
                {
                    // Create a new room with auto-generated number
                    var newRoomNumber = GenerateRoomNumber(roomType, currentCount + i + 1);
                    var room = new Room
                    {
                        RoomNumber = newRoomNumber,
                        RoomType = roomType,
                        Description = GetRoomTypeDescription(roomType),
                        PricePerNight = GetDefaultPrice(roomType),
                        MaxGuests = GetDefaultCapacity(roomType),
                        BedType = GetDefaultBedType(roomType),
                        RoomSize = GetDefaultRoomSize(roomType),
                        AvailabilityStatus = AvailabilityStatus.Available,
                        IsAvailable = true,
                        ImageUrl = GetDefaultImageUrl(roomType)
                    };
                    
                    _context.Rooms.Add(room);
                }
                
                await _context.SaveChangesAsync();
            }
            // If we need to remove rooms
            else if (totalRooms < currentCount)
            {
                // Sort by available first, so we remove available rooms first
                var roomsToRemove = existingRooms
                    .Where(r => r.IsAvailable)
                    .OrderBy(r => r.RoomNumber)
                    .Take(currentCount - totalRooms)
                    .ToList();
                
                if (roomsToRemove.Any())
                {
                    _context.Rooms.RemoveRange(roomsToRemove);
                    await _context.SaveChangesAsync();
                }
            }
        }

        // Helper methods for room creation
        private string GenerateRoomNumber(RoomType roomType, int index)
        {
            string prefix = roomType switch
            {
                RoomType.Standard => "S",
                RoomType.Deluxe => "D",
                RoomType.DeluxeSuite => "DS",
                RoomType.ExecutiveDeluxe => "ED",
                RoomType.Presidential => "P",
                _ => "R"
            };
            
            return $"{prefix}{index:D3}";
        }

        private decimal GetDefaultPrice(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Standard => 1500,
                RoomType.Deluxe => 2500,
                RoomType.DeluxeSuite => 3500,
                RoomType.ExecutiveDeluxe => 4500,
                RoomType.Presidential => 10000,
                _ => 2000
            };
        }

        private int GetDefaultCapacity(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Standard => 2,
                RoomType.Deluxe => 2,
                RoomType.DeluxeSuite => 4,
                RoomType.ExecutiveDeluxe => 2,
                RoomType.Presidential => 6,
                _ => 2
            };
        }

        private string GetDefaultBedType(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Standard => "Twin Beds",
                RoomType.Deluxe => "Queen Bed",
                RoomType.DeluxeSuite => "King Bed",
                RoomType.ExecutiveDeluxe => "King Bed",
                RoomType.Presidential => "King Bed",
                _ => "Queen Bed"
            };
        }

        private string GetDefaultRoomSize(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Standard => "25 sq m",
                RoomType.Deluxe => "35 sq m",
                RoomType.DeluxeSuite => "50 sq m",
                RoomType.ExecutiveDeluxe => "45 sq m",
                RoomType.Presidential => "75 sq m",
                _ => "30 sq m"
            };
        }

        private string GetDefaultImageUrl(RoomType roomType)
        {
            return $"/images/{roomType.ToString().ToLower()}_room.png";
        }
    }
} 