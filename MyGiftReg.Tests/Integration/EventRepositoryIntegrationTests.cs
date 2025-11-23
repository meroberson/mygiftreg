using Azure;
using Azure.Data.Tables;
using MyGiftReg.Backend.Models;
using MyGiftReg.Backend.Services;
using MyGiftReg.Backend.Storage;
using MyGiftReg.Backend.Exceptions;
using Xunit.Abstractions;

namespace MyGiftReg.Tests.Integration
{
    public class EventRepositoryIntegrationTests : AzuriteTestBase
    {
        private EventRepository _eventRepository = null!;
        private AzureTableConfig _tableConfig = null!;

        public EventRepositoryIntegrationTests(ITestOutputHelper output) : base()
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            
            _eventRepository = new EventRepository(TableServiceClient);
            _tableConfig = ServiceProvider.GetRequiredService<AzureTableConfig>();
        }

        [Fact]
        public async Task CreateEventAsync_ValidEvent_ReturnsCreatedEvent()
        {
            // Arrange
            var eventEntity = new Event
            {
                Name = _testPrefix + "_Test Event",
                Description = "Test Description",
                CreatedBy = "testuser",
                EventDate = DateTime.UtcNow.AddDays(30)
            };

            // Act
            var result = await _eventRepository.CreateAsync(eventEntity);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testPrefix + "_Test Event", result.Name);
            Assert.Equal("Test Description", result.Description);
            Assert.Equal("testuser", result.CreatedBy);
            Assert.Equal("", result.PartitionKey);
            Assert.Equal(_testPrefix + "_Test Event", result.RowKey);
            Assert.NotEqual(default(ETag), result.ETag); // Check ETag is not default
            Assert.NotNull(result.Timestamp);

            // Verify it's actually in the database
            var retrieved = await _eventRepository.GetAsync(_testPrefix + "_Test Event");
            Assert.NotNull(retrieved);
            Assert.Equal(eventEntity.Name, retrieved.Name);
        }

        [Fact]
        public async Task CreateEventAsync_DuplicateEvent_ThrowsValidationException()
        {
            // Arrange
            var eventEntity = new Event
            {
                Name = _testPrefix + "_Duplicate Event",
                Description = "First Description",
                CreatedBy = "user1"
            };

            // Act & Assert - First creation should succeed
            var firstResult = await _eventRepository.CreateAsync(eventEntity);
            Assert.NotNull(firstResult);

            // Second creation should throw exception
            var duplicateEntity = new Event
            {
                Name = _testPrefix + "_Duplicate Event",
                Description = "Second Description",
                CreatedBy = "user2"
            };

            await Assert.ThrowsAsync<ValidationException>(
                async () => await _eventRepository.CreateAsync(duplicateEntity));
        }

        [Fact]
        public async Task GetEventAsync_ExistingEvent_ReturnsEvent()
        {
            // Arrange
            var eventEntity = new Event
            {
                Name = _testPrefix + "_Get Test Event",
                Description = "Get Test Description",
                CreatedBy = "testuser"
            };

            await _eventRepository.CreateAsync(eventEntity);

            // Act
            var result = await _eventRepository.GetAsync(_testPrefix + "_Get Test Event");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testPrefix + "_Get Test Event", result.Name);
            Assert.Equal("Get Test Description", result.Description);
            Assert.Equal("testuser", result.CreatedBy);
        }

        [Fact]
        public async Task GetEventAsync_NonExistingEvent_ReturnsNull()
        {
            // Act
            var result = await _eventRepository.GetAsync(_testPrefix + "_NonExisting Event");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateEventAsync_ExistingEvent_ReturnsUpdatedEvent()
        {
            // Arrange
            var originalEvent = new Event
            {
                Name = _testPrefix + "_Update Test Event",
                Description = "Original Description",
                CreatedBy = "testuser"
            };

            var createdEvent = await _eventRepository.CreateAsync(originalEvent);

            var updatedEvent = new Event
            {
                Name = _testPrefix + "_Update Test Event",
                Description = "Updated Description",
                CreatedBy = "updateduser",
                EventDate = DateTime.UtcNow.AddDays(60)
            };

            // Act
            var result = await _eventRepository.UpdateAsync(_testPrefix + "_Update Test Event", updatedEvent);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Description", result.Description);
            Assert.Equal("updateduser", result.CreatedBy);
            Assert.NotNull(result.EventDate);

            // Verify update in database
            var retrieved = await _eventRepository.GetAsync(_testPrefix + "_Update Test Event");
            Assert.NotNull(retrieved);
            Assert.Equal("Updated Description", retrieved.Description);
        }

        [Fact]
        public async Task UpdateEventAsync_NonExistingEvent_ThrowsNotFoundException()
        {
            // Arrange
            var eventEntity = new Event
            {
                Name = _testPrefix + "_Non Existent Event",
                Description = "This doesn't exist"
            };

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                async () => await _eventRepository.UpdateAsync(_testPrefix + "_Non Existent Event", eventEntity));
        }

        [Fact]
        public async Task DeleteEventAsync_ExistingEvent_ReturnsTrue()
        {
            // Arrange
            var eventEntity = new Event
            {
                Name = _testPrefix + "_Delete Test Event",
                Description = "Will be deleted",
                CreatedBy = "testuser"
            };

            await _eventRepository.CreateAsync(eventEntity);

            // Act
            var result = await _eventRepository.DeleteAsync(_testPrefix + "_Delete Test Event");

            // Assert
            Assert.True(result);

            // Verify deletion
            var retrieved = await _eventRepository.GetAsync(_testPrefix + "_Delete Test Event");
            Assert.Null(retrieved);
        }

        [Fact]
        public async Task DeleteEventAsync_NonExistingEvent_ReturnsFalse()
        {
            // Act
            var result = await _eventRepository.DeleteAsync(_testPrefix + "_Non Existent Event");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetAllAsync_MultipleEvents_ReturnsAllEvents()
        {
            // Arrange
            var events = new List<Event>
            {
                new Event { Name = _testPrefix + "_Event 1", Description = "Description 1", CreatedBy = "user1" },
                new Event { Name = _testPrefix + "_Event 2", Description = "Description 2", CreatedBy = "user2" },
                new Event { Name = _testPrefix + "_Event 3", Description = "Description 3", CreatedBy = "user3" }
            };

            foreach (var evt in events)
            {
                await _eventRepository.CreateAsync(evt);
            }

            // Act
            var result = await _eventRepository.GetAllAsync();

            // Assert - Filter for only our test events to avoid conflicts
            var ourEvents = result.Where(e => e.Name.StartsWith(_testPrefix + "_")).ToList();
            Assert.Equal(3, ourEvents.Count);

            // Verify our specific events are present
            var ourEventNames = ourEvents.Select(e => e.Name).ToList();
            Assert.Contains(_testPrefix + "_Event 1", ourEventNames);
            Assert.Contains(_testPrefix + "_Event 2", ourEventNames);
            Assert.Contains(_testPrefix + "_Event 3", ourEventNames);
        }

        [Fact]
        public async Task ExistsAsync_WithExistingEvent_ReturnsTrue()
        {
            // Arrange
            var eventEntity = new Event
            {
                Name = _testPrefix + "_Exists Test Event",
                Description = "Test",
                CreatedBy = "testuser"
            };

            await _eventRepository.CreateAsync(eventEntity);

            // Act
            var result = await _eventRepository.ExistsAsync("", _testPrefix + "_Exists Test Event");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExistsAsync_WithNonExistingEvent_ReturnsFalse()
        {
            // Act
            var result = await _eventRepository.ExistsAsync("", _testPrefix + "_Non Existent Event");

            // Assert
            Assert.False(result);
        }
    }
}
