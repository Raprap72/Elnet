using Microsoft.AspNetCore.Mvc;
using RoyalStayHotel.Models;
using RoyalStayHotel.Models.ViewModels;
using RoyalStayHotel.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RoyalStayHotel.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // In a real application, this would use Identity or a database
        private static List<User> _users = new List<User>();

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel { Username = string.Empty, Password = string.Empty, RememberMe = false });
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = _context.Users.FirstOrDefault(u => u.Username == model.Username && u.Password == model.Password);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

            // Redirect based on user type
            if (user.UserType == UserType.Admin)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            else if (user.UserType == UserType.Staff)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Staff" });
            }
            else
            {
                ModelState.AddModelError("", "Access denied. Only staff and admin can log in here.");
                return View(model);
            }
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (_users.Any(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Username already exists.");
                return View(model);
            }

            if (_users.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View(model);
            }

            var user = new User
            {
                Id = _users.Count + 1,
                FullName = model.FullName,
                Email = model.Email,
                Username = model.Username,
                Password = model.Password // In a real app, we would hash this
            };

            _users.Add(user);

            // In a real application, we would sign in the user

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            // In a real application, we would sign out the user
            return RedirectToAction("Login");
        }
    }
} 