using Microsoft.AspNetCore.Mvc;
using RoyalStayHotel.Models;
using System.Collections.Generic;

namespace RoyalStayHotel.Controllers
{
    public class RoomsController : Controller
    {
        // Normally this would come from a database
        private static List<Room> _rooms = new List<Room>
        {
            new Room 
            { 
                Id = 1, 
                Name = "Deluxe Room", 
                Price = 22628, 
                ImageUrl = "/images/deluxe_room.png",
                Description = "Experience luxury in our spacious Deluxe Room with modern amenities and elegant design.",
                HasKingBed = true,
                HasDoubleBeds = false,
                MaxGuests = 3
            },
            new Room 
            { 
                Id = 2, 
                Name = "Deluxe Suite Room", 
                Price = 33800, 
                ImageUrl = "/images/deluxe-suite_room.png",
                Description = "Upgrade your stay with our Deluxe Suite featuring a separate living area and premium furnishings.",
                HasKingBed = false,
                HasDoubleBeds = true,
                MaxGuests = 4
            },
            new Room 
            { 
                Id = 3, 
                Name = "Executive Deluxe Room", 
                Price = 55500, 
                ImageUrl = "/images/executive-deluxe_room.png",
                Description = "Our Executive Deluxe Room offers the ultimate luxury experience with panoramic views and exclusive amenities.",
                HasKingBed = true,
                HasDoubleBeds = true,
                MaxGuests = 4
            }
        };

        public IActionResult Index()
        {
            return View(_rooms);
        }

        public IActionResult Details(int id)
        {
            var room = _rooms.FirstOrDefault(r => r.Id == id);
            if (room == null)
            {
                return NotFound();
            }
            return View(room);
        }

        [HttpPost]
        public IActionResult FilterRooms(bool kingBed, bool doubleBeds, bool threeGuests, bool fourGuests)
        {
            var filteredRooms = _rooms.Where(r => 
                (!kingBed && !doubleBeds || kingBed && r.HasKingBed || doubleBeds && r.HasDoubleBeds) &&
                (!threeGuests && !fourGuests || threeGuests && r.MaxGuests >= 3 || fourGuests && r.MaxGuests >= 4)
            ).ToList();
            
            return PartialView("_RoomList", filteredRooms);
        }
    }
} 