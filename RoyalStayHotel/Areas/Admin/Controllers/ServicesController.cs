using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RoyalStayHotel.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ServicesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ServicesController> _logger;

        public ServicesController(ApplicationDbContext context, ILogger<ServicesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index(string sortOrder = "name_asc")
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["IdSort"] = sortOrder == "id_asc" ? "id_desc" : "id_asc";
            ViewData["NameSort"] = sortOrder == "name_asc" ? "name_desc" : "name_asc";
            ViewData["PriceSort"] = sortOrder == "price_asc" ? "price_desc" : "price_asc";
            ViewData["TypeSort"] = sortOrder == "type_asc" ? "type_desc" : "type_asc";
            ViewData["StatusSort"] = sortOrder == "status_asc" ? "status_desc" : "status_asc";
            
            // Get all hotel services
            var services = _context.HotelServices.AsQueryable();
            
            // Apply sorting
            services = sortOrder switch
            {
                "id_asc" => services.OrderBy(s => s.Id),
                "id_desc" => services.OrderByDescending(s => s.Id),
                "name_desc" => services.OrderByDescending(s => s.Name),
                "price_asc" => services.OrderBy(s => s.Price),
                "price_desc" => services.OrderByDescending(s => s.Price),
                "type_asc" => services.OrderBy(s => s.ServiceType),
                "type_desc" => services.OrderByDescending(s => s.ServiceType),
                "status_asc" => services.OrderBy(s => s.IsAvailable),
                "status_desc" => services.OrderByDescending(s => s.IsAvailable),
                _ => services.OrderBy(s => s.Name) // Default sort by name ascending
            };
                
            return View(services.ToList());
        }
        
        // GET: Admin/Services/Bookings
        public async Task<IActionResult> Bookings()
        {
            var bookings = await _context.BookedServices
                .Include(b => b.Service)
                .Include(b => b.Booking)
                    .ThenInclude(booking => booking.Room)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
                
            return View(bookings);
        }
        
        // POST: Admin/Services/UpdateBookingStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookingStatus(int id, string status)
        {
            var booking = await _context.BookedServices.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }
            
            booking.Status = status;
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Booking status updated successfully!";
            return RedirectToAction(nameof(Bookings));
        }

        // GET: Services/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Services/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HotelService service)
        {
            if (ModelState.IsValid)
            {
                _context.Add(service);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Service created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(service);
        }

        // GET: Services/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var service = await _context.HotelServices.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }
            return View(service);
        }

        // POST: Services/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, HotelService service)
        {
            if (id != service.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Update(service);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Service updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(service);
        }

        // GET: Services/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var service = await _context.HotelServices.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }

            return View(service);
        }

        // POST: Services/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var service = await _context.HotelServices.FindAsync(id);
            if (service != null)
            {
                _context.HotelServices.Remove(service);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Service '{service.Name}' (ID: {id}) deleted from database");
                TempData["SuccessMessage"] = "Service deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }
        
        // POST: Services/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var service = await _context.HotelServices.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }
            
            // Toggle the availability status
            service.IsAvailable = !service.IsAvailable;
            
            await _context.SaveChangesAsync();
            
            string status = service.IsAvailable ? "activated" : "deactivated";
            TempData["SuccessMessage"] = $"Service {status} successfully!";
            
            return RedirectToAction(nameof(Index));
        }
    }
} 