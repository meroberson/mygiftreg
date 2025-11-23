using MyGiftReg.Backend.Interfaces;
using MyGiftReg.Backend.Models;
using MyGiftReg.Backend.Models.DTOs;
using MyGiftReg.Backend.Exceptions;
using MyGiftReg.Backend.Utilities;
using System.ComponentModel.DataAnnotations;

namespace MyGiftReg.Backend.Services
{
    public class GiftItemService : IGiftItemService
    {
        private readonly IGiftItemRepository _giftItemRepository;
        private readonly IGiftListRepository _giftListRepository;

        public GiftItemService(IGiftItemRepository giftItemRepository, IGiftListRepository giftListRepository)
        {
            _giftItemRepository = giftItemRepository;
            _giftListRepository = giftListRepository;
        }

        public async Task<GiftItem?> CreateGiftItemAsync(string eventName, CreateGiftItemRequest request, string userId)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Event name cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("User ID cannot be null or empty.");
            }

            // Validate event name conforms to Azure Storage naming restrictions
            AzureStorageValidator.ValidateEventNameForAzureStorage(eventName);

            // Validate the request
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(request);
            
            if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
            {
                var errorMessages = string.Join("; ", validationResults.Select(vr => vr.ErrorMessage));
                throw new MyGiftReg.Backend.Exceptions.ValidationException($"Gift item validation failed: {errorMessages}");
            }

            // Verify that the user owns the gift list
            var giftList = await _giftListRepository.GetAsync(eventName, request.GiftListId);
            if (giftList == null)
            {
                throw new NotFoundException($"Gift list with ID '{request.GiftListId}' not found in event '{eventName}'.");
            }

            if (giftList.Owner != userId)
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("You can only add items to gift lists that you own.");
            }

            // Create the gift item entity
            var giftItemEntity = new GiftItem
            {
                Name = request.Name,
                Description = request.Description,
                Url = request.Url,
                GiftListId = Guid.Parse(request.GiftListId),
                CreatedDate = DateTime.UtcNow
            };

            // Set PartitionKey and RowKey
            giftItemEntity.PartitionKey = request.GiftListId;
            giftItemEntity.RowKey = giftItemEntity.Id.ToString();

            try
            {
                return await _giftItemRepository.CreateAsync(giftItemEntity);
            }
            catch (MyGiftReg.Backend.Exceptions.ValidationException)
            {
                throw; // Re-throw validation exceptions from repository
            }
            catch (Exception ex)
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException($"Failed to create gift item: {ex.Message}");
            }
        }

        public async Task<GiftItem?> GetGiftItemAsync(string eventName, string giftListId, string itemId)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Event name cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(giftListId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Gift list ID cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Item ID cannot be null or empty.");
            }

            // Validate event name conforms to Azure Storage naming restrictions
            AzureStorageValidator.ValidateEventNameForAzureStorage(eventName);

            return await _giftItemRepository.GetAsync(giftListId, itemId);
        }

        public async Task<GiftItem?> UpdateGiftItemAsync(string eventName, string giftListId, string itemId, CreateGiftItemRequest request, string userId)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Event name cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(giftListId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Gift list ID cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Item ID cannot be null or empty.");
            }

            if (request == null)
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Request cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("User ID cannot be null or empty.");
            }

            // Validate event name conforms to Azure Storage naming restrictions
            AzureStorageValidator.ValidateEventNameForAzureStorage(eventName);

            // Validate the request
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(request);
            
            if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
            {
                var errorMessages = string.Join("; ", validationResults.Select(vr => vr.ErrorMessage));
                throw new MyGiftReg.Backend.Exceptions.ValidationException($"Gift item validation failed: {errorMessages}");
            }

            // Get the existing gift item
            var existingGiftItem = await _giftItemRepository.GetAsync(giftListId, itemId);
            if (existingGiftItem == null)
            {
                throw new NotFoundException($"Gift item with ID '{itemId}' not found in gift list '{giftListId}'.");
            }

            // Verify that the user owns the gift list containing this item
            var giftList = await _giftListRepository.GetAsync(eventName, giftListId);
            if (giftList == null || giftList.Owner != userId)
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("You can only update items in gift lists that you own.");
            }

            // Update the gift item (preserve reservation status and ID)
            var giftItemEntity = new GiftItem
            {
                Id = existingGiftItem.Id,
                Name = request.Name,
                Description = request.Description,
                Url = request.Url,
                GiftListId = existingGiftItem.GiftListId,
                ReservedBy = existingGiftItem.ReservedBy,
                CreatedDate = existingGiftItem.CreatedDate
            };

            return await _giftItemRepository.UpdateAsync(giftListId, itemId, giftItemEntity);
        }

        public async Task<bool> DeleteGiftItemAsync(string eventName, string giftListId, string itemId, string userId)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Event name cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(giftListId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Gift list ID cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Item ID cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("User ID cannot be null or empty.");
            }

            // Validate event name conforms to Azure Storage naming restrictions
            AzureStorageValidator.ValidateEventNameForAzureStorage(eventName);

            // Verify that the user owns the gift list containing this item
            var giftList = await _giftListRepository.GetAsync(eventName, giftListId);
            if (giftList == null)
            {
                return false; // Gift list not found
            }

            if (giftList.Owner != userId)
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("You can only delete items from gift lists that you own.");
            }

            return await _giftItemRepository.DeleteAsync(giftListId, itemId);
        }

        public async Task<IList<GiftItem>> GetGiftItemsByListAsync(string eventName, string giftListId, string viewerUserId)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Event name cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(giftListId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Gift list ID cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(viewerUserId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Viewer user ID cannot be null or empty.");
            }

            // Validate event name conforms to Azure Storage naming restrictions
            AzureStorageValidator.ValidateEventNameForAzureStorage(eventName);

            var allItems = await _giftItemRepository.GetByGiftListAsync(giftListId);

            // Get the gift list to determine ownership
            var giftList = await _giftListRepository.GetAsync(eventName, giftListId);
            if (giftList == null)
            {
                throw new NotFoundException($"Gift list with ID '{giftListId}' not found in event '{eventName}'.");
            }

            // If the viewer is the owner, hide reservation status
            if (giftList.Owner == viewerUserId)
            {
                foreach (var item in allItems)
                {
                    item.ReservedBy = null; // Hide reservation status from owner
                }
            }

            return allItems;
        }

        public async Task<GiftItem?> ReserveGiftItemAsync(string eventName, string giftListId, string itemId, string userId)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Event name cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(giftListId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Gift list ID cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Item ID cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("User ID cannot be null or empty.");
            }

            // Validate event name conforms to Azure Storage naming restrictions
            AzureStorageValidator.ValidateEventNameForAzureStorage(eventName);

            // Verify that the gift list exists and is not owned by the current user
            var giftList = await _giftListRepository.GetAsync(eventName, giftListId);
            if (giftList == null)
            {
                throw new NotFoundException($"Gift list with ID '{giftListId}' not found in event '{eventName}'.");
            }

            if (giftList.Owner == userId)
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("You cannot reserve items from your own gift list.");
            }

            // Get the existing gift item
            var existingGiftItem = await _giftItemRepository.GetAsync(giftListId, itemId);
            if (existingGiftItem == null)
            {
                throw new NotFoundException($"Gift item with ID '{itemId}' not found in gift list '{giftListId}'.");
            }

            // Check if item is already reserved
            if (!string.IsNullOrEmpty(existingGiftItem.ReservedBy))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Item is already reserved.");
            }

            // Reserve the item
            existingGiftItem.ReservedBy = userId;

            // Update the item
            try
            {
                var updatedItem = await _giftItemRepository.UpdateAsync(giftListId, itemId, existingGiftItem);
                return updatedItem;
            }
            catch (ConcurrencyException)
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Failed to reserve item due to concurrent modification.");
            }
        }

        public async Task<bool> UnreserveGiftItemAsync(string eventName, string giftListId, string itemId, string userId)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Event name cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(giftListId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Gift list ID cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Item ID cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("User ID cannot be null or empty.");
            }

            // Validate event name conforms to Azure Storage naming restrictions
            AzureStorageValidator.ValidateEventNameForAzureStorage(eventName);

            // Get the existing gift item
            var existingGiftItem = await _giftItemRepository.GetAsync(giftListId, itemId);
            if (existingGiftItem == null)
            {
                return false;
            }

            // Check if item is reserved by the specified user
            if (existingGiftItem.ReservedBy != userId)
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("You can only unreserve items that you have reserved.");
            }

            // Unreserve the item
            existingGiftItem.ReservedBy = null;

            // Update the item
            try
            {
                await _giftItemRepository.UpdateAsync(giftListId, itemId, existingGiftItem);
                return true;
            }
            catch (ConcurrencyException)
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Failed to unreserve item due to concurrent modification.");
            }
        }
    }
}
