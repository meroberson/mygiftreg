using MyGiftReg.Backend.Models;
using MyGiftReg.Backend.Models.DTOs;

namespace MyGiftReg.Backend.Interfaces
{
    public interface IGiftItemService
    {
        Task<GiftItem?> CreateGiftItemAsync(CreateGiftItemRequest request, string userId);
        Task<GiftItem?> GetGiftItemAsync(string giftListId, string itemId);
        Task<GiftItem?> UpdateGiftItemAsync(string giftListId, string itemId, CreateGiftItemRequest request, string userId);
        Task<bool> DeleteGiftItemAsync(string giftListId, string itemId, string userId);
        Task<IList<GiftItem>> GetGiftItemsByListAsync(string giftListId, string viewerUserId);
        Task<GiftItem?> ReserveGiftItemAsync(string giftListId, string itemId, string userId);
        Task<bool> UnreserveGiftItemAsync(string giftListId, string itemId, string userId);
    }
}
