using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalStayHotel.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }
        
        [Required]
        [ForeignKey("Booking")]
        public int BookingId { get; set; }
        
        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }
        
        [Required]
        public PaymentMethod PaymentMethod { get; set; }
        
        [Required]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }
        
        // Add Amount property for backward compatibility
        public decimal Amount { get => AmountPaid; set => AmountPaid = value; }
        
        [Required]
        public TransactionStatus TransactionStatus { get; set; }
        
        // Add Status property for backward compatibility
        public TransactionStatus Status { get => TransactionStatus; set => TransactionStatus = value; }
        
        [Required]
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        
        // Add PaymentDate property for backward compatibility
        public DateTime PaymentDate { get => TransactionDate; set => TransactionDate = value; }
        
        // Navigation properties
        public virtual Booking? Booking { get; set; }
        public virtual User? User { get; set; }
    }
    
    public enum PaymentMethod
    {
        CreditCard,
        DebitCard,
        Cash,
        BankTransfer,
        PayPal,
        GCash
    }
    
    public enum TransactionStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }
} 