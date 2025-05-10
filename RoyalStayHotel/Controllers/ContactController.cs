using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;

namespace RoyalStayHotel.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Contact
        public IActionResult Index()
        {
            return View();
        }

        // POST: Contact/Submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(ContactFormSubmission model)
        {
            if (ModelState.IsValid)
            {
                model.SubmissionDate = DateTime.Now;
                model.IsRead = false;

                _context.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thank you for contacting us. We will get back to you shortly.";
                return RedirectToAction(nameof(Index));
            }
            
            return View("Index", model);
        }
    }
} 