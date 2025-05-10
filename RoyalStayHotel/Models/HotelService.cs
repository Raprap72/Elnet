using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalStayHotel.Models
{
    public enum ServiceType
    {
        Main,
        AdditionalService
    }
    
    public class HotelService
    {
        public int Id { get; set; }
        
        // ServiceId property for backward compatibility
        [NotMapped]
        public int ServiceId { get => Id; set => Id = value; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [Range(0, 10000)]
        public decimal Price { get; set; }
        
        public bool IsAvailable { get; set; } = true;
        
        public ServiceType ServiceType { get; set; } = ServiceType.Main;
    }
} 