using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalStayHotel.Models
{
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }
        
        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }
        
        [Required]
        [ForeignKey("Room")]
        public int RoomId { get; set; }
        
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }
        
        public string? Comment { get; set; }
        
        [Required]
        public DateTime ReviewDate { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual User? User { get; set; }
        public virtual Room? Room { get; set; }
    }
} 