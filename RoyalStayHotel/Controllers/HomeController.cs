using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RoyalStayHotel.Models;
using RoyalStayHotel.Data;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace RoyalStayHotel.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Services()
    {
        // Instead of removing services, let's just make sure our required services exist
        
        // Check if the new services exist, add them if they don't
        if (!_context.HotelServices.Any(s => s.Name == "Spa Treatment"))
        {
            _context.HotelServices.Add(new HotelService
            {
                Name = "Spa Treatment",
                Description = "Indulge in our luxurious spa treatments for ultimate relaxation and rejuvenation.",
                Price = 1500.00m,
                IsAvailable = true,
                ServiceType = ServiceType.Main
            });
            _context.SaveChanges();
        }
        
        if (!_context.HotelServices.Any(s => s.Name == "Limousine Service"))
        {
            _context.HotelServices.Add(new HotelService
            {
                Name = "Limousine Service",
                Description = "Travel in style with our luxury limousine service for city tours and special events.",
                Price = 3500.00m,
                IsAvailable = true,
                ServiceType = ServiceType.Main
            });
            _context.SaveChanges();
        }

        // Get all available hotel services to display, separated by service type
        var mainServices = _context.HotelServices
            .Where(s => s.IsAvailable && s.ServiceType == ServiceType.Main)
            .OrderBy(s => s.Name)
            .ToList();
            
        var additionalServices = _context.HotelServices
            .Where(s => s.IsAvailable && s.ServiceType == ServiceType.AdditionalService)
            .OrderBy(s => s.Name)
            .ToList();
            
        // Create a view model to pass both lists to the view
        var viewModel = new ServicesViewModel
        {
            MainServices = mainServices,
            AdditionalServices = additionalServices
        };
            
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BookService(
        int ServiceId, 
        DateTime RequestDate, 
        string RequestTime, 
        string? Notes, 
        string? RoomNumber,
        int Quantity = 1)
    {
        try
        {
            // Convert RequestTime string to TimeSpan
            if (!TimeSpan.TryParse(RequestTime, out TimeSpan timeSpan))
            {
                TempData["ErrorMessage"] = "Invalid time format. Please enter a valid time.";
                return RedirectToAction(nameof(Services));
            }
            
            // Check if the service exists
            var service = await _context.HotelServices.FindAsync(ServiceId);
            if (service == null || !service.IsAvailable)
            {
                TempData["ErrorMessage"] = "Service not available. Please try another service.";
                return RedirectToAction(nameof(Services));
            }

            // Validate quantity
            if (Quantity < 1 || Quantity > 100)
            {
                TempData["ErrorMessage"] = "Invalid quantity. Please select between 1 and 100.";
                return RedirectToAction(nameof(Services));
            }
            
            // Validate request date is not in the past
            if (RequestDate.Date < DateTime.Now.Date)
            {
                TempData["ErrorMessage"] = "Please select a current or future date.";
                return RedirectToAction(nameof(Services));
            }
            
            // Check if room exists if room number is provided
            int? bookingId = null;
            if (!string.IsNullOrWhiteSpace(RoomNumber))
            {
                // Check if the room exists and has an active booking
                var room = await _context.Rooms
                    .FirstOrDefaultAsync(r => r.RoomNumber == RoomNumber);
                
                if (room == null)
                {
                    TempData["ErrorMessage"] = "Room number not found. Please check the room number and try again.";
                    return RedirectToAction(nameof(Services));
                }
                
                // Find an active booking for this room
                var booking = await _context.Bookings
                    .Where(b => b.RoomId == room.Id && 
                               b.CheckInDate <= RequestDate && 
                               b.CheckOutDate >= RequestDate &&
                               (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending))
                    .OrderByDescending(b => b.CreatedAt)
                    .FirstOrDefaultAsync();
                
                if (booking != null)
                {
                    bookingId = booking.BookingId;
                    // Add the booking information to notes
                    Notes = (Notes ?? "") + $" | Room: {RoomNumber}, Booking ID: {booking.BookingId}";
                }
                else
                {
                    TempData["ErrorMessage"] = "No active booking found for this room on the selected date.";
                    return RedirectToAction(nameof(Services));
                }
            }
            
            // Calculate total price
            decimal totalPrice = service.Price * Quantity;
            
            // Get user ID if logged in, or use null for guest bookings
            string? userId = null;
            if (User?.Identity?.IsAuthenticated == true)
            {
                // Get the user ID based on your authentication system
                // userId = _userManager.GetUserId(User);
            }
            
            // Create booked service record
            var bookedService = new BookedService
            {
                ServiceId = ServiceId,
                BookingId = bookingId,
                RequestDate = RequestDate,
                RequestTime = timeSpan,
                Notes = Notes,
                Quantity = Quantity,
                TotalPrice = totalPrice,
                Status = "Confirmed",
                CreatedAt = DateTime.Now
            };
            
            _context.BookedServices.Add(bookedService);
            await _context.SaveChangesAsync();
            
            // Log service booking
            _logger.LogInformation($"Service booking created: ServiceId={ServiceId}, BookingId={bookingId}, Quantity={Quantity}, TotalPrice={totalPrice}");
            
            TempData["SuccessMessage"] = $"Your {service.Name} has been booked successfully! Our staff will contact you to confirm.";
            return RedirectToAction(nameof(Services));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while booking service");
            TempData["ErrorMessage"] = "An error occurred while processing your request. Please try again later.";
            return RedirectToAction(nameof(Services));
        }
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
