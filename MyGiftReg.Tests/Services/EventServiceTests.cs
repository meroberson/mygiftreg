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
            
            var expectedEvent = new Event
            {
                Name = "Test Event",
                Description = "Test Description",
                EventDate = DateTime.Today,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow
            };

            _mockEventRepository.Setup(repo => repo.GetAsync(request.Name))
                .ReturnsAsync((Event?)null);
            _mockEventRepository.Setup(repo => repo.CreateAsync(It.IsAny<Event>()))
                .ReturnsAsync(expectedEvent);

            // Act
            var result = await _eventService.CreateEventAsync(request, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Name, result.Name);
            Assert.Equal(request.Description, result.Description);
            Assert.Equal(userId, result.CreatedBy);
            
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
            
            var existingEvent = new Event { Name = "Existing Event" };
            _mockEventRepository.Setup(repo => repo.GetAsync(request.Name))
                .ReturnsAsync(existingEvent);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _eventService.CreateEventAsync(request, userId));
            
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

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _eventService.CreateEventAsync(request, userId));
            
            Assert.Contains("User ID cannot be null or empty", exception.Message);
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
    }
}
