using System.Collections.Generic;

namespace RoyalStayHotel.Models
{
    public class ServicesViewModel
    {
        public List<HotelService> MainServices { get; set; } = new List<HotelService>();
        public List<HotelService> AdditionalServices { get; set; } = new List<HotelService>();
    }
} 