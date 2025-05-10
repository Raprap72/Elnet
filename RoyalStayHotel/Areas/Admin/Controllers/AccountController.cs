using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;

namespace RoyalStayHotel.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Clear any existing session
            HttpContext.Session.Clear();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            Console.WriteLine($"Login attempt - Username: {username}, Password length: {password?.Length ?? 0}");
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Username and password are required.");
                return View();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                Console.WriteLine("Login failed - User not found");
                ModelState.AddModelError("", "Invalid username or password.");
                return View();
            }
            
            // For testing purposes, directly compare passwords without hashing
            bool passwordMatches = (user.Password == password);
            Console.WriteLine($"Stored password: {user.Password}");
            Console.WriteLine($"Input password: {password}");
            Console.WriteLine($"Password comparison result: {passwordMatches}");

            if (!passwordMatches)
            {
                Console.WriteLine("Login failed - Password mismatch");
                ModelState.AddModelError("", "Invalid username or password.");
                return View();
            }

            // Set session first
            HttpContext.Session.SetInt32("AdminId", user.Id ?? 0);

            // Then set authentication cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id?.ToString() ?? "0"),
                new Claim("FullName", user.FullName),
                new Claim("UserType", user.UserType.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync("Cookies", claimsPrincipal);

            return RedirectToAction("Index", "Home", new { area = "Admin" });
        }

        // Helper method to hash passwords - same as in UserController
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var hashedPassword = Convert.ToBase64String(hashedBytes);
                Console.WriteLine($"LOGIN DEBUG - Original password: {password}");
                Console.WriteLine($"LOGIN DEBUG - Hashed password: {hashedPassword}");
                return hashedPassword;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Clear session first
            HttpContext.Session.Clear();
            
            // Then sign out
            await HttpContext.SignOutAsync("Cookies");
            
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int id))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(User user)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _context.Users.FindAsync(user.UserId);
                if (existingUser == null)
                {
                    return NotFound();
                }

                existingUser.FullName = user.FullName;
                existingUser.Email = user.Email;
                existingUser.PhoneNumber = user.PhoneNumber;
                
                // Make sure Id is synced with UserId
                existingUser.Id = user.UserId;

                if (!string.IsNullOrEmpty(user.Password))
                {
                    existingUser.Password = user.Password; // In production, use password hashing
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Dashboard");
            }

            return View(user);
        }
    }
} 