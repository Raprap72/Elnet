using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;
using System.Linq;
using System.Threading.Tasks;

namespace RoyalStayHotel.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserController : AdminBaseController
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/User
        public async Task<IActionResult> Index(string searchString)
        {
            var users = _context.Users.AsQueryable();
            
            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(u => 
                    u.FullName.Contains(searchString) ||
                    u.Email.Contains(searchString) ||
                    u.Username.Contains(searchString));
            }
            
            return View(await users.ToListAsync());
        }

        // GET: Admin/User/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            // Get booking history for this user
            ViewBag.BookingHistory = await _context.Bookings
                .Where(b => b.UserId == id)
                .Include(b => b.Room)
                .OrderByDescending(b => b.CheckInDate)
                .Take(5)
                .ToListAsync();

            return View(user);
        }

        // GET: Admin/User/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,Email,Username,Password,Phone,IsAdmin")] User user)
        {
            if (ModelState.IsValid)
            {
                // Check if username or email already exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => 
                    u.Email == user.Email || u.Username == user.Username);
                
                if (existingUser != null)
                {
                    if (existingUser.Email == user.Email)
                    {
                        ModelState.AddModelError("Email", "This email is already in use.");
                    }
                    
                    if (existingUser.Username == user.Username)
                    {
                        ModelState.AddModelError("Username", "This username is already taken.");
                    }
                    
                    return View(user);
                }
                
                _context.Add(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "User created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Admin/User/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: Admin/User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,FullName,Email,Username,Password,Phone,IsAdmin")] User user)
        {
            if (id != user.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if username or email already exists (excluding current user)
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => 
                            (u.Email == user.Email || u.Username == user.Username) && 
                            u.UserId != user.UserId);
                    
                    if (existingUser != null)
                    {
                        if (existingUser.Email == user.Email)
                        {
                            ModelState.AddModelError("Email", "This email is already in use.");
                        }
                        
                        if (existingUser.Username == user.Username)
                        {
                            ModelState.AddModelError("Username", "This username is already taken.");
                        }
                        
                        return View(user);
                    }
                    
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "User updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Admin/User/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserId == id);
                
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Admin/User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            
            // Check if user has bookings
            var hasBookings = await _context.Bookings.AnyAsync(b => b.UserId == id);
            if (hasBookings)
            {
                TempData["ErrorMessage"] = "Cannot delete user as they have existing bookings.";
                return RedirectToAction(nameof(Index));
            }
            
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "User deleted successfully!";
            }
            
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/User/GetUserDetails
        [HttpGet]
        public async Task<IActionResult> GetUserDetails(int id)
        {
            var user = await _context.Users.FindAsync(id);
            
            if (user == null)
            {
                return NotFound();
            }
            
            var userDetails = new
            {
                userId = user.UserId,
                fullName = user.FullName,
                email = user.Email,
                phone = user.Phone
            };
            
            return Json(userDetails);
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
} 