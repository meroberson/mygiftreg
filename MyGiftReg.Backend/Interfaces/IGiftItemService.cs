using MyGiftReg.Backend.Models;
using MyGiftReg.Backend.Models.DTOs;

namespace MyGiftReg.Backend.Interfaces
{
    public interface IGiftItemService
    {
        Task<GiftItem?> CreateGiftItemAsync(string eventName, CreateGiftItemRequest request, string userId);
        Task<GiftItem?> GetGiftItemAsync(string eventName, string giftListId, string itemId, string viewerUserId);
        Task<GiftItem?> UpdateGiftItemAsync(string eventName, string giftListId, string itemId, CreateGiftItemRequest request, string userId);
        Task<bool> DeleteGiftItemAsync(string eventName, string giftListId, string itemId, string userId);
        Task<IList<GiftItem>> GetGiftItemsByListAsync(string eventName, string giftListId, string viewerUserId);
        Task<GiftItem?> ReserveGiftItemAsync(string eventName, string giftListId, string itemId, string userId);
        Task<bool> UnreserveGiftItemAsync(string eventName, string giftListId, string itemId, string userId);
    }
}
