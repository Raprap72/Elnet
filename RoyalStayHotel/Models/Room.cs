using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalStayHotel.Models
{
    public class Room
    {
        [Key]
        public int RoomId { get; set; }
        
        // Add Id property to match old code usage
        public int Id { get => RoomId; set => RoomId = value; }
        
        [Required]
        public string RoomNumber { get; set; } = string.Empty;
        
        [Required]
        public RoomType RoomType { get; set; }
        
        // Add Name property for backward compatibility
        public string Name { get => RoomType.ToString(); set { } }
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Price Per Night")]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerNight { get; set; }
        
        // Add Price property to match old code usage
        public decimal Price { get => PricePerNight; set => PricePerNight = value; }
        
        [Required]
        [Range(1, 10)]
        public int MaxGuests { get; set; }
        
        // Add Capacity property for compatibility
        public int Capacity { get => MaxGuests; set => MaxGuests = value; }
        
        [Required]
        public string BedType { get; set; } = string.Empty;
        
        // Add bed type properties for compatibility
        public bool HasKingBed { get; set; }
        public bool HasDoubleBeds { get; set; }
        
        [Required]
        public string RoomSize { get; set; } = string.Empty;
        
        [Required]
        public AvailabilityStatus AvailabilityStatus { get; set; } = AvailabilityStatus.Available;
        
        // Add IsAvailable property for compatibility
        public bool IsAvailable 
        { 
            get => AvailabilityStatus == AvailabilityStatus.Available; 
            set => AvailabilityStatus = value ? AvailabilityStatus.Available : AvailabilityStatus.Booked; 
        }
        
        // Additional properties
        public string ImageUrl { get; set; } = string.Empty;
        
        // Navigation properties
        public virtual ICollection<Booking>? Bookings { get; set; }
        public virtual ICollection<Review>? Reviews { get; set; }
    }
    
    public enum RoomType
    {
        Deluxe,
        DeluxeSuite,
        ExecutiveDeluxe,
        Presidential,
        Standard
    }
    
    public enum AvailabilityStatus
    {
        Available,
        Booked,
        Maintenance,
        Cleaning
    }
} 