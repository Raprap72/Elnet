using System.ComponentModel.DataAnnotations;

namespace RoyalStayHotel.Models
{
    public class UserActivityLog
    {
        [Key]
        public int LogId { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public string Action { get; set; } = string.Empty;
        
        public int? AdminId { get; set; }
        
        [Required]
        public DateTime Timestamp { get; set; }
        
        // Navigation properties
        public virtual User? User { get; set; }
        public virtual User? Admin { get; set; }
    }
} 