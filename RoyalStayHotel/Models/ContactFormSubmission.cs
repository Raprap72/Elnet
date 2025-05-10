using System;
using System.ComponentModel.DataAnnotations;

namespace RoyalStayHotel.Models
{
    public class ContactFormSubmission
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;
        
        [Required]
        public string Message { get; set; } = string.Empty;
        
        public DateTime SubmissionDate { get; set; } = DateTime.Now;
        
        public bool IsRead { get; set; } = false;
        
        public string? ResponseMessage { get; set; }
        
        public DateTime? RespondedAt { get; set; }
    }
} 