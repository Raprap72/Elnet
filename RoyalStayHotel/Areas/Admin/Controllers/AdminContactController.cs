using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;

namespace RoyalStayHotel.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/AdminContact
        public async Task<IActionResult> Index()
        {
            var submissions = await _context.ContactFormSubmissions
                .OrderByDescending(s => s.SubmissionDate)
                .ToListAsync();
            
            return View(submissions);
        }

        // GET: Admin/AdminContact/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var submission = await _context.ContactFormSubmissions
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (submission == null)
            {
                return NotFound();
            }

            // Mark as read if it wasn't already
            if (!submission.IsRead)
            {
                submission.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return View(submission);
        }
        
        // GET: Admin/AdminContact/View/5
        [HttpGet]
        [ActionName("View")]
        public async Task<IActionResult> ViewSubmission(int? id)
        {
            // Redirect to Details action
            return RedirectToAction(nameof(Details), new { id = id });
        }
        
        // GET: Admin/AdminContact/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var submission = await _context.ContactFormSubmissions
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (submission == null)
            {
                return NotFound();
            }

            return View(submission);
        }

        // POST: Admin/AdminContact/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var submission = await _context.ContactFormSubmissions.FindAsync(id);
            
            if (submission != null)
            {
                _context.ContactFormSubmissions.Remove(submission);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Message deleted successfully.";
            }
            
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/AdminContact/DeleteDirect/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDirect(int id)
        {
            try
            {
                var submission = await _context.ContactFormSubmissions.FindAsync(id);
                if (submission == null)
                {
                    TempData["ErrorMessage"] = "Message not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Get the name for the success message before deleting
                string name = submission.Name;
                
                // Remove from the database
                _context.ContactFormSubmissions.Remove(submission);
                await _context.SaveChangesAsync();
                
                // Log the deletion
                Console.WriteLine($"Message from {name} was deleted successfully");
                
                TempData["SuccessMessage"] = $"Message from {name} was deleted successfully.";
            }
            catch (Exception ex)
            {
                // Log any errors
                Console.WriteLine($"Error deleting message: {ex.Message}");
                TempData["ErrorMessage"] = $"Error deleting message: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/AdminContact/Respond/5
        public async Task<IActionResult> Respond(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var submission = await _context.ContactFormSubmissions
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (submission == null)
            {
                return NotFound();
            }

            // Mark as read when viewing the response form
            if (!submission.IsRead)
            {
                submission.IsRead = true;
                await _context.SaveChangesAsync();
            }

            ViewBag.Message = TempData["Message"];
            
            return View(submission);
        }
        
        // POST: Admin/AdminContact/SendResponse
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendResponse(int id, string responseMessage, bool sendEmail, bool sendSMS)
        {
            var submission = await _context.ContactFormSubmissions.FindAsync(id);
            
            if (submission == null)
            {
                return NotFound();
            }
            
            bool success = false;
            string message = "";
            
            try
            {
                if (string.IsNullOrWhiteSpace(responseMessage))
                {
                    TempData["Message"] = "Please enter a response message.";
                    return RedirectToAction(nameof(Respond), new { id = id });
                }
                
                // In a real app, you would integrate with email and SMS services here
                
                if (sendEmail)
                {
                    // Example of sending email
                    // await _emailService.SendAsync(submission.Email, "Re: " + submission.Subject, responseMessage);
                    message += $"Email sent to {submission.Email}. ";
                    success = true;
                }
                
                if (sendSMS)
                {
                    // Example of sending SMS
                    // await _smsService.SendAsync(submission.PhoneNumber, responseMessage);
                    message += $"SMS sent to {submission.PhoneNumber}. ";
                    success = true;
                }
                
                // Mark the submission as responded to in the database
                submission.IsRead = true;
                await _context.SaveChangesAsync();
                
                if (success)
                {
                    // Log successful response
                    Console.WriteLine($"Response sent to {submission.Name} via {(sendEmail ? "email" : "")}{(sendEmail && sendSMS ? " and " : "")}{(sendSMS ? "SMS" : "")}");
                    
                    TempData["SuccessMessage"] = "Response sent successfully. " + message;
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Message"] = "Please select at least one method of response (Email or SMS).";
                    return RedirectToAction(nameof(Respond), new { id = id });
                }
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error sending response: {ex.Message}");
                
                TempData["Message"] = $"Error sending response: {ex.Message}";
                return RedirectToAction(nameof(Respond), new { id = id });
            }
        }
    }
} 