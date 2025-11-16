using MyGiftReg.Backend.Models;
using MyGiftReg.Backend.Models.DTOs;

namespace MyGiftReg.Backend.Interfaces
{
    public interface IEventService
    {
        Task<Event?> CreateEventAsync(CreateEventRequest request, string userId);
        Task<Event?> GetEventAsync(string eventName);
        Task<Event?> UpdateEventAsync(string eventName, CreateEventRequest request, string userId);
        Task<bool> DeleteEventAsync(string eventName, string userId);
        Task<IList<Event>> GetAllEventsAsync();
    }
}
