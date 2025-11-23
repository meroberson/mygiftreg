using Azure;
using Azure.Data.Tables;
using MyGiftReg.Backend.Interfaces;
using MyGiftReg.Backend.Models;
using MyGiftReg.Backend.Models.DTOs;
using MyGiftReg.Backend.Services;
using MyGiftReg.Backend.Storage;
using MyGiftReg.Backend.Exceptions;
using Xunit.Abstractions;

namespace MyGiftReg.Tests.Integration
{
    public class EventServiceIntegrationTests : AzuriteTestBase
    {
        private EventService _eventService = null!;
        private EventRepository _eventRepository = null!;
        private AzureTableConfig _tableConfig = null!;

        public EventServiceIntegrationTests(ITestOutputHelper output) : base()
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            
            _eventRepository = new EventRepository(TableServiceClient);
            _eventService = new EventService(_eventRepository);
            _tableConfig = ServiceProvider.GetRequiredService<AzureTableConfig>();
        }

        [Fact]
        public async Task CreateEventAsync_ValidRequest_ReturnsCreatedEvent()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                Name = _testPrefix + "_Service Test Event",
                Description = "Service Test Description",
                EventDate = DateTime.Today.AddDays(60)
            };
            var userId = "serviceuser";

            // Act
            var result = await _eventService.CreateEventAsync(request, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testPrefix + "_Service Test Event", result.Name);
            Assert.Equal("Service Test Description", result.Description);
            Assert.Equal(userId, result.CreatedBy);
            Assert.NotNull(result.EventDate);

            // Verify it's persisted
            var retrieved = await _eventService.GetEventAsync(_testPrefix + "_Service Test Event");
            Assert.NotNull(retrieved);
            Assert.Equal(_testPrefix + "_Service Test Event", retrieved.Name);
        }

        [Fact]
        public async Task CreateEventAsync_DuplicateEvent_ThrowsValidationException()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                Name = _testPrefix + "_Duplicate Service Event",
                Description = "First Description"
            };
            var userId = "user1";

            // Act & Assert - First creation should succeed
            var firstResult = await _eventService.CreateEventAsync(request, userId);
            Assert.NotNull(firstResult);

            // Second creation should throw exception
            var duplicateRequest = new CreateEventRequest
            {
                Name = _testPrefix + "_Duplicate Service Event",
                Description = "Second Description"
            };
            var userId2 = "user2";

            await Assert.ThrowsAsync<ValidationException>(
                async () => await _eventService.CreateEventAsync(duplicateRequest, userId2));
        }

        [Fact]
        public async Task CreateEventAsync_EmptyUserId_ThrowsValidationException()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                Name = _testPrefix + "_Empty User Test Event",
                Description = "Test"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _eventService.CreateEventAsync(request, ""));
        }

        [Fact]
        public async Task CreateEventAsync_NullUserId_ThrowsValidationException()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                Name = _testPrefix + "_Null User Test Event",
                Description = "Test"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _eventService.CreateEventAsync(request, null!));
        }

        [Fact]
        public async Task CreateEventAsync_EmptyEventName_ThrowsValidationException()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                Name = "",
                Description = "Test Description"
            };
            var userId = "testuser";

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _eventService.CreateEventAsync(request, userId));
        }

        [Fact]
        public async Task GetEventAsync_ValidEventName_ReturnsEvent()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                Name = _testPrefix + "_Get Service Test Event",
                Description = "Get Service Test Description"
            };
            var userId = "testuser";

            await _eventService.CreateEventAsync(request, userId);

            // Act
            var result = await _eventService.GetEventAsync(_testPrefix + "_Get Service Test Event");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testPrefix + "_Get Service Test Event", result.Name);
            Assert.Equal("Get Service Test Description", result.Description);
            Assert.Equal(userId, result.CreatedBy);
        }

        [Fact]
        public async Task GetEventAsync_EmptyEventName_ThrowsValidationException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _eventService.GetEventAsync(""));
        }

        [Fact]
        public async Task GetEventAsync_NonExistingEvent_ReturnsNull()
        {
            // Act
            var result = await _eventService.GetEventAsync(_testPrefix + "_Non Existing Service Event");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateEventAsync_ExistingEvent_ReturnsUpdatedEvent()
        {
            // Arrange
            var createRequest = new CreateEventRequest
            {
                Name = _testPrefix + "_Update Service Test Event",
                Description = "Original Service Description"
            };
            var userId = "originaluser";
            await _eventService.CreateEventAsync(createRequest, userId);

            var updateRequest = new CreateEventRequest
            {
                Name = _testPrefix + "_Update Service Test Event",
                Description = "Updated Service Description",
                EventDate = DateTime.Today.AddDays(90)
            };
            var updateUserId = "updateuser";

            // Act
            var result = await _eventService.UpdateEventAsync(_testPrefix + "_Update Service Test Event", updateRequest, updateUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Service Description", result.Description);
            Assert.NotNull(result.EventDate);

            // Verify update persisted
            var retrieved = await _eventService.GetEventAsync(_testPrefix + "_Update Service Test Event");
            Assert.NotNull(retrieved);
            Assert.Equal("Updated Service Description", retrieved.Description);
            Assert.Equal(userId, retrieved.CreatedBy); // CreatedBy should remain original
        }

        [Fact]
        public async Task UpdateEventAsync_NonExistingEvent_ThrowsNotFoundException()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                Name = _testPrefix + "_Non Existent Service Event",
                Description = "This doesn't exist"
            };
            var userId = "testuser";

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                async () => await _eventService.UpdateEventAsync(_testPrefix + "_Non Existent Service Event", request, userId));
        }

        [Fact]
        public async Task UpdateEventAsync_EmptyEventName_ThrowsValidationException()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                Name = _testPrefix + "_Test Event",
                Description = "Test"
            };
            var userId = "testuser";

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _eventService.UpdateEventAsync("", request, userId));
        }

        [Fact]
        public async Task UpdateEventAsync_EmptyUserId_ThrowsValidationException()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                Name = _testPrefix + "_Test Event",
                Description = "Test"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _eventService.UpdateEventAsync(_testPrefix + "_Test Event", request, ""));
        }

        [Fact]
        public async Task DeleteEventAsync_ExistingEvent_ReturnsTrue()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                Name = _testPrefix + "_Delete Service Test Event",
                Description = "Will be deleted"
            };
            var userId = "testuser";
            await _eventService.CreateEventAsync(request, userId);

            // Act
            var result = await _eventService.DeleteEventAsync(_testPrefix + "_Delete Service Test Event", userId);

            // Assert
            Assert.True(result);

            // Verify deletion
            var retrieved = await _eventService.GetEventAsync(_testPrefix + "_Delete Service Test Event");
            Assert.Null(retrieved);
        }

        [Fact]
        public async Task DeleteEventAsync_NonExistingEvent_ReturnsFalse()
        {
            // Act
            var result = await _eventService.DeleteEventAsync(_testPrefix + "_Non Existent Service Event", "testuser");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteEventAsync_EmptyEventName_ThrowsValidationException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _eventService.DeleteEventAsync("", "testuser"));
        }

        [Fact]
        public async Task DeleteEventAsync_EmptyUserId_ThrowsValidationException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                async () => await _eventService.DeleteEventAsync(_testPrefix + "_Test Event", ""));
        }

        [Fact]
        public async Task GetAllEventsAsync_MultipleEvents_ReturnsAllEvents()
        {
            // Arrange
            var requests = new List<(CreateEventRequest request, string userId)>
            {
                (new CreateEventRequest { Name = _testPrefix + "_Service Event 1", Description = "Description 1" }, "user1"),
                (new CreateEventRequest { Name = _testPrefix + "_Service Event 2", Description = "Description 2" }, "user2"),
                (new CreateEventRequest { Name = _testPrefix + "_Service Event 3", Description = "Description 3" }, "user3")
            };

            foreach (var (request, userId) in requests)
            {
                await _eventService.CreateEventAsync(request, userId);
            }

            // Act
            var result = await _eventService.GetAllEventsAsync();

            // Assert - Filter for only our test events to avoid conflicts
            var ourEvents = result.Where(e => e.Name.StartsWith(_testPrefix + "_")).ToList();
            Assert.Equal(3, ourEvents.Count);

            // Verify our specific events are present
            var ourEventNames = ourEvents.Select(e => e.Name).ToList();
            Assert.Contains(_testPrefix + "_Service Event 1", ourEventNames);
            Assert.Contains(_testPrefix + "_Service Event 2", ourEventNames);
            Assert.Contains(_testPrefix + "_Service Event 3", ourEventNames);
        }

        [Fact]
        public async Task GetAllEventsAsync_NoEvents_ReturnsEmptyList()
        {
            // Act
            var result = await _eventService.GetAllEventsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Where(e => e.Name.StartsWith(_testPrefix + "_"))); // Only check our events
        }

        [Fact]
        public async Task CreateEventWithNullEventDate_Succeeds()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                Name = _testPrefix + "_Null Date Event",
                Description = "Event with null date",
                EventDate = null
            };
            var userId = "testuser";

            // Act
            var result = await _eventService.CreateEventAsync(request, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.EventDate);
            Assert.Equal(_testPrefix + "_Null Date Event", result.Name);
        }

        [Fact]
        public async Task ServiceAndRepository_ConsistencyTest()
        {
            // Arrange - Create via service
            var serviceRequest = new CreateEventRequest
            {
                Name = _testPrefix + "_Consistency Test Event",
                Description = "Testing service-repository consistency"
            };
            var userId = "testuser";
            var serviceResult = await _eventService.CreateEventAsync(serviceRequest, userId);

            // Act - Retrieve via repository
            var repoResult = await _eventRepository.GetAsync(_testPrefix + "_Consistency Test Event");

            // Assert
            Assert.NotNull(serviceResult);
            Assert.NotNull(repoResult);
            Assert.Equal(serviceResult.Name, repoResult.Name);
            Assert.Equal(serviceResult.Description, repoResult.Description);
            Assert.Equal(serviceResult.CreatedBy, repoResult.CreatedBy);
            Assert.Equal(serviceResult.PartitionKey, repoResult.PartitionKey);
            Assert.Equal(serviceResult.RowKey, repoResult.RowKey);
        }
    }
}
