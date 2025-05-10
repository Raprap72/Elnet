using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalStayHotel.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }
        
        [ForeignKey("User")]
        public int? UserId { get; set; }  // Null for system-wide notifications
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; }
        
        [Required]
        [StringLength(1000)]
        public string Message { get; set; }
        
        [Required]
        public NotificationType Type { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? ReadAt { get; set; }
        
        [Required]
        public bool IsRead { get; set; } = false;
        
        [Required]
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        
        // For linking to specific objects (e.g., booking, maintenance request)
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
        
        // For deep linking to specific pages
        public string? ActionLink { get; set; }
        
        // Navigation properties
        public virtual User? User { get; set; }
    }
    
    public enum NotificationType
    {
        Booking,
        Payment,
        Maintenance,
        System,
        UserActivity,
        Alert
    }
    
    public enum NotificationPriority
    {
        Low,
        Normal,
        High,
        Urgent
    }
} 