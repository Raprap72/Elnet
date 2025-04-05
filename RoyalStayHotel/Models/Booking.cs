using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalStayHotel.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }
        
        // Add Id property for backward compatibility
        public int Id { get => BookingId; set => BookingId = value; }
        
        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }
        
        [Required]
        [ForeignKey("Room")]
        public int RoomId { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Check In Date")]
        public DateTime CheckInDate { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Check Out Date")]
        public DateTime CheckOutDate { get; set; }
        
        [Required]
        [Range(1, 10)]
        public int NumberOfGuests { get; set; }
        
        [Required]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }
        
        [Required]
        public BookingStatus Status { get; set; } = BookingStatus.Pending;
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Add BookingDate property for backward compatibility
        public DateTime BookingDate { get => CreatedAt; set => CreatedAt = value; }
        
        // Navigation properties
        public virtual User? User { get; set; }
        public virtual Room? Room { get; set; }
        public virtual ICollection<BookedService>? BookedServices { get; set; }
        public virtual ICollection<Payment>? Payments { get; set; }
    }
    
    public enum BookingStatus
    {
        Pending,
        Confirmed,
        Declined,
        CheckedIn,
        CheckedOut,
        Cancelled,
        NoShow
    }
} 