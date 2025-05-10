using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace RoyalStayHotel.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BookingController> _logger;

        public BookingController(ApplicationDbContext context, ILogger<BookingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Redirect to the unified Rooms/Booking page
            return RedirectToAction("Index", "Rooms");
        }

        [HttpPost]
        public async Task<IActionResult> ValidateDiscountCode(string code, int roomId, DateTime checkIn, DateTime checkOut, int numberOfDays, decimal originalPrice)
        {
            _logger.LogInformation($"Validating discount code: {code} for room {roomId}, {checkIn} to {checkOut}, nights: {numberOfDays}, originalPrice: {originalPrice:C2}");
            
            try 
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    return Json(new { isValid = false, message = "Please enter a discount code." });
                }
                
                // Find discount by code
                var discount = await _context.Discounts
                    .FirstOrDefaultAsync(d => d.Code.ToUpper() == code.ToUpper());
                    
                if (discount == null)
                {
                    _logger.LogWarning($"Invalid discount code attempted: '{code}'");
                    return Json(new { isValid = false, message = "Invalid discount code." });
                }
                
                _logger.LogInformation($"Found discount: ID={discount.DiscountId}, Name={discount.Name}, Amount={discount.DiscountAmount}, IsPercentage={discount.IsPercentage}");
                
                // Check if discount is active
                if (!discount.IsActive)
                {
                    _logger.LogInformation($"Discount '{code}' is inactive");
                    return Json(new { isValid = false, message = "This discount code has expired or is inactive." });
                }
                
                // Check if discount is within valid date range
                if (discount.StartDate > DateTime.Now || discount.EndDate < DateTime.Now)
                {
                    _logger.LogInformation($"Discount '{code}' date range is invalid. Current: {DateTime.Now}, Valid: {discount.StartDate} to {discount.EndDate}");
                    return Json(new { isValid = false, message = $"This discount is only valid from {discount.StartDate:d} to {discount.EndDate:d}." });
                }
                
                // Check if max usage has been reached
                if (discount.MaxUsage.HasValue && discount.UsageCount >= discount.MaxUsage.Value)
                {
                    _logger.LogInformation($"Discount '{code}' max usage reached: {discount.UsageCount}/{discount.MaxUsage}");
                    return Json(new { isValid = false, message = "This discount code has reached its maximum usage limit." });
                }
                
                // Check minimum stay requirement if applicable
                if (discount.MinimumStay.HasValue && numberOfDays < discount.MinimumStay.Value)
                {
                    _logger.LogInformation($"Discount '{code}' minimum stay not met: {numberOfDays}/{discount.MinimumStay} nights");
                    return Json(new { 
                        isValid = false, 
                        message = $"This discount requires a minimum stay of {discount.MinimumStay.Value} nights." 
                    });
                }
                
                // Check minimum spend requirement if applicable
                if (discount.MinimumSpend.HasValue && originalPrice < discount.MinimumSpend.Value)
                {
                    _logger.LogInformation($"Discount '{code}' minimum spend not met: {originalPrice:C2}/{discount.MinimumSpend:C2}");
                    return Json(new { 
                        isValid = false, 
                        message = $"This discount requires a minimum spend of ₱{discount.MinimumSpend.Value:N2}." 
                    });
                }
                
                // Check if booking dates fall completely within the discount validity period
                if (checkIn < discount.StartDate || checkOut > discount.EndDate)
                {
                    _logger.LogInformation($"Booking dates outside discount validity: Booking {checkIn} to {checkOut}, Discount valid {discount.StartDate} to {discount.EndDate}");
                    return Json(new {
                        isValid = false,
                        message = $"This discount is only valid for stays between {discount.StartDate:d} and {discount.EndDate:d}."
                    });
                }
                
                // Check if discount applies to this room type if it's room-specific
                if (discount.RoomTypeId.HasValue)
                {
                    var room = await _context.Rooms.FindAsync(roomId);
                    if (room == null)
                    {
                        _logger.LogWarning($"Room ID {roomId} not found while validating discount");
                        return Json(new { isValid = false, message = "Room not found." });
                    }
                    
                    if ((int)room.RoomType != discount.RoomTypeId.Value)
                    {
                        var roomTypeName = Enum.GetName(typeof(RoomType), discount.RoomTypeId.Value);
                        _logger.LogInformation($"Discount '{code}' not applicable to room type: {room.RoomType}, requires: {roomTypeName}");
                        return Json(new { 
                            isValid = false, 
                            message = $"This discount is only valid for {roomTypeName} rooms." 
                        });
                    }
                }
                
                // Calculate discount amount
                decimal discountAmount = 0;
                if (discount.IsPercentage)
                {
                    discountAmount = Math.Round(originalPrice * (discount.DiscountAmount / 100), 2);
                    _logger.LogInformation($"Calculated percentage discount: {discount.DiscountAmount}% = {discountAmount:C2} off {originalPrice:C2}");
                }
                else
                {
                    // For fixed amount discounts, apply the discount for each night if it's a nightly discount
                    if (discount.Type == DiscountType.RoomRate)
                    {
                        discountAmount = discount.DiscountAmount * numberOfDays;
                        _logger.LogInformation($"Applied nightly discount: {discount.DiscountAmount:C2} x {numberOfDays} nights = {discountAmount:C2}");
                    }
                    else
                    {
                        discountAmount = discount.DiscountAmount;
                        _logger.LogInformation($"Applied fixed discount: {discountAmount:C2}");
                    }
                }
                
                // Cap discount amount to original price
                if (discountAmount > originalPrice)
                {
                    _logger.LogInformation($"Capping discount to original price: {discountAmount:C2} -> {originalPrice:C2}");
                    discountAmount = originalPrice;
                }
                
                decimal discountedPrice = originalPrice - discountAmount;
                
                // Create discount description
                string discountDescription = discount.IsPercentage 
                    ? $"{discount.Name}: {discount.DiscountAmount}% off" 
                    : $"{discount.Name}: ₱{discount.DiscountAmount:N2} off";
                
                _logger.LogInformation($"Discount '{code}' applied successfully: {discountDescription}, saved: {discountAmount:C2}");
                    
                return Json(new { 
                    isValid = true, 
                    discountId = discount.DiscountId,
                    discountAmount = discountAmount,
                    discountedPrice = discountedPrice,
                    discountDescription = discountDescription,
                    isPercentage = discount.IsPercentage,
                    percentValue = discount.IsPercentage ? discount.DiscountAmount : 0,
                    message = "Discount applied successfully!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating discount code: {ex.Message}");
                return Json(new { isValid = false, message = "An error occurred while applying the discount. Please try again or contact support." });
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> GetBestDiscount(int roomId, DateTime checkIn, DateTime checkOut, decimal totalPrice)
        {
            try
            {
                int numberOfDays = (int)(checkOut - checkIn).TotalDays;
                if (numberOfDays <= 0) 
                {
                    return Json(new { success = false, message = "Invalid date range" });
                }
                
                _logger.LogInformation($"Finding best discount for room {roomId}, {checkIn} to {checkOut}, total: {totalPrice:C2}");
                
                var room = await _context.Rooms.FindAsync(roomId);
                if (room == null)
                {
                    return Json(new { success = false, message = "Room not found" });
                }
                
                // Get all active discounts that could apply to this booking
                var applicableDiscounts = await _context.Discounts
                    .Where(d => d.IsActive 
                        && d.StartDate <= DateTime.Now 
                        && d.EndDate >= DateTime.Now
                        && (!d.MinimumStay.HasValue || d.MinimumStay <= numberOfDays)
                        && (!d.MinimumSpend.HasValue || d.MinimumSpend <= totalPrice)
                        && (!d.MaxUsage.HasValue || d.UsageCount < d.MaxUsage.Value)
                        && (!d.RoomTypeId.HasValue || d.RoomTypeId == (int)room.RoomType))
                    .ToListAsync();
                
                if (!applicableDiscounts.Any())
                {
                    return Json(new { success = true, hasDiscount = false, message = "No applicable discounts found" });
                }
                
                _logger.LogInformation($"Found {applicableDiscounts.Count} potentially applicable discounts");
                
                // Calculate the savings for each discount
                var discountsWithSavings = applicableDiscounts.Select(d => {
                    decimal savings;
                    if (d.IsPercentage)
                    {
                        savings = Math.Round(totalPrice * (d.DiscountAmount / 100), 2);
                    }
                    else if (d.Type == DiscountType.RoomRate)
                    {
                        savings = d.DiscountAmount * numberOfDays;
                    }
                    else
                    {
                        savings = d.DiscountAmount;
                    }
                    
                    // Cap savings to the total price
                    if (savings > totalPrice) savings = totalPrice;
                    
                    return new { Discount = d, Savings = savings };
                })
                .OrderByDescending(ds => ds.Savings)
                .ToList();
                
                var bestDiscount = discountsWithSavings.FirstOrDefault();
                if (bestDiscount != null)
                {
                    var discount = bestDiscount.Discount;
                    decimal discountedPrice = totalPrice - bestDiscount.Savings;
                    
                    _logger.LogInformation($"Best discount: ID={discount.DiscountId}, Name={discount.Name}, Savings={bestDiscount.Savings:C2}");
                    
                    string discountDescription = discount.IsPercentage 
                        ? $"{discount.Name}: {discount.DiscountAmount}% off" 
                        : $"{discount.Name}: ₱{discount.DiscountAmount:N2} off";
                    
                    return Json(new { 
                        success = true, 
                        hasDiscount = true,
                        discountId = discount.DiscountId,
                        code = discount.Code,
                        name = discount.Name,
                        description = discount.Description,
                        discountAmount = bestDiscount.Savings,
                        discountedPrice = discountedPrice,
                        discountDescription = discountDescription,
                        message = $"Best available discount: {discountDescription}, saving ₱{bestDiscount.Savings:N2}"
                    });
                }
                
                return Json(new { success = true, hasDiscount = false, message = "No applicable discounts found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding best discount: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while checking for discounts" });
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> CanStackDiscounts()
        {
            // Hotel policy: don't allow stacking discounts
            // This method exists to make it easy to change this policy in the future
            return Json(new { canStack = false, message = "Only one discount code can be applied per booking" });
        }

        public async Task<IActionResult> Book(
            int roomId, 
            DateTime checkIn, 
            DateTime checkOut, 
            int numberOfGuests = 1, 
            string? specialRequests = null,
            string guestName = null,
            string guestEmail = null,
            string guestPhone = null,
            List<string>? SelectedServices = null,
            int PoolGuestCount = 0,
            decimal PoolPrice = 0,
            int BreakfastGuestCount = 0,
            decimal BreakfastPrice = 0,
            int GymGuestCount = 0,
            decimal GymPrice = 0,
            int? appliedDiscountId = null,
            decimal discountAmount = 0,
            decimal originalPrice = 0)
        {
            // Log incoming parameters
            _logger.LogInformation($"Booking started: RoomId={roomId}, CheckIn={checkIn}, CheckOut={checkOut}, Guests={numberOfGuests}");
            _logger.LogInformation($"Guest info: Name={guestName}, Email={guestEmail}, Phone={guestPhone}");
            _logger.LogInformation($"Selected services: {(SelectedServices != null ? string.Join(", ", SelectedServices) : "None")}");
            
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null)
            {
                _logger.LogWarning($"Room with ID {roomId} not found");
                return NotFound();
            }

            // Validate days
            int days = (int)(checkOut - checkIn).TotalDays;
            if (days <= 0)
            {
                ModelState.AddModelError("", "Check-out date must be after check-in date.");
                ViewBag.Room = room;
                return View();
            }

            // Calculate room price
            decimal roomTotalPrice = room.PricePerNight * days;
            decimal servicesTotalPrice = 0;

            // Generate a random booking reference
            string bookingReference = "BK" + DateTime.Now.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString().Substring(0, 5).ToUpper();

            try
            {
                // Step 1: First create user if needed with direct SQL to avoid any EF Core tracking issues
                int userId;
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    connection.Open();
                    
                    // Check if user exists
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT UserId FROM Users WHERE Email = @Email";
                        command.Parameters.AddWithValue("@Email", guestEmail ?? "");
                        
                        var result = await command.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            // User exists
                            userId = Convert.ToInt32(result);
                            _logger.LogInformation($"Using existing user with ID: {userId}");
                        }
                        else
                        {
                            // Create new user with direct SQL
                            string fullName = guestName ?? "Guest User";
                            string email = guestEmail ?? $"guest{Guid.NewGuid().ToString().Substring(0, 8)}@example.com";
                            string username = guestEmail != null ? guestEmail.Split('@')[0] : $"guest{new Random().Next(10000, 99999)}";
                            string password = "Guest123!";
                            string phone = guestPhone ?? "0000000000";
                            int userType = (int)UserType.Customer;
                            
                            command.CommandText = @"
                                INSERT INTO Users (FullName, Email, Username, Password, PhoneNumber, UserType, CreatedAt) 
                                VALUES (@FullName, @Email, @Username, @Password, @Phone, @UserType, @CreatedAt);
                                
                                DECLARE @NewUserId int;
                                SET @NewUserId = SCOPE_IDENTITY();
                                
                                -- Update the Id field to match the UserId
                                UPDATE Users SET Id = @NewUserId WHERE UserId = @NewUserId;
                                
                                SELECT @NewUserId;";
                            
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@FullName", fullName);
                            command.Parameters.AddWithValue("@Email", email);
                            command.Parameters.AddWithValue("@Username", username);
                            command.Parameters.AddWithValue("@Password", password);
                            command.Parameters.AddWithValue("@Phone", phone);
                            command.Parameters.AddWithValue("@UserType", userType);
                            command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                            
                            // Execute and get the new ID
                            var newIdResult = await command.ExecuteScalarAsync();
                            userId = Convert.ToInt32(newIdResult);
                            
                            _logger.LogInformation($"Created new user with ID: {userId}");
                        }
                    }
                    
                    // Step 2: Create booking with direct SQL
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            INSERT INTO Bookings 
                            (UserId, RoomId, CheckInDate, CheckOutDate, NumberOfGuests, 
                            Status, CreatedAt, SpecialRequests, BookingReference, TotalPrice,
                            AppliedDiscountId, DiscountAmount, OriginalPrice)
                            VALUES 
                            (@UserId, @RoomId, @CheckInDate, @CheckOutDate, @NumberOfGuests,
                            @Status, @CreatedAt, @SpecialRequests, @BookingReference, @TotalPrice,
                            @AppliedDiscountId, @DiscountAmount, @OriginalPrice);
                            SELECT SCOPE_IDENTITY();";
                        
                        decimal totalPrice = roomTotalPrice;
                        
                        // Apply discount if present
                        if (appliedDiscountId.HasValue && discountAmount > 0)
                        {
                            _logger.LogInformation($"Applying discount ID {appliedDiscountId} with amount {discountAmount}");
                            totalPrice = originalPrice > 0 ? originalPrice - discountAmount : totalPrice - discountAmount;
                            
                            // Increment usage count for the discount
                            using (var discountCommand = connection.CreateCommand())
                            {
                                discountCommand.CommandText = "UPDATE Discounts SET UsageCount = UsageCount + 1 WHERE DiscountId = @DiscountId";
                                discountCommand.Parameters.AddWithValue("@DiscountId", appliedDiscountId.Value);
                                await discountCommand.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
                            // No discount
                            discountAmount = 0;
                            originalPrice = totalPrice;
                        }
                        
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@RoomId", roomId);
                        command.Parameters.AddWithValue("@CheckInDate", checkIn);
                        command.Parameters.AddWithValue("@CheckOutDate", checkOut);
                        command.Parameters.AddWithValue("@NumberOfGuests", numberOfGuests);
                        command.Parameters.AddWithValue("@Status", (int)BookingStatus.Pending);
                        command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                        command.Parameters.AddWithValue("@SpecialRequests", specialRequests ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@BookingReference", bookingReference);
                        command.Parameters.AddWithValue("@TotalPrice", totalPrice);
                        command.Parameters.AddWithValue("@AppliedDiscountId", appliedDiscountId.HasValue ? (object)appliedDiscountId.Value : DBNull.Value);
                        command.Parameters.AddWithValue("@DiscountAmount", discountAmount);
                        command.Parameters.AddWithValue("@OriginalPrice", originalPrice);
                        
                        // Execute and get the new booking ID
                        var bookingIdResult = await command.ExecuteScalarAsync();
                        int bookingId = Convert.ToInt32(bookingIdResult);
                        
                        _logger.LogInformation($"Created booking with ID: {bookingId}");
                        
                        // Step 3: Add booked services if any
                        if (SelectedServices != null && SelectedServices.Any())
            {
                foreach (var serviceType in SelectedServices)
                {
                    HotelService? service = null;
                    int quantity = 1;
                    decimal price = 0;
                    string notes = "";

                    switch (serviceType)
                    {
                        case "PoolAccess":
                            service = await _context.HotelServices.FirstOrDefaultAsync(s => s.Name == "Swimming Pool");
                            quantity = PoolGuestCount;
                            price = PoolPrice;
                            notes = $"Pool for {PoolGuestCount} guests";
                            break;
                        case "BreakfastBuffet":
                            service = await _context.HotelServices.FirstOrDefaultAsync(s => s.Name == "Breakfast Buffet");
                            quantity = BreakfastGuestCount;
                            price = BreakfastPrice;
                            notes = $"Breakfast for {BreakfastGuestCount} guests";
                            break;
                        case "GymAccess":
                            service = await _context.HotelServices.FirstOrDefaultAsync(s => s.Name == "Gym Access");
                            quantity = GymGuestCount;
                            price = GymPrice;
                            notes = $"Gym for {GymGuestCount} guests";
                            break;
                    }

                    if (service != null && quantity > 0)
                    {
                        if (price <= 0)
                            price = service.Price * quantity;

                                    totalPrice += price;
                                    
                                    // Insert booked service
                                    using (var serviceCmd = connection.CreateCommand())
                                    {
                                        serviceCmd.CommandText = @"
                                            INSERT INTO BookedServices 
                                            (ServiceId, BookingId, RequestDate, RequestTime, Notes, Quantity, TotalPrice, Status, CreatedAt)
                                            VALUES 
                                            (@ServiceId, @BookingId, @RequestDate, @RequestTime, @Notes, @Quantity, @TotalPrice, @Status, @CreatedAt)";
                                        
                                        serviceCmd.Parameters.AddWithValue("@ServiceId", service.Id);
                                        serviceCmd.Parameters.AddWithValue("@BookingId", bookingId);
                                        serviceCmd.Parameters.AddWithValue("@RequestDate", checkIn);
                                        serviceCmd.Parameters.AddWithValue("@RequestTime", new TimeSpan(14, 0, 0));
                                        serviceCmd.Parameters.AddWithValue("@Notes", notes ?? (object)DBNull.Value);
                                        serviceCmd.Parameters.AddWithValue("@Quantity", quantity);
                                        serviceCmd.Parameters.AddWithValue("@TotalPrice", price);
                                        serviceCmd.Parameters.AddWithValue("@Status", "Confirmed");
                                        serviceCmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                                        
                                        await serviceCmd.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                            
                            // Update booking with total price
                            using (var updateCmd = connection.CreateCommand())
                            {
                                updateCmd.CommandText = "UPDATE Bookings SET TotalPrice = @TotalPrice WHERE BookingId = @BookingId";
                                updateCmd.Parameters.AddWithValue("@TotalPrice", totalPrice);
                                updateCmd.Parameters.AddWithValue("@BookingId", bookingId);
                                
                                await updateCmd.ExecuteNonQueryAsync();
                            }
                        }
                        
                        // Update room availability
                        using (var roomCmd = connection.CreateCommand())
                        {
                            roomCmd.CommandText = "UPDATE Rooms SET IsAvailable = 0, AvailabilityStatus = @Status WHERE RoomId = @RoomId";
                            roomCmd.Parameters.AddWithValue("@Status", (int)AvailabilityStatus.Booked);
                            roomCmd.Parameters.AddWithValue("@RoomId", roomId);
                            
                            await roomCmd.ExecuteNonQueryAsync();
                        }
                        
                        // Success - redirect to confirmation
                        return RedirectToAction("Confirmation", new { id = bookingId });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving booking: {ex.Message}");
                
                // Add more detailed exception information
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                
                // If there's an error about the User.Id column, update existing users
                if (ex.Message.Contains("Id") && ex.Message.Contains("NULL"))
                {
                    _logger.LogWarning("Attempting to fix User.Id values...");
                    try
                    {
                        using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                        {
                            connection.Open();
                            
                            using (var command = connection.CreateCommand())
                            {
                                command.CommandText = "UPDATE Users SET Id = UserId WHERE Id IS NULL";
                                int rowsAffected = command.ExecuteNonQuery();
                                _logger.LogInformation($"Fixed {rowsAffected} user records with NULL Id");
                            }
                        }
                    }
                    catch (Exception fixEx)
                    {
                        _logger.LogError(fixEx, "Error while trying to fix User.Id values");
                    }
                }
                
                // Store the error in TempData so we can display it to the user
                TempData["ErrorMessage"] = "There was an error processing your reservation. Please try again or contact support.";
                
                // Redirect to home page with error parameter
                return RedirectToAction("Index", "Home", new { error = "BookingError" });
            }
        }

        public async Task<IActionResult> Confirmation(int id)
        {
            _logger.LogInformation($"Loading confirmation for booking ID: {id}");
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.User)
                .Include(b => b.BookedServices)
                    .ThenInclude(bs => bs.Service)
                .Include(b => b.AppliedDiscount)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                _logger.LogWarning($"Booking with ID {id} not found");
                return NotFound();
            }

            _logger.LogInformation($"Booking with ID {id} exists: {booking != null}");
            _logger.LogInformation($"Loaded booking details: ID={booking.BookingId}, UserId={booking.UserId}, RoomId={booking.RoomId}");
            
            if (booking.Room != null)
                _logger.LogInformation($"Loaded room details: ID={booking.Room.RoomId}, Number={booking.Room.RoomNumber}");
            
            if (booking.User != null)
                _logger.LogInformation($"Loaded user details: ID={booking.User.UserId}, Name={booking.User.FullName}");
            
            // If we have a discount but no original price, set it based on room rate * nights
            if (booking.AppliedDiscountId.HasValue && booking.DiscountAmount > 0 && (!booking.OriginalPrice.HasValue || booking.OriginalPrice == 0))
            {
                int days = (booking.CheckOutDate - booking.CheckInDate).Days;
                if (booking.Room != null && days > 0)
                {
                    booking.OriginalPrice = booking.Room.PricePerNight * days;
                }
            }

            try
            {
                // Load booked services with direct ADO.NET to handle TimeSpan correctly
                if (booking.BookedServices == null || !booking.BookedServices.Any())
                {
                    booking.BookedServices = new List<BookedService>();
                    using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                    {
                        await connection.OpenAsync();
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = @"
                                SELECT bs.Id, bs.ServiceId, bs.BookingId, bs.RequestDate, bs.RequestTime, 
                                       bs.Notes, bs.Quantity, bs.TotalPrice, bs.Status, bs.CreatedAt,
                                       s.Id as ServiceId, s.Name, s.Description, s.Price, s.ServiceType, s.IsAvailable
                                FROM BookedServices bs
                                JOIN HotelServices s ON bs.ServiceId = s.Id
                                WHERE bs.BookingId = @BookingId";
                            command.Parameters.AddWithValue("@BookingId", id);

                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var bookedService = new BookedService
                                    {
                                        Id = reader.GetInt32(0),
                                        ServiceId = reader.GetInt32(1),
                                        BookingId = reader.GetInt32(2),
                                        RequestDate = reader.GetDateTime(3),
                                        Status = reader.GetString(8),
                                        TotalPrice = reader.GetDecimal(7),
                                        Quantity = reader.GetInt32(6),
                                        Notes = reader.IsDBNull(5) ? null : reader.GetString(5),
                                        Service = new HotelService
                                        {
                                            Id = reader.GetInt32(10),
                                            Name = reader.GetString(11),
                                            Description = reader.GetString(12),
                                            Price = reader.GetDecimal(13),
                                            ServiceType = (ServiceType)reader.GetInt32(14),
                                            IsAvailable = reader.GetBoolean(15)
                                        }
                                    };

                                    // Handle TimeSpan conversion carefully
                                    try 
                                    {
                                        if (reader.IsDBNull(4))
                                        {
                                            bookedService.RequestTime = new TimeSpan(14, 0, 0); // Default to 2 PM
                                        }
                                        else
                                        {
                                            var rawValue = reader.GetValue(4);
                                            _logger.LogInformation($"Raw TimeSpan value type: {rawValue?.GetType().Name}, Value: {rawValue}");
                                            
                                            if (rawValue is TimeSpan ts)
                                            {
                                                bookedService.RequestTime = ts;
                                            }
                                            else if (rawValue is DateTime dt)
                                            {
                                                bookedService.RequestTime = dt.TimeOfDay;
                                            }
                                            else if (rawValue is string timeStr)
                                            {
                                                if (TimeSpan.TryParse(timeStr, out TimeSpan parsedTs))
                                                {
                                                    bookedService.RequestTime = parsedTs;
                                                }
                                                else
                                                {
                                                    bookedService.RequestTime = new TimeSpan(14, 0, 0); // Default to 2 PM
                                                }
                                            }
                                            else
                                            {
                                                // Try to parse as string as last resort
                                                string valueStr = rawValue?.ToString();
                                                if (!string.IsNullOrEmpty(valueStr) && TimeSpan.TryParse(valueStr, out TimeSpan parsedTs))
                                                {
                                                    bookedService.RequestTime = parsedTs;
                                                }
                                                else
                                                {
                                                    _logger.LogWarning($"Could not convert {rawValue} to TimeSpan");
                                                    bookedService.RequestTime = new TimeSpan(14, 0, 0); // Default to 2 PM
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Error converting RequestTime");
                                        bookedService.RequestTime = new TimeSpan(14, 0, 0); // Default to 2 PM
                                    }

                                    booking.BookedServices.Add(bookedService);
                                }
                            }
                        }
                    }
                }
                
                // Load discount information if we have a discount ID but not the discount object
                if (booking.AppliedDiscountId.HasValue && booking.AppliedDiscount == null)
                {
                    booking.AppliedDiscount = await _context.Discounts
                        .FirstOrDefaultAsync(d => d.DiscountId == booking.AppliedDiscountId);
                }
                
                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading booking confirmation: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return View("Error", new ErrorViewModel 
                { 
                    RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    Message = "There was an error loading your booking confirmation. Please contact customer service." 
                });
            }
        }

        public async Task<IActionResult> MyBookings()
        {
            try
            {
                // Get all bookings in the system (for testing purposes)
                var bookings = await _context.Bookings
                    .Include(b => b.Room)
                    .Include(b => b.User)
                    .OrderByDescending(b => b.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync();
                
                _logger.LogInformation($"Loaded {bookings.Count} bookings for display");
                
                return View(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading bookings: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading your bookings.";
                return View(new List<Booking>());
            }
        }
        
        public async Task<IActionResult> Details(int id)
        {
            _logger.LogInformation($"Loading details for booking ID: {id}");
            
            if (id <= 0)
            {
                _logger.LogWarning($"Invalid booking ID: {id}");
                TempData["ErrorMessage"] = "Invalid booking ID.";
                return RedirectToAction(nameof(MyBookings));
            }
            
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Room)
                    .Include(b => b.User)
                    .Include(b => b.BookedServices)
                        .ThenInclude(bs => bs.Service)
                    .AsNoTracking()
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(b => b.BookingId == id);
                
                if (booking == null)
                {
                    _logger.LogWarning($"Booking with ID {id} not found");
                    TempData["ErrorMessage"] = "Booking not found.";
                    return RedirectToAction(nameof(MyBookings));
                }

                // Log successful retrieval
                _logger.LogInformation($"Successfully retrieved booking {booking.BookingReference}");
                
                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading booking details: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading booking details.";
                return RedirectToAction(nameof(MyBookings));
            }
        }
        
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation($"Loading delete page for booking ID: {id}");
            
            if (id <= 0)
            {
                _logger.LogWarning($"Invalid booking ID: {id}");
                TempData["ErrorMessage"] = "Invalid booking ID.";
                return RedirectToAction(nameof(MyBookings));
            }
            
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Room)
                    .Include(b => b.BookedServices)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.BookingId == id);
                
                if (booking == null)
                {
                    _logger.LogWarning($"Booking with ID {id} not found for deletion");
                    TempData["ErrorMessage"] = "Booking not found.";
                    return RedirectToAction(nameof(MyBookings));
                }

                // Check if booking can be cancelled
                if (booking.Status == BookingStatus.Cancelled || 
                    booking.Status == BookingStatus.Completed || 
                    booking.Status == BookingStatus.CheckedOut ||
                    booking.CheckInDate <= DateTime.Now)
                {
                    _logger.LogWarning($"Cannot cancel booking {booking.BookingReference} due to its status: {booking.Status}");
                    TempData["ErrorMessage"] = "This booking cannot be cancelled due to its current status.";
                    return RedirectToAction(nameof(MyBookings));
                }
                
                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading delete booking page: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading the delete page.";
                return RedirectToAction(nameof(MyBookings));
            }
        }
        
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            _logger.LogInformation($"Confirming deletion of booking ID: {id}");
            
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.BookedServices)
                .Include(b => b.Room)
                    .FirstOrDefaultAsync(b => b.BookingId == id);
                
                if (booking == null)
                {
                    _logger.LogWarning($"Booking with ID {id} not found for deletion");
                    TempData["ErrorMessage"] = "Booking not found.";
                    return RedirectToAction(nameof(MyBookings));
                }

                // Check if booking can be cancelled
                if (booking.Status == BookingStatus.Cancelled || 
                    booking.Status == BookingStatus.Completed || 
                    booking.Status == BookingStatus.CheckedOut ||
                    booking.CheckInDate <= DateTime.Now)
                {
                    _logger.LogWarning($"Cannot cancel booking {booking.BookingReference} due to its status: {booking.Status}");
                    TempData["ErrorMessage"] = "This booking cannot be cancelled due to its current status.";
                    return RedirectToAction(nameof(MyBookings));
                }
                
                // Update booking status instead of deleting
                booking.Status = BookingStatus.Cancelled;
                
                // Make the room available again
                if (booking.Room != null)
                {
                    booking.Room.IsAvailable = true;
                    booking.Room.AvailabilityStatus = AvailabilityStatus.Available;
                }
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Successfully cancelled booking {booking.BookingReference}");
                TempData["SuccessMessage"] = $"Booking {booking.BookingReference} has been successfully cancelled.";
                return RedirectToAction(nameof(MyBookings));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cancelling booking: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while cancelling the booking.";
                return RedirectToAction(nameof(MyBookings));
            }
        }
    }
} 