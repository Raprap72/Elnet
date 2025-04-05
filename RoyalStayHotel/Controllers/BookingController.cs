using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RoyalStayHotel.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Redirect to the unified Rooms/Booking page
            return RedirectToAction("Index", "Rooms");
        }

        public async Task<IActionResult> Book(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }
            
            // Redirect to the unified Rooms/Booking page with a query parameter to open the booking modal
            return RedirectToAction("Index", "Rooms", new { bookRoomId = id });
        }

        [HttpPost]
        public async Task<IActionResult> Book(int roomId, DateTime checkIn, DateTime checkOut, int numberOfGuests = 1)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null)
            {
                return NotFound();
            }

            // In a real app, we would check if the user is logged in
            // and get their ID from the session or claims
            int userId = 1; // Dummy user ID - would be replaced with authenticated user

            // Ensure user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                // For demo purposes, create a guest user if none exists
                user = new User
                {
                    FullName = "Guest User",
                    Email = "guest@example.com",
                    Username = "guest",
                    Password = "Guest123!", // In production, use a password hasher
                    UserType = UserType.Customer,
                    CreatedAt = DateTime.Now
                };
                
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                userId = user.UserId;
            }

            // Calculate the number of days
            int days = (int)(checkOut - checkIn).TotalDays;
            if (days <= 0)
            {
                ModelState.AddModelError("", "Check-out date must be after check-in date.");
                ViewBag.Room = room;
                return View();
            }

            // Calculate total price
            decimal totalPrice = room.PricePerNight * days;

            var booking = new Booking
            {
                UserId = userId,
                RoomId = roomId,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                NumberOfGuests = numberOfGuests,
                TotalPrice = totalPrice,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.Now
            };

            // Add to database
            _context.Bookings.Add(booking);
            
            // Update room availability
            room.AvailabilityStatus = AvailabilityStatus.Booked;
            _context.Rooms.Update(room);
            
            await _context.SaveChangesAsync();

            return RedirectToAction("Confirmation", new { id = booking.BookingId });
        }

        public async Task<IActionResult> Confirmation(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.BookingId == id);
                
            if (booking == null)
            {
                return NotFound();
            }
            
            return View(booking);
        }

        public async Task<IActionResult> MyBookings()
        {
            // In a real app, we would get the user ID from the session or claims
            int userId = 1; // Dummy user ID
            
            var userBookings = await _context.Bookings
                .Include(b => b.Room)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
                
            return View(userBookings);
        }
    }
} 