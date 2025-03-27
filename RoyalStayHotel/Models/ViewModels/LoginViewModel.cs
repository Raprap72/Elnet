using System.ComponentModel.DataAnnotations;

namespace RoyalStayHotel.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        public required string Username { get; set; }
        
        [Required]
        [DataType(DataType.Password)]
        public required string Password { get; set; }
        
        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
} 