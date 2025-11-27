using Moq;
using MyGiftReg.Backend.Interfaces;
using MyGiftReg.Backend.Models;
using MyGiftReg.Backend.Models.DTOs;
using MyGiftReg.Backend.Services;
using MyGiftReg.Backend.Exceptions;

namespace MyGiftReg.Tests.Services
{
    public class EventServiceTests
    {
        private readonly Mock<IEventRepository> _mockEventRepository;
        private readonly EventService _eventService;

        public EventServiceTests()
        {
            _mockEventRepository = new Mock<IEventRepository>();
            _eventService = new EventService(_mockEventRepository.Object);
        }

        [Fact]
        public async Task CreateEventAsync_ValidRequest_ReturnsCreatedEvent()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                Name = "Test Event",
                Description = "Test Description",
                EventDate = DateTime.Today
            };
            var userId = "testuser";
            var userDisplayName = "Test User";
            
            var expectedEvent = new Event
            {
                Name = "Test Event",
                Description = "Test Description",
                EventDate = DateTime.Today,
                CreatedBy = userId,
                CreatedByDisplayName = userDisplayName,
                CreatedDate = DateTime.UtcNow
            };

            _mockEventRepository.Setup(repo => repo.GetAsync(request.Name))
                .ReturnsAsync((Event?)null);
            _mockEventRepository.Setup(repo => repo.CreateAsync(It.IsAny<Event>()))
                .ReturnsAsync(expectedEvent);

            // Act
            var result = await _eventService.CreateEventAsync(request, userId, userDisplayName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Name, result.Name);
            Assert.Equal(request.Description, result.Description);
            Assert.Equal(userId, result.CreatedBy);
            Assert.Equal(userDisplayName, result.CreatedByDisplayName);
            
            _mockEventRepository.Verify(repo => repo.GetAsync(request.Name), Times.Once);
            _mockEventRepository.Verify(repo => repo.CreateAsync(It.IsAny<Event>()), Times.Once);
        }

        [Fact]
        public async Task CreateEventAsync_DuplicateEvent_ThrowsValidationException()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                Name = "Existing Event",
                Description = "Test Description"
            };
            var userId = "testuser";
            var userDisplayName = "Test User";
            
            var existingEvent = new Event { Name = "Existing Event" };
            _mockEventRepository.Setup(repo => repo.GetAsync(request.Name))
                .ReturnsAsync(existingEvent);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _eventService.CreateEventAsync(request, userId, userDisplayName));
            
            Assert.Contains("already exists", exception.Message);
        }

        [Fact]
        public async Task CreateEventAsync_EmptyUserId_ThrowsValidationException()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                Name = "Test Event",
                Description = "Test Description"
            };
            var userId = "";
            var userDisplayName = "Test User";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _eventService.CreateEventAsync(request, userId, userDisplayName));
            
            Assert.Contains("User ID cannot be null or empty", exception.Message);
        }

        // New Azure Storage validation tests
        [Theory]
        [InlineData("Valid Event Name")]
        [InlineData("Event123")]
        [InlineData("Event-With-Hyphens")]
        [InlineData("Event_With_Underscores")]
        [InlineData("Event With Spaces")]
        [InlineData("Event1234567890123456789012345678901234567890123456789012345678901234567890")] // 100 chars
        public async Task CreateEventAsync_ValidAzureStorageName_Succeeds(string validEventName)
        {
            // Arrange
            var request = new CreateEventRequest
            {
                Name = validEventName,
                Description = "Test Description"
            };
            var userId = "testuser";
            var userDisplayName = "Test User";

            var expectedEvent = new Event
            {
                Name = validEventName,
                Description = "Test Description",
                CreatedBy = userId,
                CreatedByDisplayName = userDisplayName,
                CreatedDate = DateTime.UtcNow
            };

            _mockEventRepository.Setup(repo => repo.GetAsync(request.Name))
                .ReturnsAsync((Event?)null);
            _mockEventRepository.Setup(repo => repo.CreateAsync(It.IsAny<Event>()))
                .ReturnsAsync(expectedEvent);

            // Act
            var result = await _eventService.CreateEventAsync(request, userId, userDisplayName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(validEventName, result.Name);
        }

        [Theory]
        [InlineData("Event/With/Slashes")]
        [InlineData("Event\\With\\Backslashes")]
        [InlineData("Event#With#Hash")]
        [InlineData("Event?With?Question")]
        public async Task CreateEventAsync_EventNameWithForbiddenCharacter_ThrowsValidationException(string invalidEventName)
        {
            // Arrange
            var request = new CreateEventRequest
            {
                Name = invalidEventName,
                Description = "Test Description"
            };
            var userId = "testuser";
            var userDisplayName = "Test User";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _eventService.CreateEventAsync(request, userId, userDisplayName));
            
            Assert.Contains("cannot contain the character", exception.Message);
            Assert.Contains("not allowed in Azure Table Storage PartitionKey and RowKey", exception.Message);
        }

        [Fact]
        public async Task CreateEventAsync_EventNameTooLongForDataAnnotations_ThrowsValidationException()
        {
            // Arrange
            var longEventName = new string('A', 101); // 101 characters (exceeds 100 char DataAnnotations limit)
            var request = new CreateEventRequest
            {
                Name = longEventName,
                Description = "Test Description"
            };
            var userId = "testuser";
            var userDisplayName = "Test User";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _eventService.CreateEventAsync(request, userId, userDisplayName));
            
            Assert.Contains("Event name cannot exceed 100 characters", exception.Message);
        }

        [Fact]
        public async Task CreateEventAsync_EventNameWithControlCharacter_ThrowsValidationException()
        {
            // Arrange
            var eventNameWithControlChar = "Event\x01With\x1FControl"; // Contains U+0001 and U+001F
            var request = new CreateEventRequest
            {
                Name = eventNameWithControlChar,
                Description = "Test Description"
            };
            var userId = "testuser";
            var userDisplayName = "Test User";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _eventService.CreateEventAsync(request, userId, userDisplayName));
            
            Assert.Contains("cannot contain control character", exception.Message);
            Assert.Contains("not allowed in Azure Table Storage PartitionKey and RowKey", exception.Message);
        }

        [Theory]
        [InlineData(" EventWithLeadingSpace")]
        [InlineData("EventWithTrailingSpace ")]
        public async Task CreateEventAsync_EventNameWithLeadingOrTrailingSpace_ThrowsValidationException(string invalidEventName)
        {
            // Arrange
            var request = new CreateEventRequest
            {
                Name = invalidEventName,
                Description = "Test Description"
            };
            var userId = "testuser";
            var userDisplayName = "Test User";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _eventService.CreateEventAsync(request, userId, userDisplayName));
            
            Assert.Contains("cannot start or end with whitespace characters", exception.Message);
        }

        [Fact]
        public async Task GetEventAsync_ValidEventName_ReturnsEvent()
        {
            // Arrange
            var eventName = "Test Event";
            var expectedEvent = new Event 
            { 
                Name = eventName,
                Description = "Test Description"
            };

            _mockEventRepository.Setup(repo => repo.GetAsync(eventName))
                .ReturnsAsync(expectedEvent);

            // Act
            var result = await _eventService.GetEventAsync(eventName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(eventName, result.Name);
            
            _mockEventRepository.Verify(repo => repo.GetAsync(eventName), Times.Once);
        }

        [Fact]
        public async Task GetEventAsync_EmptyEventName_ThrowsValidationException()
        {
            // Arrange
            var eventName = "";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _eventService.GetEventAsync(eventName));
            
            Assert.Contains("Event name cannot be null or empty", exception.Message);
        }

        [Theory]
        [InlineData("Event/With/Slashes")]
        [InlineData("Event\\With\\Backslashes")]
        public async Task GetEventAsync_EventNameWithForbiddenCharacter_ThrowsValidationException(string invalidEventName)
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _eventService.GetEventAsync(invalidEventName));
            
            Assert.Contains("cannot contain the character", exception.Message);
            Assert.Contains("not allowed in Azure Table Storage PartitionKey and RowKey", exception.Message);
        }

        [Fact]
        public async Task DeleteEventAsync_ExistingEvent_ReturnsTrue()
        {
            // Arrange
            var eventName = "Test Event";
            var userId = "testuser";
            
            _mockEventRepository.Setup(repo => repo.DeleteAsync(eventName))
                .ReturnsAsync(true);

            // Act
            var result = await _eventService.DeleteEventAsync(eventName, userId);

            // Assert
            Assert.True(result);
            
            _mockEventRepository.Verify(repo => repo.DeleteAsync(eventName), Times.Once);
        }

        [Fact]
        public async Task DeleteEventAsync_EventNameWithForbiddenCharacter_ThrowsValidationException()
        {
            // Arrange
            var eventName = "Event#With#Hash";
            var userId = "testuser";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _eventService.DeleteEventAsync(eventName, userId));
            
            Assert.Contains("cannot contain the character '#'", exception.Message);
        }

        [Fact]
        public async Task UpdateEventAsync_ValidRequest_UpdatesEvent()
        {
            // Arrange
            var eventName = "Original Event";
            var request = new CreateEventRequest
            {
                Name = "Updated Event",
                Description = "Updated Description"
            };
            var userId = "testuser";
            var userDisplayName = "Test User";
            
            var existingEvent = new Event 
            { 
                Name = eventName,
                Description = "Original Description",
                CreatedBy = userId,
                CreatedByDisplayName = userDisplayName,
                CreatedDate = DateTime.UtcNow
            };
            
            var updatedEvent = new Event
            {
                Name = request.Name,
                Description = request.Description,
                CreatedBy = userId,
                CreatedByDisplayName = userDisplayName,
                CreatedDate = existingEvent.CreatedDate
            };

            _mockEventRepository.Setup(repo => repo.GetAsync(eventName))
                .ReturnsAsync(existingEvent);
            _mockEventRepository.Setup(repo => repo.UpdateAsync(eventName, It.IsAny<Event>()))
                .ReturnsAsync(updatedEvent);

            // Act
            var result = await _eventService.UpdateEventAsync(eventName, request, userId, userDisplayName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Name, result.Name);
            Assert.Equal(request.Description, result.Description);
            
            _mockEventRepository.Verify(repo => repo.GetAsync(eventName), Times.Once);
            _mockEventRepository.Verify(repo => repo.UpdateAsync(eventName, It.IsAny<Event>()), Times.Once);
        }

        [Fact]
        public async Task UpdateEventAsync_NewEventNameWithForbiddenCharacter_ThrowsValidationException()
        {
            // Arrange
            var eventName = "Original Event";
            var request = new CreateEventRequest
            {
                Name = "Event/With/Slashes",
                Description = "Updated Description"
            };
            var userId = "testuser";
            var userDisplayName = "Test User";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _eventService.UpdateEventAsync(eventName, request, userId, userDisplayName));
            
            Assert.Contains("cannot contain the character", exception.Message);
        }

        [Fact]
        public async Task GetAllEventsAsync_ReturnsAllEvents()
        {
            // Arrange
            var events = new List<Event>
            {
                new Event { Name = "Event1", Description = "Description1" },
                new Event { Name = "Event2", Description = "Description2" }
            };

            _mockEventRepository.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(events);

            // Act
            var result = await _eventService.GetAllEventsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            
            _mockEventRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateEventAsync_PopulatesCreatedByDisplayName()
        {
            // Arrange
            var request = new CreateEventRequest { Name = "Test Event", Description = "Test" };
            var userId = "testuser";
            var userDisplayName = "John Smith";
            
            var expectedEvent = new Event
            {
                Name = "Test Event",
                Description = "Test",
                CreatedBy = userId,
                CreatedByDisplayName = userDisplayName,
                CreatedDate = DateTime.UtcNow
            };

            _mockEventRepository.Setup(repo => repo.GetAsync(request.Name))
                .ReturnsAsync((Event?)null);
            _mockEventRepository.Setup(repo => repo.CreateAsync(It.IsAny<Event>()))
                .ReturnsAsync(expectedEvent);

            // Act
            var result = await _eventService.CreateEventAsync(request, userId, userDisplayName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userDisplayName, result.CreatedByDisplayName);
        }

        [Fact]
        public async Task UpdateEventAsync_PreservesOriginalCreatedByDisplayName()
        {
            // Arrange
            var eventName = "Test Event";
            var originalUserId = "originaluser";
            var originalDisplayName = "Original User";
            var request = new CreateEventRequest { Name = "Test Event", Description = "Updated" };
            
            var existingEvent = new Event 
            { 
                Name = eventName,
                Description = "Original Description",
                CreatedBy = originalUserId,
                CreatedByDisplayName = originalDisplayName,
                CreatedDate = DateTime.UtcNow
            };
            
            var updatedEvent = new Event
            {
                Name = request.Name,
                Description = request.Description,
                CreatedBy = originalUserId,
                CreatedByDisplayName = originalDisplayName,
                CreatedDate = existingEvent.CreatedDate
            };

            _mockEventRepository.Setup(repo => repo.GetAsync(eventName))
                .ReturnsAsync(existingEvent);
            _mockEventRepository.Setup(repo => repo.UpdateAsync(eventName, It.IsAny<Event>()))
                .ReturnsAsync(updatedEvent);

            // Act
            var result = await _eventService.UpdateEventAsync(eventName, request, "updateuser", "Update User");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(originalDisplayName, result.CreatedByDisplayName);
            Assert.Equal(originalUserId, result.CreatedBy); // CreatedBy should remain original
        }
    }
}
