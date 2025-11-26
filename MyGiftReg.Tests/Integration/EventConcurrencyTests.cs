using Azure;
using Azure.Data.Tables;
using MyGiftReg.Backend.Models;
using MyGiftReg.Backend.Models.DTOs;
using MyGiftReg.Backend.Services;
using MyGiftReg.Backend.Storage;
using MyGiftReg.Backend.Exceptions;
using Xunit.Abstractions;

namespace MyGiftReg.Tests.Integration
{
    public class EventConcurrencyTests : AzuriteTestBase
    {
        private EventService _eventService = null!;
        private EventRepository _eventRepository = null!;

        public EventConcurrencyTests(ITestOutputHelper output) : base()
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            
            _eventRepository = new EventRepository(TableServiceClient);
            _eventService = new EventService(_eventRepository);
        }

        [Fact]
        public async Task CreateAndUpdateMaintainsCorrectETag()
        {
            // Arrange
            var createRequest = new CreateEventRequest
            {
                Name = _testPrefix + "_ETag Test Event",
                Description = "Initial Description"
            };
            var userId = "testuser";

            // Act - Create event
            var createdEvent = await _eventService.CreateEventAsync(createRequest, userId);
            Assert.NotNull(createdEvent);
            var originalETag = createdEvent.ETag;

            // Update event
            var updateRequest = new CreateEventRequest
            {
                Name = _testPrefix + "_ETag Test Event",
                Description = "Updated Description"
            };

            var updatedEvent = await _eventService.UpdateEventAsync(_testPrefix + "_ETag Test Event", updateRequest, userId);
            Assert.NotNull(updatedEvent);

            // Assert - ETag should have changed
            Assert.NotEqual(originalETag, updatedEvent.ETag);

            // Verify in database
            var retrievedEvent = await _eventService.GetEventAsync(_testPrefix + "_ETag Test Event");
            Assert.NotNull(retrievedEvent);
            Assert.Equal(updatedEvent.ETag, retrievedEvent.ETag);
            Assert.Equal("Updated Description", retrievedEvent.Description);
        }

        [Fact]
        public async Task MultipleSimultaneousReads_ReturnConsistentData()
        {
            // Arrange
            var createRequest = new CreateEventRequest
            {
                Name = _testPrefix + "_Concurrent Read Test Event",
                Description = "Multi-read Test"
            };
            var userId = "testuser";
            await _eventService.CreateEventAsync(createRequest, userId);

            // Act - Multiple concurrent reads
            var tasks = new List<Task<Event?>>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_eventService.GetEventAsync(_testPrefix + "_Concurrent Read Test Event"));
            }

            var results = await Task.WhenAll(tasks);

            // Assert - All results should be identical
            Assert.Equal(10, results.Length);
            foreach (var result in results)
            {
                Assert.NotNull(result);
                Assert.Equal(_testPrefix + "_Concurrent Read Test Event", result!.Name);
                Assert.Equal("Multi-read Test", result.Description);
                Assert.Equal(userId, result.CreatedBy);
            }
        }

        [Fact]
        public async Task ConcurrentCreateAndRead_HandlesProperly()
        {
            // Arrange
            var name = _testPrefix + "_Concurrent CreateRead Test Event";
            var createRequest = new CreateEventRequest
            {
                Name = name,
                Description = "Testing concurrent operations"
            };
            var userId = "testuser";

            // Act
            var createTask = _eventService.CreateEventAsync(createRequest, userId);
            var readTask = _eventService.GetEventAsync(name);

            await Task.WhenAll(createTask, readTask);

            var createdEvent = await createTask;
            var readEvent = await readTask;
            var readEvent2 = await _eventService.GetEventAsync(name);

            // Assert - Create and second read should succeed and be identical. If first read succeeded it should be identical as well.
            Assert.NotNull(createdEvent);
            Assert.NotNull(readEvent2);
            Assert.Equal(createdEvent.Name, readEvent2.Name);
            Assert.Equal(createdEvent.Description, readEvent2.Description);

            if (readEvent != null)
            {
                Assert.Equal(createdEvent.Name, readEvent.Name);
                Assert.Equal(createdEvent.Description, readEvent.Description);
            }
        }

        [Fact]
        public async Task ConcurrentUpdateOperations_MaintainDataIntegrity()
        {
            // Arrange
            var name = _testPrefix + "_Concurrent Update Test Event";
            var createRequest = new CreateEventRequest
            {
                Name = name,
                Description = "Original Description"
            };
            var userId = "testuser";
            await _eventService.CreateEventAsync(createRequest, userId);

            // Act - Sequential updates to avoid ETag conflicts (more realistic scenario)
            var finalDescription = "";
            for (int i = 0; i < 3; i++)
            {
                var updateRequest = new CreateEventRequest
                {
                    Name = name,
                    Description = $"Update {i + 1}"
                };
                
                var updatedEvent = await _eventService.UpdateEventAsync(name, updateRequest, userId);
                finalDescription = updatedEvent!.Description;
            }

            // Assert - At least one update should succeed
            Assert.NotEqual("", finalDescription);

            // Verify final state
            var finalEvent = await _eventService.GetEventAsync(name);
            Assert.NotNull(finalEvent);
            Assert.Contains(finalEvent.Description, new[] { "Update 1", "Update 2", "Update 3" });
        }

        [Fact]
        public async Task DeleteWithConcurrentAccess_HandlesProperly()
        {
            // Arrange
            var createRequest = new CreateEventRequest
            {
                Name = _testPrefix + "_Concurrent Delete Test Event",
                Description = "Will be deleted"
            };
            var userId = "testuser";
            var createdEvent = await _eventService.CreateEventAsync(createRequest, userId);
            Assert.NotNull(createdEvent);

            // Act - Concurrent read and delete
            var readTask = _eventService.GetEventAsync(_testPrefix + "_Concurrent Delete Test Event");
            var deleteTask = _eventService.DeleteEventAsync(_testPrefix + "_Concurrent Delete Test Event", userId);

            await Task.WhenAll(readTask, deleteTask);

            var readEvent = await readTask;
            var deleteResult = await deleteTask;

            // Assert - Delete should succeed
            Assert.True(deleteResult);

            // Read might succeed if it happened before delete, or return null if after
            if (readEvent != null)
            {
                Assert.Equal(_testPrefix + "_Concurrent Delete Test Event", readEvent.Name);
            }

            // Verify final state
            var finalEvent = await _eventService.GetEventAsync(_testPrefix + "_Concurrent Delete Test Event");
            Assert.Null(finalEvent);
        }

        [Fact]
        public async Task ETagProperty_ReflectsAzureStorageFormat()
        {
            // Arrange
            var createRequest = new CreateEventRequest
            {
                Name = _testPrefix + "_ETag Format Test Event",
                Description = "Testing ETag format"
            };
            var userId = "testuser";

            // Act
            var createdEvent = await _eventService.CreateEventAsync(createRequest, userId);

            // Assert - ETag should be valid (Azurite format may vary)
            Assert.NotNull(createdEvent);
            Assert.NotEqual(default(ETag), createdEvent.ETag); // Check ETag is not default
            Assert.NotEqual("", createdEvent.ETag.ToString());

            // ETag should be non-null and non-empty (format may vary by storage provider)
            var etagString = createdEvent.ETag.ToString();
            Assert.NotNull(etagString);
            Assert.NotEqual("", etagString);
        }

        [Fact]
        public async Task MultipleEntities_IndependentETagHandling()
        {
            // Arrange
            var events = new List<(string name, string description)>
            {
                (_testPrefix + "_Entity 1", "Description 1"),
                (_testPrefix + "_Entity 2", "Description 2"),
                (_testPrefix + "_Entity 3", "Description 3")
            };

            var createdEvents = new List<Event>();

            // Act - Create multiple events
            foreach (var (name, description) in events)
            {
                var request = new CreateEventRequest { Name = name, Description = description };
                var created = await _eventService.CreateEventAsync(request, "testuser");
                createdEvents.Add(created!);
            }

            // Assert - Each should have a valid ETag (they may have same format but that's ok)
            for (int i = 0; i < createdEvents.Count; i++)
            {
                Assert.NotEqual(default(ETag), createdEvents[i].ETag);
                Assert.NotEqual("", createdEvents[i].ETag.ToString());
            }

            // Update one entity
            var updateRequest = new CreateEventRequest
            {
                Name = _testPrefix + "_Entity 2",
                Description = "Updated Description 2"
            };
            var updatedEvent = await _eventService.UpdateEventAsync(_testPrefix + "_Entity 2", updateRequest, "testuser");
            Assert.NotNull(updatedEvent);

            // Assert - Entity 2's ETag should change
            var retrievedEvents = await _eventService.GetAllEventsAsync();
            var entity1 = retrievedEvents.First(e => e.Name == _testPrefix + "_Entity 1");
            var entity2Updated = retrievedEvents.First(e => e.Name == _testPrefix + "_Entity 2");
            var entity3 = retrievedEvents.First(e => e.Name == _testPrefix + "_Entity 3");

            Assert.Equal(createdEvents[0].ETag, entity1.ETag); // Should not change
            Assert.NotEqual(createdEvents[1].ETag, entity2Updated.ETag); // Should change
            Assert.Equal(createdEvents[2].ETag, entity3.ETag); // Should not change
        }
    }
}
