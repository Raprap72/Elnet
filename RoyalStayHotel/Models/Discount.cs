using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalStayHotel.Models
{
    public class Discount
    {
        [Key]
        public int DiscountId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Code { get; set; }
        
        // Compatibility property for DiscountCode
        public string DiscountCode { get => Code; set => Code = value; }
        
        [Required]
        [StringLength(200)]
        public string Description { get; set; }
        
        [Required]
        public decimal DiscountAmount { get; set; }
        
        public bool IsPercentage { get; set; }
        
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public int? MinimumStay { get; set; }
        
        public decimal? MinimumSpend { get; set; }
        
        public int? MaxUsage { get; set; }
        
        // Compatibility property for MaxUses
        public int? MaxUses { get => MaxUsage; set => MaxUsage = value; }
        
        public int UsageCount { get; set; } = 0;
        
        public DiscountType Type { get; set; }
        
        [ForeignKey("ApplicableRoomType")]
        public int? RoomTypeId { get; set; }
        
        public RoomType? ApplicableRoomType { get; set; }
        
        // Navigation properties
        public virtual ICollection<Booking>? AppliedBookings { get; set; }
    }
    
    public enum DiscountType
    {
        RoomRate,
        Service,
        Package,
        Seasonal,
        Special
    }
} 