using MyGiftReg.Backend.Models;

namespace MyGiftReg.Backend.Interfaces
{
    public interface IGiftItemRepository
    {
        Task<GiftItem?> GetAsync(string giftListId, string itemId);
        Task<GiftItem> CreateAsync(GiftItem giftItemEntity);
        Task<GiftItem?> UpdateAsync(string giftListId, string itemId, GiftItem giftItemEntity);
        Task<bool> DeleteAsync(string giftListId, string itemId);
        Task<IList<GiftItem>> GetByGiftListAsync(string giftListId);
        Task<IList<GiftItem>> GetByGiftListExcludingReservationAsync(string giftListId, string userId);
    }
}
