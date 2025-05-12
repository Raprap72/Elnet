using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RoyalStayHotel
{
    public class AddPayment
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Adding payment for booking #16...");
            
            try
            {
                // Create DbContext options
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=RoyalStayHotel;Trusted_Connection=True;MultipleActiveResultSets=true");
                
                // Create context
                using var context = new ApplicationDbContext(optionsBuilder.Options);
                
                // Check if booking exists
                var booking = await context.Bookings
                    .Include(b => b.User)
                    .FirstOrDefaultAsync(b => b.BookingId == 16);
                    
                if (booking == null)
                {
                    Console.WriteLine("Booking #16 not found!");
                    return;
                }
                
                Console.WriteLine($"Found booking for {booking.User?.FullName ?? "unknown user"}");
                Console.WriteLine($"Total price: {booking.TotalPrice:C}");
                
                // Create new payment
                var payment = new Payment
                {
                    BookingId = booking.BookingId,
                    UserId = booking.UserId,
                    Amount = booking.TotalPrice,
                    PaymentMethod = PaymentMethod.GCash,
                    Status = PaymentStatus.Completed,
                    PaymentDate = DateTime.Now,
                    TransactionId = $"GC-{DateTime.Now.Ticks}",
                    Notes = "Payment added for booking #16"
                };
                
                // Add payment to database
                context.Payments.Add(payment);
                await context.SaveChangesAsync();
                
                Console.WriteLine($"Payment added successfully with ID: {payment.PaymentId}");
                
                // Update booking status to Confirmed
                booking.Status = BookingStatus.Confirmed;
                await context.SaveChangesAsync();
                
                Console.WriteLine($"Booking status updated to {booking.Status}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner error: {ex.InnerException.Message}");
                }
            }
            
            Console.WriteLine("Done!");
        }
    }
} 