using System.ComponentModel.DataAnnotations;

namespace RoyalStayHotel.Models
{
    public class SiteSetting
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string Value { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string Type { get; set; } = "text"; // text, toggle, textarea, etc.
    }
} 