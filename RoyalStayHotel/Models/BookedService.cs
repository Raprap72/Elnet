using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalStayHotel.Models
{
    public class BookedService
    {
        [Key]
        public int BookingServiceId { get; set; }
        
        [Required]
        [ForeignKey("Booking")]
        public int BookingId { get; set; }
        
        [Required]
        [ForeignKey("Service")]
        public int ServiceId { get; set; }
        
        [Required]
        [Range(1, 100)]
        public int Quantity { get; set; }
        
        [Required]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }
        
        // Navigation properties
        public virtual Booking? Booking { get; set; }
        public virtual Service? Service { get; set; }
    }
} 