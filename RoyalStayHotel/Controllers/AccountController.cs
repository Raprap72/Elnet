using Microsoft.AspNetCore.Mvc;
using RoyalStayHotel.Models;
using RoyalStayHotel.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace RoyalStayHotel.Controllers
{
    public class AccountController : Controller
    {
        // In a real application, this would use Identity or a database
        private static List<User> _users = new List<User>();

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = _users.FirstOrDefault(u => u.Username == model.Username && u.Password == model.Password);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }

            // In a real application, we would sign in the user
            // using ASP.NET Core Identity

            return RedirectToAction("Index", "Home");
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

            return RedirectToAction("Index", "Home");
        }
    }
} 