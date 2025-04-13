using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalStayHotel.Models
{
    public class RoomTypeInventory
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public RoomType RoomType { get; set; }

        [Required]
        public int Price { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        [Display(Name = "Total Rooms")]
        public int TotalRooms { get; set; }
        
        [Display(Name = "Description")]
        public string? Description { get; set; }
        
        // Calculated property - not stored in database
        [NotMapped]
        public int AvailableRooms { get; set; }
        
        [NotMapped]
        public int OccupiedRooms => TotalRooms - AvailableRooms;
    }
} 