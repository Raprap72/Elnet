using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace RoyalStayHotel.Models
{
    public class RoomTypeInfo
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        public decimal BasePrice { get; set; }
        
        [StringLength(200)]
        public string ImageUrl { get; set; } = string.Empty;
        
        public int MaxOccupancy { get; set; }
        
        [StringLength(100)]
        public string BedType { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string Size { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string Amenities { get; set; } = string.Empty;
        
        // Navigation property
        public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
} 