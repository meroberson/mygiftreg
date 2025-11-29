using Azure.Data.Tables;
using MyGiftReg.Backend.Interfaces;
using MyGiftReg.Backend.Models;
using MyGiftReg.Backend.Exceptions;
using System.Net;

namespace MyGiftReg.Backend.Storage
{
    public class GiftItemRepository : BaseRepository<GiftItem>, IGiftItemRepository
    {
        public GiftItemRepository(TableServiceClient tableServiceClient) : base(tableServiceClient.GetTableClient("GiftItems"))
        {
        }

        public new async Task<GiftItem?> GetAsync(string giftListId, string itemId)
        {
            return await base.GetAsync(giftListId, itemId);
        }

        public new async Task<GiftItem> CreateAsync(GiftItem giftItemEntity)
        {
            giftItemEntity.PartitionKey = giftItemEntity.GiftListId.ToString();
            giftItemEntity.RowKey = giftItemEntity.Id.ToString();
            
            // Check if gift item already exists
            if (await ExistsAsync(giftItemEntity.GiftListId.ToString(), giftItemEntity.Id.ToString()))
            {
                throw new ValidationException($"Gift item with ID '{giftItemEntity.Id}' already exists in gift list '{giftItemEntity.GiftListId}'.");
            }

            await base.CreateAsync(giftItemEntity);
            return giftItemEntity;
        }

        public async Task<GiftItem?> UpdateAsync(string giftListId, string itemId, GiftItem giftItemEntity)
        {
            giftItemEntity.PartitionKey = giftListId;
            giftItemEntity.RowKey = itemId;
            
            // Verify entity exists
            var existingGiftItem = await GetAsync(giftListId, itemId);
            if (existingGiftItem == null)
            {
                throw new NotFoundException($"Gift item with ID '{itemId}' not found in gift list '{giftListId}'.");
            }

            // Preserve the original ETag for concurrency control
            giftItemEntity.ETag = existingGiftItem.ETag;
            giftItemEntity.Timestamp = existingGiftItem.Timestamp;
            
            return await UpdateAsync(giftItemEntity);
        }

        public new async Task<bool> DeleteAsync(string giftListId, string itemId)
        {
            // Verify entity exists before deletion
            var existingGiftItem = await GetAsync(giftListId, itemId);
            if (existingGiftItem == null)
            {
                return false;
            }

            await base.DeleteAsync(giftListId, itemId);
            return true;
        }

        public async Task<IList<GiftItem>> GetByGiftListAsync(string giftListId)
        {
            var allGiftItems = await GetAllAsync();
            return GetByPartitionKey(giftListId, allGiftItems).ToList();
        }

        public async Task<IList<GiftItem>> GetByGiftListExcludingReservationAsync(string giftListId, string userId)
        {
            var giftItems = await GetByGiftListAsync(giftListId);
            
            // Return all items, including reservation status for non-owners
            // This is handled at the service level, so we return all items
            return giftItems;
        }
    }
}
