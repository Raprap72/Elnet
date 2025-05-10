using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalStayHotel.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required]
        [StringLength(20)]
        public string BookingReference { get; set; } = "";

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
        
        // Discount relationship
        [ForeignKey("AppliedDiscount")]
        public int? AppliedDiscountId { get; set; }
        
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountAmount { get; set; }
        
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? OriginalPrice { get; set; }
        
        [StringLength(500)]
        public string? SpecialRequests { get; set; }
        
        // Navigation properties
        public virtual User? User { get; set; }
        public virtual Room? Room { get; set; }
        public virtual Discount? AppliedDiscount { get; set; }
        public virtual ICollection<BookedService>? BookedServices { get; set; }
        public virtual ICollection<Payment>? Payments { get; set; }

        private static string GenerateBookingReference()
        {
            // Format: RS-YYYYMMDD-XXXX where XXXX is a random number
            return $"RS-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        }

        public Booking()
        {
            BookingReference = GenerateBookingReference();
        }
    }
    
    public enum BookingStatus
    {
        Pending,
        Confirmed,
        Declined,
        CheckedIn,
        CheckedOut,
        Cancelled,
        NoShow,
        Completed
    }
} 