using System.ComponentModel.DataAnnotations;

namespace RoyalStayHotel.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        
        // Add Id property for backward compatibility
        public int Id { get => UserId; set => UserId = value; }
        
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        
        [Phone]
        public string? PhoneNumber { get; set; }
        
        // Add Phone property for backward compatibility
        public string? Phone { get => PhoneNumber; set => PhoneNumber = value; }
        
        [Required]
        public UserType UserType { get; set; } = UserType.Customer;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual ICollection<Booking>? Bookings { get; set; }
        public virtual ICollection<Review>? Reviews { get; set; }
        public virtual ICollection<Payment>? Payments { get; set; }
    }
    
    public enum UserType
    {
        Customer,
        Admin,
        Staff
    }
} 