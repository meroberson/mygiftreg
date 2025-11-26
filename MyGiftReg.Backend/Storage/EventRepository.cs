using Azure.Data.Tables;
using MyGiftReg.Backend.Interfaces;
using MyGiftReg.Backend.Models;
using MyGiftReg.Backend.Exceptions;
using System.Net;

namespace MyGiftReg.Backend.Storage
{
    public class EventRepository : BaseRepository<Event>, IEventRepository
    {
        public EventRepository(TableServiceClient tableServiceClient) : base(tableServiceClient.GetTableClient("Events"))
        {
        }

        public async Task<Event?> GetAsync(string eventName)
        {
            // For EventTable: PartitionKey="", RowKey=EventName
            return await GetAsync("", eventName);
        }

        public new async Task<Event> CreateAsync(Event eventEntity)
        {
            eventEntity.PartitionKey = "";
            eventEntity.RowKey = eventEntity.Name;
            
            // Check if event already exists
            if (await ExistsAsync("", eventEntity.Name))
            {
                throw new ValidationException($"Event with name '{eventEntity.Name}' already exists.");
            }

            eventEntity = await base.CreateAsync(eventEntity);
            return eventEntity;
        }

        public async Task<Event?> UpdateAsync(string eventName, Event eventEntity)
        {
            eventEntity.PartitionKey = "";
            eventEntity.RowKey = eventName;
            
            // Verify entity exists
            var existingEvent = await GetEventAsync(eventName);
            if (existingEvent == null)
            {
                throw new NotFoundException($"Event with name '{eventName}' not found.");
            }

            // Preserve the original ETag for concurrency control
            eventEntity.ETag = existingEvent.ETag;
            eventEntity.Timestamp = existingEvent.Timestamp;
            
            return await UpdateAsync(eventEntity);
        }

        public async Task<bool> DeleteAsync(string eventName)
        {
            // Verify entity exists before deletion
            var existingEvent = await GetEventAsync(eventName);
            if (existingEvent == null)
            {
                return false;
            }

            await DeleteAsync("", eventName);
            return true;
        }

        public new async Task<IList<Event>> GetAllAsync()
        {
            var allEvents = await base.GetAllAsync();
            return allEvents.Where(e => e.PartitionKey == "").ToList();
        }

        private async Task<Event?> GetEventAsync(string eventName)
        {
            return await GetAsync("", eventName);
        }
    }
}
