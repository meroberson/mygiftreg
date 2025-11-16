using System.ComponentModel.DataAnnotations;

namespace MyGiftReg.Backend.Models.DTOs
{
    public class CreateGiftListRequest
    {
        [Required(ErrorMessage = "Gift list name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Event name is required")]
        public string EventName { get; set; } = string.Empty;
    }
}
