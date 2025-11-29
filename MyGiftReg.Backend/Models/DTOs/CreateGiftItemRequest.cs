using System.ComponentModel.DataAnnotations;

namespace MyGiftReg.Backend.Models.DTOs
{
    public class CreateGiftItemRequest
    {
        [Required(ErrorMessage = "Item name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;
        
        [Url(ErrorMessage = "Please provide a valid URL")]
        public string? Url { get; set; }
        
        [Required(ErrorMessage = "Gift list ID is required")]
        public string GiftListId { get; set; } = string.Empty;
        
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; } = 1;
    }
}
