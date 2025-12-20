using System;

namespace MyGiftReg.Frontend.Models
{
    public class ShoppingListItemViewModel
    {
        public string GiftListId { get; set; } = string.Empty;
        public string GiftListName { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Url { get; set; }
        public int QuantityReserved { get; set; }
        public int QuantityTotal { get; set; }
    }
}
