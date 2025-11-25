using MyGiftReg.Backend.Models;
using MyGiftReg.Backend.Models.DTOs;

namespace MyGiftReg.Backend.Interfaces
{
    public interface IGiftListService
    {
        Task<GiftList?> CreateGiftListAsync(CreateGiftListRequest request, string userId, string userDisplayName);
        Task<GiftList?> GetGiftListAsync(string eventName, string giftListId);
        Task<GiftList?> UpdateGiftListAsync(string eventName, string giftListId, CreateGiftListRequest request, string userId);
        Task<bool> DeleteGiftListAsync(string eventName, string giftListId, string userId);
        Task<IList<GiftList>> GetGiftListsByEventAsync(string eventName);
        Task<IList<GiftList>> GetGiftListsByEventAndUserAsync(string eventName, string userId);
        Task<IList<GiftList>> GetGiftListsByEventForOthersAsync(string eventName, string userId);
    }
}
