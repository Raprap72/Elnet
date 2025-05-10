using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalStayHotel.Models
{
    public class MaintenanceRequest
    {
        [Key]
        public int Id { get; set; }
        
        // Add RequestId property for backward compatibility
        public int RequestId { get => Id; set => Id = value; }
        
        [Required]
        [ForeignKey("Room")]
        public int RoomId { get; set; }
        
        [ForeignKey("ReportedBy")]
        public int? ReportedById { get; set; }
        
        [ForeignKey("AssignedTechnician")]
        public int? TechnicianId { get; set; }
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string Description { get; set; }
        
        [Required]
        public MaintenanceRequestStatus Status { get; set; } = MaintenanceRequestStatus.Reported;
        
        [Required]
        public MaintenanceIssueType IssueType { get; set; }
        
        [Required]
        public int Priority { get; set; } = 2; // 1-Critical, 2-High, 3-Medium, 4-Low
        
        [Required]
        public DateTime RequestDate { get; set; } = DateTime.Now;

        // Add ReportedAt property
        public DateTime ReportedAt { get => RequestDate; set => RequestDate = value; }
        
        public DateTime? ScheduledFor { get; set; }
        
        public DateTime? CompletedAt { get; set; }
        
        public string? Resolution { get; set; }
        
        public decimal? CostOfRepair { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        // Navigation properties
        public virtual Room? Room { get; set; }
        public virtual User? ReportedBy { get; set; }
        public virtual User? AssignedTechnician { get; set; }
    }
    
    public enum MaintenanceRequestStatus
    {
        Reported,
        Scheduled,
        InProgress,
        OnHold,
        Completed,
        Cancelled
    }
    
    public enum MaintenanceIssueType
    {
        Plumbing,
        Electrical,
        Furniture,
        Appliance,
        HVAC,
        Structural,
        Other
    }
} 