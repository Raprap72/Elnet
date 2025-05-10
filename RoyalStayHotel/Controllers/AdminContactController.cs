using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;

namespace RoyalStayHotel.Controllers
{
    public class AdminContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AdminContact
        public async Task<IActionResult> Index()
        {
            var submissions = await _context.ContactFormSubmissions
                .OrderByDescending(s => s.SubmissionDate)
                .ToListAsync();
            
            return View(submissions);
        }

        // GET: AdminContact/Details/5
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

        // POST: AdminContact/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var submission = await _context.ContactFormSubmissions.FindAsync(id);
            
            if (submission != null)
            {
                _context.ContactFormSubmissions.Remove(submission);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
} 