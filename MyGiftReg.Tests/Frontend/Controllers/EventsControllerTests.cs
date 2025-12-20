using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Moq;
using MyGiftReg.Frontend.Controllers;
using MyGiftReg.Frontend.Services;
using MyGiftReg.Frontend.Authorization;
using MyGiftReg.Backend.Interfaces;
using MyGiftReg.Backend.Models;
using MyGiftReg.Backend.Models.DTOs;
using System.Security.Claims;
using System.Linq;
using MyGiftReg.Frontend.Models;

namespace MyGiftReg.Tests.Frontend.Controllers
{
    public class EventsControllerTests
    {
        private readonly Mock<IEventService> _mockEventService;
        private readonly Mock<IGiftListService> _mockGiftListService;
        private readonly Mock<IGiftItemService> _mockGiftItemService;
        private readonly Mock<IAzureUserService> _mockAzureUserService;
        private readonly Mock<ILogger<EventsController>> _mockLogger;
        private readonly EventsController _controller;
        private readonly DefaultHttpContext _httpContext;

        public EventsControllerTests()
        {
            _mockEventService = new Mock<IEventService>();
            _mockGiftListService = new Mock<IGiftListService>();
            _mockGiftItemService = new Mock<IGiftItemService>();
            _mockAzureUserService = new Mock<IAzureUserService>();
            _mockLogger = new Mock<ILogger<EventsController>>();
            
            _controller = new EventsController(
                _mockEventService.Object,
                _mockGiftListService.Object,
                _mockGiftItemService.Object,
                _mockAzureUserService.Object,
                _mockLogger.Object);

            _httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };
        }

        [Fact]
        public async Task Index_Get_DefaultAction_ReturnsEventsList()
        {
            // Arrange
            var events = new List<Event>
            {
                new Event { Name = "Birthday", Description = "John's birthday", EventDate = DateTime.Now },
                new Event { Name = "Christmas", Description = "Christmas celebration", EventDate = DateTime.Now }
            };
            
            _mockEventService.Setup(s => s.GetAllEventsAsync()).ReturnsAsync(events);

            // Act
            var result = await _controller.Index(null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);
            Assert.Equal(events, viewResult.Model);
        }

        [Fact]
        public async Task Index_Get_CreateView_ReturnsCreateView()
        {
            // Act
            var result = await _controller.Index("create", null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Create", viewResult.ViewName);
            Assert.IsType<CreateEventRequest>(viewResult.Model);
        }

        [Fact]
        public async Task Details_Get_WithValidEventName_ReturnsEventDetails()
        {
            // Arrange
            var eventName = "Birthday";
            var eventEntity = new Event
            {
                Name = eventName,
                Description = "John's birthday",
                EventDate = DateTime.Now
            };

            var myGiftLists = new List<GiftList>
            {
                new GiftList { Id = Guid.NewGuid(), Name = "My List", Owner = "user123" }
            };

            var othersGiftLists = new List<GiftList>
            {
                new GiftList { Id = Guid.NewGuid(), Name = "Other List", Owner = "user456" }
            };

            _mockEventService.Setup(s => s.GetEventAsync(eventName)).ReturnsAsync(eventEntity);
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");
            _mockGiftListService.Setup(s => s.GetGiftListsByEventAndUserAsync(eventName, "user123")).ReturnsAsync(myGiftLists);
            _mockGiftListService.Setup(s => s.GetGiftListsByEventForOthersAsync(eventName, "user123")).ReturnsAsync(othersGiftLists);

            // Act
            var result = await _controller.Details(eventName);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(eventEntity, viewResult.Model);
            Assert.Equal(eventName, _controller.ViewBag.EventName);
            Assert.Equal(myGiftLists, _controller.ViewBag.MyGiftLists);
            Assert.Equal(othersGiftLists, _controller.ViewBag.OthersGiftLists);
        }

        [Fact]
        public async Task Index_Get_EditView_WithValidEventName_ReturnsEditView()
        {
            // Arrange
            var eventName = "Birthday";
            var eventEntity = new Event
            {
                Name = eventName,
                Description = "John's birthday",
                EventDate = DateTime.Now
            };

            _mockEventService.Setup(s => s.GetEventAsync(eventName)).ReturnsAsync(eventEntity);

            // Act
            var result = await _controller.Index("edit", eventName);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Edit", viewResult.ViewName);
            var editRequest = Assert.IsType<CreateEventRequest>(viewResult.Model);
            Assert.Equal(eventName, editRequest.Name);
            Assert.Equal(eventEntity.Description, editRequest.Description);
            Assert.Equal(eventEntity.EventDate, editRequest.EventDate);
        }

        [Fact]
        public async Task Index_Get_EditView_WithMissingEventName_ReturnsNotFound()
        {
            // Arrange
            var eventName = "Birthday";
            _mockEventService.Setup(s => s.GetEventAsync(eventName)).ReturnsAsync((Event)null!);

            // Act
            var result = await _controller.Index("edit", eventName);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // POST Tests

        [Fact]
        public async Task Index_Post_Create_WithValidRequest_CreatesEventAndRedirects()
        {
            // Arrange
            var createRequest = new CreateEventRequest
            {
                Name = "Birthday",
                Description = "John's birthday",
                EventDate = DateTime.Now
            };

            var createdEvent = new Event
            {
                Name = createRequest.Name,
                Description = createRequest.Description,
                EventDate = createRequest.EventDate
            };

            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");
            _mockAzureUserService.Setup(s => s.GetCurrentUserDisplayName()).Returns("Test User");
            _mockEventService.Setup(s => s.CreateEventAsync(It.IsAny<CreateEventRequest>(), "user123", "Test User")).ReturnsAsync(createdEvent);

            // Act
            var result = await _controller.Index("create", null, createRequest);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal(createdEvent.Name, redirectResult.RouteValues!["eventName"]);
        }

        [Fact]
        public async Task Index_Post_Create_WithInvalidModelState_ReturnsCreateView()
        {
            // Arrange
            var createRequest = new CreateEventRequest
            {
                Name = "Birthday",
                Description = "John's birthday",
                EventDate = DateTime.Now
            };

            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await _controller.Index("create", null, createRequest);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Create", viewResult.ViewName);
            Assert.Equal(createRequest, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Index_Post_Create_WithValidationException_AddsModelError()
        {
            // Arrange
            var createRequest = new CreateEventRequest
            {
                Name = "Birthday",
                Description = "John's birthday",
                EventDate = DateTime.Now
            };

            var validationException = new MyGiftReg.Backend.Exceptions.ValidationException("Event name already exists");
            
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");
            _mockAzureUserService.Setup(s => s.GetCurrentUserDisplayName()).Returns("Test User");
            _mockEventService.Setup(s => s.CreateEventAsync(It.IsAny<CreateEventRequest>(), "user123", "Test User"))
                           .ThrowsAsync(validationException);

            // Act
            var result = await _controller.Index("create", null, createRequest);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Create", viewResult.ViewName);
            Assert.Equal(createRequest, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
            Assert.NotNull(_controller.ModelState[""]);
            Assert.Contains("Event name already exists", _controller.ModelState[""]!.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Index_Post_Edit_WithValidRequest_UpdatesEventAndRedirects()
        {
            // Arrange
            var eventName = "Birthday";
            var editRequest = new CreateEventRequest
            {
                Name = eventName,
                Description = "Updated description",
                EventDate = DateTime.Now
            };

            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");
            _mockAzureUserService.Setup(s => s.GetCurrentUserDisplayName()).Returns("Test User");

            // Act
            var result = await _controller.Index("edit", eventName, editRequest);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal(eventName, redirectResult.RouteValues!["eventName"]);
            
            _mockEventService.Verify(s => s.UpdateEventAsync(eventName, editRequest, "user123", "Test User"), Times.Once);
        }

        [Fact]
        public async Task Index_Post_Edit_WithNotFoundException_ReturnsNotFound()
        {
            // Arrange
            var eventName = "Birthday";
            var editRequest = new CreateEventRequest
            {
                Name = eventName,
                Description = "Updated description",
                EventDate = DateTime.Now
            };

            var notFoundException = new MyGiftReg.Backend.Exceptions.NotFoundException("Event not found");
            
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");
            _mockAzureUserService.Setup(s => s.GetCurrentUserDisplayName()).Returns("Test User");
            _mockEventService.Setup(s => s.UpdateEventAsync(eventName, editRequest, "user123", "Test User"))
                           .ThrowsAsync(notFoundException);

            // Act
            var result = await _controller.Index("edit", eventName, editRequest);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Index_Post_Edit_WithValidationError_RepresentsEditViewWithEvent()
        {
            // Arrange
            var eventName = "Birthday";
            var editRequest = new CreateEventRequest
            {
                Name = eventName,
                Description = "Updated description",
                EventDate = DateTime.Now
            };

            var originalEvent = new Event
            {
                Name = eventName,
                Description = "Original description",
                EventDate = DateTime.Now
            };

            var validationException = new MyGiftReg.Backend.Exceptions.ValidationException("Description too short");
            
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");
            _mockAzureUserService.Setup(s => s.GetCurrentUserDisplayName()).Returns("Test User");
            _mockEventService.Setup(s => s.UpdateEventAsync(eventName, editRequest, "user123", "Test User"))
                           .ThrowsAsync(validationException);
            _mockEventService.Setup(s => s.GetEventAsync(eventName)).ReturnsAsync(originalEvent);

            // Act
            var result = await _controller.Index("edit", eventName, editRequest);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Edit", viewResult.ViewName);
            Assert.Equal(editRequest, viewResult.Model);
            Assert.Equal(eventName, editRequest.Name); // Name should be preserved
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Index_Post_WithMissingAction_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                Name = "Birthday",
                Description = "John's birthday",
                EventDate = DateTime.Now
            };

            // Act
            var result = await _controller.Index(null, null, request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Action parameter is required", badRequest.Value);
        }

        [Fact]
        public async Task Index_Post_WithUnknownAction_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                Name = "Birthday",
                Description = "John's birthday",
                EventDate = DateTime.Now
            };

            // Act
            var result = await _controller.Index("unknown", null, request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Unknown action: unknown", badRequest.Value);
        }

        [Fact]
        public async Task Index_Post_Edit_WithMissingId_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                Name = "Birthday",
                Description = "John's birthday",
                EventDate = DateTime.Now
            };

            // Act
            var result = await _controller.Index("edit", null, request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Event name is required for edit action", badRequest.Value);
        }

        // Error Handling Tests

        [Fact]
        public async Task Index_Get_WithException_ReturnsEventsListWithErrorMessage()
        {
            // Arrange
            _mockEventService.Setup(s => s.GetAllEventsAsync()).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Index(null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Empty(viewResult.Model as List<Event> ?? new List<Event>());
            Assert.Equal("An error occurred while loading events. Please try again.", _controller.ViewBag.ErrorMessage);
        }

        [Fact]
        public async Task Details_Get_WithException_ReturnsRedirectToIndex()
        {
            // Arrange
            var eventName = "Birthday";
            _mockEventService.Setup(s => s.GetEventAsync(eventName)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Details(eventName);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("An error occurred while loading event details. Please try again.", _controller.ViewBag.ErrorMessage);
        }

        [Fact]
        public async Task Details_Get_WithEmptyEventName_ReturnsNotFound()
        {
            // Act
            var result = await _controller.Details(string.Empty);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Index_Get_WithExceptionOnEdit_ReturnsRedirectWithError()
        {
            // Arrange
            var eventName = "Birthday";
            _mockEventService.Setup(s => s.GetEventAsync(eventName)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Index("edit", eventName);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("An error occurred while loading the event for editing.", _controller.ViewBag.ErrorMessage);
        }

        [Fact]
        public async Task Index_Post_WithException_ReturnsAppropriateResponse()
        {
            // Arrange
            var request = new CreateEventRequest
            {
                Name = "Birthday",
                Description = "John's birthday",
                EventDate = DateTime.Now
            };

            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");
            _mockAzureUserService.Setup(s => s.GetCurrentUserDisplayName()).Returns("Test User");
            _mockEventService.Setup(s => s.CreateEventAsync(It.IsAny<CreateEventRequest>(), "user123", "Test User"))
                           .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Index("create", null, request);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Create", viewResult.ViewName);
            Assert.Equal(request, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
        }

        // Admin Role Tests

        [Fact]
        public async Task Index_Get_EditView_WithValidEventName_ForAdminUser_SetsIsAdminToTrue()
        {
            // Arrange
            var eventName = "Birthday";
            var eventEntity = new Event
            {
                Name = eventName,
                Description = "John's birthday",
                EventDate = DateTime.Now
            };

            // Set up admin role in claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "MyGiftReg.Access"),
                new Claim(ClaimTypes.Role, "MyGiftReg.Admin")
            };
            _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

            _mockEventService.Setup(s => s.GetEventAsync(eventName)).ReturnsAsync(eventEntity);

            // Act
            var result = await _controller.Index("edit", eventName);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Edit", viewResult.ViewName);
            Assert.True((bool)_controller.ViewBag.IsAdmin);
        }

        [Fact]
        public async Task Index_Get_EditView_WithValidEventName_ForNonAdminUser_SetsIsAdminToFalse()
        {
            // Arrange
            var eventName = "Birthday";
            var eventEntity = new Event
            {
                Name = eventName,
                Description = "John's birthday",
                EventDate = DateTime.Now
            };

            // Set up non-admin role in claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "MyGiftReg.Access")
                // No admin role
            };
            _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };

            _mockEventService.Setup(s => s.GetEventAsync(eventName)).ReturnsAsync(eventEntity);

            // Act
            var result = await _controller.Index("edit", eventName);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Edit", viewResult.ViewName);
            Assert.False((bool)_controller.ViewBag.IsAdmin);
        }

        [Fact]
        public async Task Delete_Post_WithValidEventName_AndAdminRole_DeletesEventAndRedirects()
        {
            // Arrange
            var eventName = "Birthday";
            
            // Set up admin role in claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "MyGiftReg.Access"),
                new Claim(ClaimTypes.Role, "MyGiftReg.Admin")
            };
            _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");

            // Act
            var result = await _controller.Delete(eventName);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            
            _mockEventService.Verify(s => s.DeleteEventAsync(eventName, "user123"), Times.Once);
        }

        [Fact]
        public async Task Delete_Post_WithMissingEventName_ReturnsBadRequest()
        {
            // Arrange
            // Set up admin role in claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "MyGiftReg.Access"),
                new Claim(ClaimTypes.Role, "MyGiftReg.Admin")
            };
            _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

            // Act
            var result = await _controller.Delete("");

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Event name is required", badRequest.Value);
        }

        [Fact]
        public async Task Delete_Post_WithException_ReturnsRedirectToIndex()
        {
            // Arrange
            var eventName = "Birthday";
            
            // Set up admin role in claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "MyGiftReg.Access"),
                new Claim(ClaimTypes.Role, "MyGiftReg.Admin")
            };
            _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");
            _mockEventService.Setup(s => s.DeleteEventAsync(eventName, "user123"))
                           .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Delete(eventName);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public async Task ShoppingList_Get_WithReservedItems_ReturnsShoppingListViewWithModel()
        {
            // Arrange
            var eventName = "ShoppingEvent";
            var eventEntity = new Event { Name = eventName };

            _mockEventService.Setup(s => s.GetEventAsync(eventName)).ReturnsAsync(eventEntity);
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");

            // Gift lists
            var list1 = new GiftList { Id = Guid.NewGuid(), Name = "List One" };
            var list2 = new GiftList { Id = Guid.NewGuid(), Name = "List Two" };
            _mockGiftListService.Setup(s => s.GetGiftListsByEventAsync(eventName)).ReturnsAsync(new List<GiftList> { list1, list2 });

            // Gift items reserved by user on both lists
            var item1 = new GiftItem
            {
                Id = Guid.NewGuid(),
                Name = "Item A",
                Description = "Desc A",
                Url = "https://example.com/a",
                Quantity = 3,
                GiftListId = list1.Id,
                Reservations = new List<Reservation> {
                    new Reservation { UserId = "user456", UserDisplayName = "Other User", Quantity = 1 },
                    new Reservation { UserId = "user123", UserDisplayName = "User", Quantity = 2 }
                }
            };

            var item2 = new GiftItem
            {
                Id = Guid.NewGuid(),
                Name = "Item B",
                Description = "Desc B",
                Url = "https://example.com/b",
                Quantity = 1,
                GiftListId = list2.Id,
                Reservations = new List<Reservation> { new Reservation { UserId = "user123", UserDisplayName = "User", Quantity = 1 } }
            };

            _mockGiftItemService.Setup(s => s.GetReservedItemsByEventAsync(eventName, "user123")).ReturnsAsync(new List<GiftItem> { item1, item2 });

            // Act
            var result = await _controller.ShoppingList(eventName);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("ShoppingList", viewResult.ViewName);

            var model = Assert.IsType<List<ShoppingListItemViewModel>>(viewResult.Model);
            Assert.Equal(2, model.Count);

            var modelItem1 = model.FirstOrDefault(m => m.ItemName == "Item A");
            var modelItem2 = model.FirstOrDefault(m => m.ItemName == "Item B");

            Assert.NotNull(modelItem1);
            Assert.Equal("List One", modelItem1.GiftListName);
            Assert.Equal(2, modelItem1.QuantityReserved);
            Assert.Equal(3, modelItem1.QuantityTotal);

            Assert.NotNull(modelItem2);
            Assert.Equal("List Two", modelItem2.GiftListName);
            Assert.Equal(1, modelItem2.QuantityReserved);
            Assert.Equal(1, modelItem2.QuantityTotal);
        }

        [Fact]
        public async Task ShoppingList_Get_WithNoReservedItems_ReturnsEmptyModel()
        {
            // Arrange
            var eventName = "NoReservesEvent";
            _mockEventService.Setup(s => s.GetEventAsync(eventName)).ReturnsAsync(new Event { Name = eventName });
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");
            _mockGiftListService.Setup(s => s.GetGiftListsByEventAsync(eventName)).ReturnsAsync(new List<GiftList>());
            _mockGiftItemService.Setup(s => s.GetReservedItemsByEventAsync(eventName, "user123")).ReturnsAsync(new List<GiftItem>());

            // Act
            var result = await _controller.ShoppingList(eventName);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("ShoppingList", viewResult.ViewName);

            var model = Assert.IsType<List<ShoppingListItemViewModel>>(viewResult.Model);
            Assert.Empty(model);
        }
    }
}
