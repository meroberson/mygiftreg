using MyGiftReg.Backend.Interfaces;
using MyGiftReg.Backend.Models;
using MyGiftReg.Backend.Models.DTOs;
using MyGiftReg.Backend.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace MyGiftReg.Backend.Services
{
    public class GiftListService : IGiftListService
    {
        private readonly IGiftListRepository _giftListRepository;

        public GiftListService(IGiftListRepository giftListRepository)
        {
            _giftListRepository = giftListRepository;
        }

        public async Task<GiftList?> CreateGiftListAsync(CreateGiftListRequest request, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("User ID cannot be null or empty.");
            }

            // Validate the request
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(request);
            
            if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
            {
                var errorMessages = string.Join("; ", validationResults.Select(vr => vr.ErrorMessage));
                throw new MyGiftReg.Backend.Exceptions.ValidationException($"Gift list validation failed: {errorMessages}");
            }

            // Create the gift list entity
            var giftListEntity = new GiftList
            {
                Name = request.Name,
                EventName = request.EventName,
                Owner = userId,
                CreatedDate = DateTime.UtcNow
            };

            // Generate RowKey as Owner_Guid format
            giftListEntity.RowKey = $"{userId}_{giftListEntity.Id}";

            try
            {
                return await _giftListRepository.CreateAsync(giftListEntity);
            }
            catch (MyGiftReg.Backend.Exceptions.ValidationException)
            {
                throw; // Re-throw validation exceptions from repository
            }
            catch (Exception ex)
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException($"Failed to create gift list: {ex.Message}");
            }
        }

        public async Task<GiftList?> GetGiftListAsync(string eventName, string giftListId)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Event name cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(giftListId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Gift list ID cannot be null or empty.");
            }

            return await _giftListRepository.GetAsync(eventName, giftListId);
        }

        public async Task<GiftList?> UpdateGiftListAsync(string eventName, string giftListId, CreateGiftListRequest request, string userId)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Event name cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(giftListId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Gift list ID cannot be null or empty.");
            }

            if (request == null)
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Request cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("User ID cannot be null or empty.");
            }

            // Validate the request
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(request);
            
            if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
            {
                var errorMessages = string.Join("; ", validationResults.Select(vr => vr.ErrorMessage));
                throw new MyGiftReg.Backend.Exceptions.ValidationException($"Gift list validation failed: {errorMessages}");
            }

            // Get the existing gift list
            var existingGiftList = await _giftListRepository.GetAsync(eventName, giftListId);
            if (existingGiftList == null)
            {
                throw new NotFoundException($"Gift list with ID '{giftListId}' not found in event '{eventName}'.");
            }

            // Verify ownership
            if (existingGiftList.Owner != userId)
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("You can only update gift lists that you own.");
            }

            // Update the gift list
            var giftListEntity = new GiftList
            {
                Id = existingGiftList.Id,
                Name = request.Name,
                EventName = request.EventName,
                Owner = existingGiftList.Owner,
                CreatedDate = existingGiftList.CreatedDate
            };

            return await _giftListRepository.UpdateAsync(eventName, giftListId, giftListEntity);
        }

        public async Task<bool> DeleteGiftListAsync(string eventName, string giftListId, string userId)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Event name cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(giftListId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Gift list ID cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("User ID cannot be null or empty.");
            }

            // Get the existing gift list to verify ownership
            var existingGiftList = await _giftListRepository.GetAsync(eventName, giftListId);
            if (existingGiftList == null)
            {
                return false; // Not found, return false
            }

            // Verify ownership
            if (existingGiftList.Owner != userId)
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("You can only delete gift lists that you own.");
            }

            return await _giftListRepository.DeleteAsync(eventName, giftListId);
        }

        public async Task<IList<GiftList>> GetGiftListsByEventAsync(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Event name cannot be null or empty.");
            }

            return await _giftListRepository.GetByEventAsync(eventName);
        }

        public async Task<IList<GiftList>> GetGiftListsByEventAndUserAsync(string eventName, string userId)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Event name cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("User ID cannot be null or empty.");
            }

            return await _giftListRepository.GetByEventAndUserAsync(eventName, userId);
        }

        public async Task<IList<GiftList>> GetGiftListsByEventForOthersAsync(string eventName, string userId)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Event name cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("User ID cannot be null or empty.");
            }

            return await _giftListRepository.GetByEventForOthersAsync(eventName, userId);
        }
    }
}
