using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalStayHotel.Models
{
    public enum PaymentMethod
    {
        Cash,
        CreditCard,
        GCash
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }

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
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        
        // Compatibility property
        public PaymentStatus PaymentStatus { get => Status; set => Status = value; }
        
        [Required]
        public PaymentMethod PaymentMethod { get; set; }
        
        public string? TransactionId { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        // Payment details for various payment methods
        public string? PaymentDetails { get; set; }
        
        // Navigation properties
        public virtual User? User { get; set; }
        public virtual Booking? Booking { get; set; }
    }
} 