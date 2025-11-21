using MyGiftReg.Backend.Interfaces;
using MyGiftReg.Backend.Models;
using MyGiftReg.Backend.Models.DTOs;
using MyGiftReg.Backend.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace MyGiftReg.Backend.Services
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;

        public EventService(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        public async Task<Event?> CreateEventAsync(CreateEventRequest request, string userId)
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
                throw new MyGiftReg.Backend.Exceptions.ValidationException($"Event validation failed: {errorMessages}");
            }

            // Create the event entity
            var eventEntity = new Event
            {
                Name = request.Name,
                Description = request.Description,
                EventDate = request.EventDate?.ToUniversalTime(),
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow
            };

            // Check if event already exists
            var existingEvent = await _eventRepository.GetAsync(request.Name);
            if (existingEvent != null)
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException($"Event with name '{request.Name}' already exists.");
            }

            return await _eventRepository.CreateAsync(eventEntity);
        }

        public async Task<Event?> GetEventAsync(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Event name cannot be null or empty.");
            }

            return await _eventRepository.GetAsync(eventName);
        }

        public async Task<Event?> UpdateEventAsync(string eventName, CreateEventRequest request, string userId)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Event name cannot be null or empty.");
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
                throw new MyGiftReg.Backend.Exceptions.ValidationException($"Event validation failed: {errorMessages}");
            }

            // Get the existing event
            var existingEvent = await _eventRepository.GetAsync(eventName);
            if (existingEvent == null)
            {
                throw new NotFoundException($"Event with name '{eventName}' not found.");
            }

            // Update the event
            var eventEntity = new Event
            {
                Name = eventName,
                Description = request.Description,
                EventDate = request.EventDate,
                CreatedBy = existingEvent.CreatedBy,
                CreatedDate = existingEvent.CreatedDate
            };

            return await _eventRepository.UpdateAsync(eventName, eventEntity);
        }

        public async Task<bool> DeleteEventAsync(string eventName, string userId)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("Event name cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new MyGiftReg.Backend.Exceptions.ValidationException("User ID cannot be null or empty.");
            }

            return await _eventRepository.DeleteAsync(eventName);
        }

        public async Task<IList<Event>> GetAllEventsAsync()
        {
            return await _eventRepository.GetAllAsync();
        }
    }
}
