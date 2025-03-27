using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalStayHotel.Models
{
    public class Service
    {
        [Key]
        public int ServiceId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ServiceName { get; set; } = string.Empty;
        
        // Add Name property for backward compatibility
        public string Name { get => ServiceName; set => ServiceName = value; }
        
        [Required]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        
        public string? Description { get; set; }
        
        // Navigation properties
        public virtual ICollection<BookedService>? BookedServices { get; set; }
    }
} 