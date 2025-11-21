using Azure.Data.Tables;
using MyGiftReg.Backend.Interfaces;
using MyGiftReg.Backend.Models;
using MyGiftReg.Backend.Exceptions;
using System.Net;

namespace MyGiftReg.Backend.Storage
{
    public class GiftListRepository : BaseRepository<GiftList>, IGiftListRepository
    {
        public GiftListRepository(TableServiceClient tableServiceClient) : base(tableServiceClient.GetTableClient("GiftLists"))
        {
        }

        public async Task<GiftList?> GetAsync(string eventName, string giftListId)
        {
            return await GetAsync(eventName, giftListId);
        }

        public async Task<GiftList> CreateAsync(GiftList giftListEntity)
        {
            giftListEntity.PartitionKey = giftListEntity.EventName;
            giftListEntity.RowKey = $"{giftListEntity.Owner}_{giftListEntity.Id}";
            
            // Check if gift list already exists
            if (await ExistsAsync(giftListEntity.EventName, giftListEntity.RowKey))
            {
                throw new ValidationException($"Gift list with owner '{giftListEntity.Owner}' already exists in event '{giftListEntity.EventName}'.");
            }

            await CreateAsync(giftListEntity);
            return giftListEntity;
        }

        public async Task<GiftList?> UpdateAsync(string eventName, string giftListId, GiftList giftListEntity)
        {
            giftListEntity.PartitionKey = eventName;
            giftListEntity.RowKey = giftListId;
            
            // Verify entity exists
            var existingGiftList = await GetAsync(eventName, giftListId);
            if (existingGiftList == null)
            {
                throw new NotFoundException($"Gift list with ID '{giftListId}' not found in event '{eventName}'.");
            }

            // Preserve the original ETag for concurrency control
            giftListEntity.ETag = existingGiftList.ETag;
            giftListEntity.Timestamp = existingGiftList.Timestamp;
            
            return await UpdateAsync(giftListEntity);
        }

        public async Task<bool> DeleteAsync(string eventName, string giftListId)
        {
            // Verify entity exists before deletion
            var existingGiftList = await GetAsync(eventName, giftListId);
            if (existingGiftList == null)
            {
                return false;
            }

            await DeleteAsync(eventName, giftListId);
            return true;
        }

        public async Task<IList<GiftList>> GetByEventAsync(string eventName)
        {
            var allGiftLists = await GetAllAsync();
            return GetByPartitionKey(eventName, allGiftLists).ToList();
        }

        public async Task<IList<GiftList>> GetByEventAndUserAsync(string eventName, string userId)
        {
            var eventGiftLists = await GetByEventAsync(eventName);
            return eventGiftLists.Where(gl => gl.Owner == userId).ToList();
        }

        public async Task<IList<GiftList>> GetByEventForOthersAsync(string eventName, string userId)
        {
            var eventGiftLists = await GetByEventAsync(eventName);
            return eventGiftLists.Where(gl => gl.Owner != userId).ToList();
        }
    }
}
