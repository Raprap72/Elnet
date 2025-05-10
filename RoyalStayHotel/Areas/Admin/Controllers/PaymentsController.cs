using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RoyalStayHotel.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(ApplicationDbContext context, ILogger<PaymentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            // Get all payments with related booking information
            var payments = await _context.Payments
                .Include(p => p.Booking)
                .ThenInclude(b => b.User)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
            
            return View(payments);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Booking)
                .ThenInclude(b => b.User)
                .Include(p => p.Booking.Room)
                .FirstOrDefaultAsync(m => m.PaymentId == id);
                
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }
        
        // GET: Admin/Payments/Create/5 (5 is the booking ID)
        public async Task<IActionResult> Create(int id)
        {
            _logger.LogInformation($"Loading payment form for booking ID: {id}");
            
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.BookingId == id);
                
            if (booking == null)
            {
                _logger.LogWarning($"Booking with ID {id} not found when creating payment");
                return NotFound();
            }
            
            // Calculate total paid amount and balance
            var totalPaid = await _context.Payments
                .Where(p => p.BookingId == id && p.Status == PaymentStatus.Completed)
                .SumAsync(p => p.Amount);
                
            var balance = booking.TotalPrice - totalPaid;
            
            // Pass booking details to the view
            ViewBag.BookingDetails = booking;
            ViewBag.BookingId = booking.BookingId;
            ViewBag.UserId = booking.UserId;
            ViewBag.AmountPaid = totalPaid;
            ViewBag.Balance = balance;
            
            return View();
        }
        
        // POST: Admin/Payments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Payment payment)
        {
            _logger.LogInformation($"Processing payment for booking ID: {payment.BookingId}");
            
            if (ModelState.IsValid)
            {
                try
                {
                    // Set payment date if not provided
                    if (payment.PaymentDate == default)
                    {
                        payment.PaymentDate = DateTime.Now;
                    }
                    
                    // Add the payment record
                    _context.Add(payment);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Payment {payment.PaymentId} created successfully for booking {payment.BookingId}");
                    
                    // Update booking status if applicable
                    var booking = await _context.Bookings.FindAsync(payment.BookingId);
                    if (booking != null)
                    {
                        // Check if booking is fully paid
                        var totalPaid = await _context.Payments
                            .Where(p => p.BookingId == payment.BookingId && p.Status == PaymentStatus.Completed)
                            .SumAsync(p => p.Amount);
                            
                        if (totalPaid >= booking.TotalPrice)
                        {
                            // If booking was pending, set to confirmed when fully paid
                            if (booking.Status == BookingStatus.Pending)
                            {
                                booking.Status = BookingStatus.Confirmed;
                                await _context.SaveChangesAsync();
                                _logger.LogInformation($"Booking {payment.BookingId} status updated to Confirmed after full payment");
                            }
                        }
                    }
                    
                    TempData["SuccessMessage"] = $"Payment of ₱{payment.Amount:N2} was processed successfully.";
                    return RedirectToAction("Details", "Bookings", new { id = payment.BookingId, area = "Admin" });
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing payment: {ex.Message}");
                    ModelState.AddModelError("", "Error processing payment. Please try again.");
                }
            }
            else
            {
                _logger.LogWarning($"Invalid payment model state: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
            }
            
            // If we got this far, something failed, redisplay form
            var bookingDetails = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.BookingId == payment.BookingId);
                
            // Calculate total paid amount and balance
            var paidAmount = await _context.Payments
                .Where(p => p.BookingId == payment.BookingId && p.Status == PaymentStatus.Completed)
                .SumAsync(p => p.Amount);
                
            var balance = bookingDetails.TotalPrice - paidAmount;
            
            ViewBag.BookingDetails = bookingDetails;
            ViewBag.BookingId = payment.BookingId;
            ViewBag.UserId = payment.UserId;
            ViewBag.AmountPaid = paidAmount;
            ViewBag.Balance = balance;
            
            return View(payment);
        }

        public async Task<IActionResult> MarkAsRefunded(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmRefund(int id, string refundReason)
        {
            _logger.LogInformation($"Processing refund for payment ID: {id}");
            
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                _logger.LogWarning($"Payment with ID {id} not found when processing refund");
                return NotFound();
            }

            payment.Status = PaymentStatus.Refunded;
            payment.PaymentDetails = $"Refunded: {refundReason}";
            
            _context.Update(payment);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Payment {id} marked as refunded successfully");
            
            // Update the booking status if fully refunded
            var bookingId = payment.BookingId;
            var totalPaid = await _context.Payments
                .Where(p => p.BookingId == bookingId && p.Status == PaymentStatus.Completed)
                .SumAsync(p => p.Amount);
                
            var totalRefunded = await _context.Payments
                .Where(p => p.BookingId == bookingId && p.Status == PaymentStatus.Refunded)
                .SumAsync(p => p.Amount);
                
            if (totalRefunded >= totalPaid)
            {
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking != null && booking.Status != BookingStatus.Cancelled && booking.Status != BookingStatus.CheckedOut)
                {
                    booking.Status = BookingStatus.Cancelled;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Booking {bookingId} marked as Cancelled due to full refund");
                }
            }
            
            TempData["SuccessMessage"] = "Payment has been marked as refunded.";
            return RedirectToAction(nameof(Index));
        }

        // Method to generate reports
        public async Task<IActionResult> Report(string period = "month")
        {
            _logger.LogInformation($"Generating payment report for period: {period}");
            
            ViewBag.Period = period;
            
            var payments = await _context.Payments
                .Include(p => p.Booking)
                .Include(p => p.User)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
                
            // Filter payments based on period
            DateTime cutoffDate = DateTime.Now;
            switch (period.ToLower())
            {
                case "week":
                    cutoffDate = DateTime.Now.AddDays(-7);
                    break;
                case "month":
                    cutoffDate = DateTime.Now.AddMonths(-1);
                    break;
                case "quarter":
                    cutoffDate = DateTime.Now.AddMonths(-3);
                    break;
                case "year":
                    cutoffDate = DateTime.Now.AddYears(-1);
                    break;
                default:
                    // For "all", don't filter by date
                    cutoffDate = DateTime.MinValue;
                    break;
            }
            
            if (cutoffDate > DateTime.MinValue)
            {
                payments = payments.Where(p => p.PaymentDate >= cutoffDate).ToList();
            }

            // Calculate totals
            ViewBag.TotalAmount = payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount);
            ViewBag.SuccessfulPayments = payments.Count(p => p.Status == PaymentStatus.Completed);
            ViewBag.PendingPayments = payments.Count(p => p.Status == PaymentStatus.Pending);
            ViewBag.FailedPayments = payments.Count(p => p.Status == PaymentStatus.Failed);
            ViewBag.RefundedPayments = payments.Count(p => p.Status == PaymentStatus.Refunded);
            
            _logger.LogInformation($"Generated report: {payments.Count} payments, Total: {ViewBag.TotalAmount:C}");
            
            return View(payments);
        }
    }
} 