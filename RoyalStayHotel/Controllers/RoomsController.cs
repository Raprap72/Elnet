using Microsoft.AspNetCore.Mvc;
using RoyalStayHotel.Models;
using System.Collections.Generic;
using System;
using RoyalStayHotel.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace RoyalStayHotel.Controllers
{
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        // Static rooms for display purposes
        private static List<Room> _rooms = new List<Room>
        {
            new Room 
            { 
                Id = 1, 
                Name = "Deluxe Room", 
                Price = 22628, 
                ImageUrl = "/images/deluxe_room.png",
                Description = "Experience luxury in our spacious Deluxe Room with modern amenities and elegant design.",
                HasKingBed = true,
                HasDoubleBeds = false,
                MaxGuests = 3
            },
            new Room 
            { 
                Id = 2, 
                Name = "Deluxe Suite Room", 
                Price = 33800, 
                ImageUrl = "/images/deluxe-suite_room.png",
                Description = "Upgrade your stay with our Deluxe Suite featuring a separate living area and premium furnishings.",
                HasKingBed = false,
                HasDoubleBeds = true,
                MaxGuests = 4
            },
            new Room 
            { 
                Id = 3, 
                Name = "Executive Deluxe Room", 
                Price = 55500, 
                ImageUrl = "/images/executive-deluxe_room.png",
                Description = "Our Executive Deluxe Room offers the ultimate luxury experience with panoramic views and exclusive amenities.",
                HasKingBed = true,
                HasDoubleBeds = true,
                MaxGuests = 4
            }
        };
        
        public RoomsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Rooms";
            return View(_rooms);
        }

        public IActionResult Details(int id)
        {
            var room = _rooms.FirstOrDefault(r => r.Id == id);
            if (room == null)
            {
                return NotFound();
            }
            
            // Instead of going to a separate details page, redirect to the unified page with a parameter to open the details modal
            return RedirectToAction("Index", new { detailsRoomId = id });
        }

        [HttpPost]
        public IActionResult FilterRooms(bool kingBed, bool doubleBeds, bool threeGuests, bool fourGuests)
        {
            var filteredRooms = _rooms.Where(r => 
                (!kingBed && !doubleBeds || kingBed && r.HasKingBed || doubleBeds && r.HasDoubleBeds) &&
                (!threeGuests && !fourGuests || threeGuests && r.MaxGuests >= 3 || fourGuests && r.MaxGuests >= 4)
            ).ToList();
            
            return PartialView("_RoomList", filteredRooms);
        }
        
        [HttpPost]
        public async Task<IActionResult> CompleteBooking(
            int roomId, 
            DateTime checkIn, 
            DateTime checkOut, 
            int numberOfGuests,
            string guestName,
            string guestEmail,
            string guestPhone,
            string guestAddress = "",
            string specialRequests = "",
            string paymentMethod = "PayAtHotel",
            string cardName = "",
            string cardNumber = "",
            string cardExpiry = "",
            string cardCvc = "",
            string gcashName = "",
            string gcashNumber = "")
        {
            // In a real application, this would save to database
            var room = _rooms.FirstOrDefault(r => r.Id == roomId);
            if (room == null)
            {
                return NotFound();
            }
            
            // Validate required fields
            if (string.IsNullOrEmpty(guestName) || string.IsNullOrEmpty(guestEmail) || string.IsNullOrEmpty(guestPhone))
            {
                return BadRequest("Contact information is required");
            }
            
            try
            {
                // Check if the room exists in the database, if not, use the static room details
                var dbRoom = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId);
                int actualRoomId = roomId;
                
                if (dbRoom == null)
                {
                    // Find a room with similar characteristics in the database
                    dbRoom = await _context.Rooms
                        .FirstOrDefaultAsync(r => r.RoomType.ToString() == room.Name || 
                                             r.Description.Contains(room.Name));
                    
                    if (dbRoom != null)
                    {
                        actualRoomId = dbRoom.RoomId;
                    }
                    else
                    {
                        // If no matching room in database, create a new one
                        var newRoom = new Room
                        {
                            RoomNumber = $"R{roomId}",
                            RoomType = GetRoomTypeFromName(room.Name),
                            Description = room.Description,
                            PricePerNight = room.Price,
                            MaxGuests = room.MaxGuests,
                            BedType = room.HasKingBed ? "King Bed" : "Double Beds",
                            RoomSize = "40 sq m",
                            AvailabilityStatus = AvailabilityStatus.Available,
                            ImageUrl = room.ImageUrl,
                            IsAvailable = true
                        };
                        
                        _context.Rooms.Add(newRoom);
                        await _context.SaveChangesAsync();
                        actualRoomId = newRoom.RoomId;
                    }
                }
                
                // Check if the user exists in the database, if not, create a new user
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == guestEmail);
                
                if (user == null)
                {
                    // Create a new user
                    user = new User
                    {
                        FullName = guestName,
                        Email = guestEmail,
                        Username = guestEmail.Split('@')[0],
                        Password = "Guest123!", // In production, use password hashing
                        PhoneNumber = guestPhone,
                        UserType = UserType.Customer,
                        CreatedAt = DateTime.Now
                    };
                    
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }
                
                // Calculate total price
                int days = Math.Max(1, (checkOut - checkIn).Days);
                decimal totalPrice = room.Price * days;
                
                // Create a new booking
                var booking = new Booking
                {
                    UserId = user.UserId,
                    RoomId = actualRoomId,
                    CheckInDate = checkIn,
                    CheckOutDate = checkOut,
                    NumberOfGuests = numberOfGuests,
                    TotalPrice = totalPrice,
                    Status = BookingStatus.Pending,
                    CreatedAt = DateTime.Now
                };
                
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync(); // Save to get BookingId
                
                // Create a payment record
                PaymentStatus paymentStatus;
                string paymentDetails = string.Empty;
                
                if (paymentMethod == "PayAtHotel")
                {
                    paymentStatus = PaymentStatus.Pending;
                    paymentDetails = "Payment to be collected at hotel.";
                }
                else if (paymentMethod == "CreditCard")
                {
                    // In a real app, you would process the payment through a payment gateway
                    paymentStatus = PaymentStatus.Completed;
                    
                    // Mask card number for security
                    string maskedCardNumber = cardNumber.Length > 4 
                        ? "XXXX-XXXX-XXXX-" + cardNumber.Substring(cardNumber.Length - 4) 
                        : cardNumber;
                        
                    paymentDetails = $"Credit Card: {cardName}, {maskedCardNumber}, Exp: {cardExpiry}";
                }
                else if (paymentMethod == "GCash")
                {
                    // In a real app, you would process the payment through GCash API
                    paymentStatus = PaymentStatus.Completed;
                    
                    // Mask GCash number for security
                    string maskedGCashNumber = gcashNumber.Length > 4
                        ? "****" + gcashNumber.Substring(gcashNumber.Length - 4)
                        : gcashNumber;
                        
                    paymentDetails = $"GCash: {gcashName}, Account: {maskedGCashNumber}";
                }
                else
                {
                    paymentStatus = PaymentStatus.Pending;
                    paymentDetails = $"{paymentMethod} payment to be processed.";
                }
                
                var payment = new Payment
                {
                    UserId = user.UserId,
                    BookingId = booking.BookingId,
                    Amount = totalPrice,
                    PaymentMethod = paymentMethod,
                    PaymentStatus = paymentStatus,
                    PaymentDetails = paymentDetails,
                    PaymentDate = DateTime.Now
                };
                
                _context.Payments.Add(payment);
                
                // Update room availability
                if (dbRoom != null)
                {
                    dbRoom.AvailabilityStatus = AvailabilityStatus.Booked;
                    _context.Rooms.Update(dbRoom);
                }
                
                await _context.SaveChangesAsync();
                
                // Create the confirmation response
                var confirmation = new
                {
                    RoomName = room.Name,
                    CheckInDate = checkIn.ToString("yyyy-MM-dd"),
                    CheckOutDate = checkOut.ToString("yyyy-MM-dd"),
                    Guests = numberOfGuests,
                    TotalPrice = totalPrice,
                    BookingId = booking.BookingId,
                    GuestName = guestName,
                    GuestEmail = guestEmail,
                    GuestPhone = guestPhone,
                    GuestAddress = guestAddress,
                    SpecialRequests = specialRequests,
                    PaymentMethod = paymentMethod,
                    PaymentStatus = paymentStatus.ToString()
                };
                
                return Json(confirmation);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error creating booking: {ex.Message}");
                return BadRequest("An error occurred while processing your booking. Please try again.");
            }
        }
        
        private RoomType GetRoomTypeFromName(string name)
        {
            if (name.Contains("Executive"))
                return RoomType.ExecutiveDeluxe;
            else if (name.Contains("Suite"))
                return RoomType.DeluxeSuite;
            else if (name.Contains("Deluxe"))
                return RoomType.Deluxe;
            else
                return RoomType.Standard;
        }
    }
} 