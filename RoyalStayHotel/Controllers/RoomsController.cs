using Microsoft.AspNetCore.Mvc;
using RoyalStayHotel.Models;
using System;
using RoyalStayHotel.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace RoyalStayHotel.Controllers
{
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Rooms";
            var rooms = await _context.Rooms.ToListAsync();
            return View(rooms);
        }

        public async Task<IActionResult> Details(int id)
        {
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == id);
            if (room == null)
            {
                return NotFound();
            }
            return View(room);
        }

        [HttpPost]
        public async Task<IActionResult> FilterRooms(bool kingBed, bool doubleBeds, bool threeGuests, bool fourGuests)
        {
            var rooms = await _context.Rooms.ToListAsync();
            var filteredRooms = rooms.Where(r => 
                (!kingBed && !doubleBeds || kingBed && r.BedType.Contains("King") || doubleBeds && r.BedType.Contains("Double")) &&
                (!threeGuests && !fourGuests || threeGuests && r.MaxGuests >= 3 || fourGuests && r.MaxGuests >= 4)
            ).ToList();
            return PartialView("_RoomList", filteredRooms);
        }
    }
} 