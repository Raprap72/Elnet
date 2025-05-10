using Microsoft.AspNetCore.Mvc;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace RoyalStayHotel.Controllers
{
    public class SetupController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SetupController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Setup
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Setup/CreateTestUsers
        public IActionResult CreateTestUsers()
        {
            try
            {
                // Clear existing users first
                var existingUsers = _context.Users.ToList();
                _context.Users.RemoveRange(existingUsers);
                _context.SaveChanges();

                // Create admin user with plaintext password
                var adminUser = new User
                {
                    FullName = "Admin User",
                    Email = "admin@royalstay.com",
                    Username = "admin",
                    Password = "Admin123!", // Plaintext for testing
                    PhoneNumber = "123-456-7890",
                    UserType = UserType.Admin,
                    CreatedAt = DateTime.Now
                };
                _context.Users.Add(adminUser);

                // Create test user
                var testUser = new User
                {
                    FullName = "Test User",
                    Email = "test@test.com",
                    Username = "test",
                    Password = "test123", // Plaintext for testing
                    PhoneNumber = "987-654-3210",
                    UserType = UserType.Admin, // Make it admin for testing
                    CreatedAt = DateTime.Now
                };
                _context.Users.Add(testUser);

                _context.SaveChanges();
                
                return Content($"Test users created successfully!<br><br>" +
                    $"Admin User: Username = admin, Password = Admin123!<br>" +
                    $"Test User: Username = test, Password = test123<br><br>" +
                    $"<a href='/Admin/Account/Login'>Go to login page</a>");
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}");
            }
        }
    }
} 