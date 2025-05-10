using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalStayHotel.Models
{
    public class HousekeepingTask
    {
        [Key]
        public int TaskId { get; set; }
        
        [Required]
        [ForeignKey("Room")]
        public int RoomId { get; set; }
        
        [ForeignKey("AssignedStaff")]
        public int? StaffId { get; set; }
        
        [Required]
        public string TaskDescription { get; set; } = string.Empty;
        
        [Required]
        public HousekeepingTaskType TaskType { get; set; }
        
        [Required]
        public HousekeepingTaskStatus Status { get; set; } = HousekeepingTaskStatus.Pending;
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? AssignedAt { get; set; }
        
        public DateTime? CompletedAt { get; set; }
        
        public string? Notes { get; set; }
        
        public int Priority { get; set; } = 2; // 1-High, 2-Medium, 3-Low
        
        // Navigation properties
        public virtual Room? Room { get; set; }
        public virtual User? AssignedStaff { get; set; }
    }
    
    public enum HousekeepingTaskType
    {
        Cleaning,
        RoomService,
        Maintenance,
        Restocking,
        SpecialRequest
    }
    
    public enum HousekeepingTaskStatus
    {
        Pending,
        Assigned,
        InProgress,
        Completed,
        Cancelled,
        Delayed
    }
} 