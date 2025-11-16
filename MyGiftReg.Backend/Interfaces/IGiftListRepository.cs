using MyGiftReg.Backend.Models;

namespace MyGiftReg.Backend.Interfaces
{
    public interface IGiftListRepository
    {
        Task<GiftList?> GetAsync(string eventName, string giftListId);
        Task<GiftList> CreateAsync(GiftList giftListEntity);
        Task<GiftList?> UpdateAsync(string eventName, string giftListId, GiftList giftListEntity);
        Task<bool> DeleteAsync(string eventName, string giftListId);
        Task<IList<GiftList>> GetByEventAsync(string eventName);
        Task<IList<GiftList>> GetByEventAndUserAsync(string eventName, string userId);
        Task<IList<GiftList>> GetByEventForOthersAsync(string eventName, string userId);
    }
}
