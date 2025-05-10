using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalStayHotel.Models
{
    public class BookedService
    {
        [Key]
        public int Id { get; set; }
        
        // BookingServiceId property for backward compatibility
        [NotMapped]
        public int BookingServiceId { get => Id; set => Id = value; }
        
        [Required]
        public int ServiceId { get; set; }
        
        [ForeignKey("ServiceId")]
        public HotelService? Service { get; set; }
        
        public int? BookingId { get; set; }
        
        [ForeignKey("BookingId")]
        public virtual Booking? Booking { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime RequestDate { get; set; }
        
        [DataType(DataType.Time)]
        public TimeSpan RequestTime { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        [Range(1, 100)]
        public int Quantity { get; set; } = 1;
        
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Completed, Cancelled
    }
} 