using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalStayHotel.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }
        
        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }
        
        [Required]
        [ForeignKey("Booking")]
        public int BookingId { get; set; }
        
        [Required]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        [Required]
        public string PaymentMethod { get; set; } = string.Empty;
        
        [Required]
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        
        public string? PaymentDetails { get; set; }
        
        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual User? User { get; set; }
        public virtual Booking? Booking { get; set; }
    }
    
    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded,
        Cancelled
    }
} 