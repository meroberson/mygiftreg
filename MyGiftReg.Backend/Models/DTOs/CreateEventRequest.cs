using System.ComponentModel.DataAnnotations;

namespace MyGiftReg.Backend.Models.DTOs
{
    public class CreateEventRequest
    {
        [Required(ErrorMessage = "Event name is required")]
        [StringLength(100, ErrorMessage = "Event name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;
        
        public DateTime? EventDate { get; set; }
    }
}
