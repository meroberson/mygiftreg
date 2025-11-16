using MyGiftReg.Backend.Models;

namespace MyGiftReg.Backend.Interfaces
{
    public interface IEventRepository
    {
        Task<Event?> GetAsync(string eventName);
        Task<Event> CreateAsync(Event eventEntity);
        Task<Event?> UpdateAsync(string eventName, Event eventEntity);
        Task<bool> DeleteAsync(string eventName);
        Task<IList<Event>> GetAllAsync();
    }
}
