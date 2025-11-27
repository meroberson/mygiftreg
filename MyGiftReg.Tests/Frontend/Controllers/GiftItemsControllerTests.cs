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

namespace MyGiftReg.Tests.Frontend.Controllers
{
    public class GiftItemsControllerTests
    {
        private readonly Mock<IGiftItemService> _mockGiftItemService;
        private readonly Mock<IGiftListService> _mockGiftListService;
        private readonly Mock<IAzureUserService> _mockAzureUserService;
        private readonly Mock<ILogger<GiftItemsController>> _mockLogger;
        private readonly GiftItemsController _controller;
        private readonly DefaultHttpContext _httpContext;

        public GiftItemsControllerTests()
        {
            _mockGiftItemService = new Mock<IGiftItemService>();
            _mockGiftListService = new Mock<IGiftListService>();
            _mockAzureUserService = new Mock<IAzureUserService>();
            _mockLogger = new Mock<ILogger<GiftItemsController>>();
            
            _controller = new GiftItemsController(
                _mockGiftItemService.Object,
                _mockGiftListService.Object,
                _mockAzureUserService.Object,
                _mockLogger.Object);

            _httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };
        }

        [Fact]
        public async Task Index_Get_CreateView_ReturnsCreateView()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();
            var giftListEntity = new GiftList
            {
                Id = Guid.Parse(giftListId),
                Name = "My Gift List",
                EventName = eventName,
                Owner = "user123",
                OwnerDisplayName = "John Doe"
            };

            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId)).ReturnsAsync(giftListEntity);

            // Act
            var result = await _controller.Index(eventName, giftListId, "create", null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Create", viewResult.ViewName);
            var createRequest = Assert.IsType<CreateGiftItemRequest>(viewResult.Model);
            Assert.Equal(giftListId, createRequest.GiftListId);
            Assert.Equal(eventName, _controller.ViewBag.EventName);
            Assert.Equal(giftListId, _controller.ViewBag.GiftListId);
            Assert.Equal(giftListEntity.Name, _controller.ViewBag.GiftListName);
        }

        [Fact]
        public async Task Index_Get_EditView_WithValidGiftItemId_ReturnsEditView()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();
            var giftItemId = Guid.NewGuid().ToString();
            var giftListEntity = new GiftList
            {
                Id = Guid.Parse(giftListId),
                Name = "My Gift List",
                EventName = eventName,
                Owner = "user123",
                OwnerDisplayName = "John Doe"
            };

            var giftItemEntity = new GiftItem
            {
                Id = Guid.Parse(giftItemId),
                Name = "Gift Item",
                Description = "A great gift",
                Url = "https://example.com",
                GiftListId = Guid.Parse(giftListId)
            };

            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId)).ReturnsAsync(giftListEntity);
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");
            _mockGiftItemService.Setup(s => s.GetGiftItemAsync(eventName, giftListId, giftItemId, "user123")).ReturnsAsync(giftItemEntity);

            // Act
            var result = await _controller.Index(eventName, giftListId, "edit", giftItemId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Edit", viewResult.ViewName);
            Assert.Equal(eventName, _controller.ViewBag.EventName);
            Assert.Equal(giftListId, _controller.ViewBag.GiftListId);
            Assert.Equal(giftItemId, _controller.ViewBag.GiftItemId);
            Assert.Equal(giftListEntity.Name, _controller.ViewBag.GiftListName);
            Assert.Equal(giftItemEntity, viewResult.Model);
        }

        [Fact]
        public async Task Index_Get_WithMissingEventName_ReturnsNotFound()
        {
            // Arrange
            var giftListId = Guid.NewGuid().ToString();

            // Act
            var result = await _controller.Index(null!, giftListId, "create", null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Index_Get_WithMissingGiftListId_ReturnsNotFound()
        {
            // Arrange
            var eventName = "Birthday";

            // Act
            var result = await _controller.Index(eventName, null!, "create", null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Index_Get_WithNonExistentGiftList_ReturnsNotFound()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();

            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId)).ReturnsAsync((GiftList)null!);

            // Act
            var result = await _controller.Index(eventName, giftListId, "create", null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Index_Get_EditView_WithMissingGiftItemId_ReturnsNotFound()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();

            var giftListEntity = new GiftList
            {
                Id = Guid.Parse(giftListId),
                Name = "My Gift List",
                EventName = eventName,
                Owner = "user123",
                OwnerDisplayName = "John Doe"
            };

            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId)).ReturnsAsync(giftListEntity);

            // Act
            var result = await _controller.Index(eventName, giftListId, "edit", null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Index_Get_EditView_WithNonExistentGiftItem_ReturnsNotFound()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();
            var giftItemId = Guid.NewGuid().ToString();

            var giftListEntity = new GiftList
            {
                Id = Guid.Parse(giftListId),
                Name = "My Gift List",
                EventName = eventName,
                Owner = "user123",
                OwnerDisplayName = "John Doe"
            };

            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId)).ReturnsAsync(giftListEntity);
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");
            _mockGiftItemService.Setup(s => s.GetGiftItemAsync(eventName, giftListId, giftItemId, "user123")).ReturnsAsync((GiftItem)null!);

            // Act
            var result = await _controller.Index(eventName, giftListId, "edit", giftItemId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // POST Tests

        [Fact]
        public async Task Index_Post_Create_WithValidRequest_CreatesGiftItemAndRedirects()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();
            var createRequest = new CreateGiftItemRequest
            {
                Name = "Gift Item",
                Description = "A great gift",
                Url = "https://example.com",
                GiftListId = giftListId
            };

            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId)).ReturnsAsync(new GiftList { Id = Guid.Parse(giftListId) });
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");

            // Act
            var result = await _controller.Index(eventName, giftListId, "create", null, createRequest);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("GiftLists", redirectResult.ControllerName);
            Assert.Equal(eventName, redirectResult.RouteValues!["eventName"]);
            Assert.Equal(giftListId, redirectResult.RouteValues!["giftListId"]);
            
            _mockGiftItemService.Verify(s => s.CreateGiftItemAsync(eventName, createRequest, "user123"), Times.Once);
        }

        [Fact]
        public async Task Index_Post_Create_WithInvalidModelState_ReturnsCreateView()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();
            var createRequest = new CreateGiftItemRequest
            {
                Name = "Gift Item",
                Description = "A great gift",
                GiftListId = giftListId
            };

            _controller.ModelState.AddModelError("Name", "Name is required");
            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId)).ReturnsAsync(new GiftList { Id = Guid.Parse(giftListId) });

            // Act
            var result = await _controller.Index(eventName, giftListId, "create", null, createRequest);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Create", viewResult.ViewName);
            Assert.Equal(createRequest, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Equal(eventName, _controller.ViewBag.EventName);
            Assert.Equal(giftListId, _controller.ViewBag.GiftListId);
        }

        [Fact]
        public async Task Index_Post_Create_WithValidationException_AddsModelError()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();
            var createRequest = new CreateGiftItemRequest
            {
                Name = "Gift Item",
                Description = "A great gift",
                GiftListId = giftListId
            };

            var validationException = new MyGiftReg.Backend.Exceptions.ValidationException("Gift item name already exists");
            
            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId)).ReturnsAsync(new GiftList { Id = Guid.Parse(giftListId) });
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");
            _mockGiftItemService.Setup(s => s.CreateGiftItemAsync(eventName, createRequest, "user123"))
                               .ThrowsAsync(validationException);

            // Act
            var result = await _controller.Index(eventName, giftListId, "create", null, createRequest);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Create", viewResult.ViewName);
            Assert.Equal(createRequest, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
            Assert.NotNull(_controller.ModelState[""]);
            Assert.Contains("Gift item name already exists", _controller.ModelState[""]!.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Index_Post_Edit_WithValidRequest_UpdatesGiftItemAndRedirects()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();
            var giftItemId = Guid.NewGuid().ToString();
            var editRequest = new CreateGiftItemRequest
            {
                Name = "Updated Gift Item",
                Description = "Updated description",
                Url = "https://updated.com",
                GiftListId = giftListId
            };

            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId)).ReturnsAsync(new GiftList { Id = Guid.Parse(giftListId) });
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");

            // Act
            var result = await _controller.Index(eventName, giftListId, "edit", giftItemId, editRequest);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("GiftLists", redirectResult.ControllerName);
            Assert.Equal(eventName, redirectResult.RouteValues!["eventName"]);
            Assert.Equal(giftListId, redirectResult.RouteValues!["giftListId"]);
            
            _mockGiftItemService.Verify(s => s.UpdateGiftItemAsync(eventName, giftListId, giftItemId, editRequest, "user123"), Times.Once);
        }

        [Fact]
        public async Task Index_Post_Edit_WithNotFoundException_ReturnsNotFound()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();
            var giftItemId = Guid.NewGuid().ToString();
            var editRequest = new CreateGiftItemRequest
            {
                Name = "Updated Gift Item",
                Description = "Updated description",
                GiftListId = giftListId
            };

            var notFoundException = new MyGiftReg.Backend.Exceptions.NotFoundException("Gift item not found");
            
            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId)).ReturnsAsync(new GiftList { Id = Guid.Parse(giftListId) });
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");
            _mockGiftItemService.Setup(s => s.UpdateGiftItemAsync(eventName, giftListId, giftItemId, editRequest, "user123"))
                               .ThrowsAsync(notFoundException);

            // Act
            var result = await _controller.Index(eventName, giftListId, "edit", giftItemId, editRequest);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Index_Post_Delete_WithValidGiftItemId_DeletesGiftItemAndRedirects()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();
            var giftItemId = Guid.NewGuid().ToString();
            
            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId)).ReturnsAsync(new GiftList { Id = Guid.Parse(giftListId) });
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");

            // Act
            var result = await _controller.Index(eventName, giftListId, "delete", giftItemId, null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("GiftLists", redirectResult.ControllerName);
            Assert.Equal(eventName, redirectResult.RouteValues!["eventName"]);
            Assert.Equal(giftListId, redirectResult.RouteValues!["giftListId"]);
            
            _mockGiftItemService.Verify(s => s.DeleteGiftItemAsync(eventName, giftListId, giftItemId, "user123"), Times.Once);
        }

        [Fact]
        public async Task Index_Post_Reserve_WithValidGiftItemId_ReservesGiftItemAndRedirects()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();
            var giftItemId = Guid.NewGuid().ToString();
            
            var giftListEntity = new GiftList
            {
                Id = Guid.Parse(giftListId),
                Name = "My Gift List",
                EventName = eventName,
                Owner = "owner123", // Different owner
                OwnerDisplayName = "Owner Doe"
            };

            var currentUser = new AzureUser("user123", "John Doe", "john@example.com", true);

            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId)).ReturnsAsync(giftListEntity);
            _mockAzureUserService.Setup(s => s.GetCurrentUser()).Returns(currentUser);

            // Act
            var result = await _controller.Index(eventName, giftListId, "reserve", giftItemId, null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("GiftLists", redirectResult.ControllerName);
            Assert.Equal(eventName, redirectResult.RouteValues!["eventName"]);
            Assert.Equal(giftListId, redirectResult.RouteValues!["giftListId"]);
            
            _mockGiftItemService.Verify(s => s.ReserveGiftItemAsync(eventName, giftListId, giftItemId, "user123", "John Doe"), Times.Once);
        }

        [Fact]
        public async Task Index_Post_Reserve_ByOwner_PreventsReservationAndRedirects()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();
            var giftItemId = Guid.NewGuid().ToString();
            
            var giftListEntity = new GiftList
            {
                Id = Guid.Parse(giftListId),
                Name = "My Gift List",
                EventName = eventName,
                Owner = "user123", // Same as current user
                OwnerDisplayName = "John Doe"
            };

            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId)).ReturnsAsync(giftListEntity);
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");

            // Act
            var result = await _controller.Index(eventName, giftListId, "reserve", giftItemId, null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("GiftLists", redirectResult.ControllerName);
            
            _mockGiftItemService.Verify(s => s.ReserveGiftItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Index_Post_Unreserve_WithValidGiftItemId_UnreservesGiftItemAndRedirects()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();
            var giftItemId = Guid.NewGuid().ToString();
            
            var giftListEntity = new GiftList
            {
                Id = Guid.Parse(giftListId),
                Name = "My Gift List",
                EventName = eventName,
                Owner = "owner123", // Different owner
                OwnerDisplayName = "Owner Doe"
            };

            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId)).ReturnsAsync(giftListEntity);
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");

            // Act
            var result = await _controller.Index(eventName, giftListId, "unreserve", giftItemId, null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("GiftLists", redirectResult.ControllerName);
            Assert.Equal(eventName, redirectResult.RouteValues!["eventName"]);
            Assert.Equal(giftListId, redirectResult.RouteValues!["giftListId"]);
            
            _mockGiftItemService.Verify(s => s.UnreserveGiftItemAsync(eventName, giftListId, giftItemId, "user123"), Times.Once);
        }

        [Fact]
        public async Task Index_Post_Unreserve_ByOwner_PreventsUnreservationAndRedirects()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();
            var giftItemId = Guid.NewGuid().ToString();
            
            var giftListEntity = new GiftList
            {
                Id = Guid.Parse(giftListId),
                Name = "My Gift List",
                EventName = eventName,
                Owner = "user123", // Same as current user
                OwnerDisplayName = "John Doe"
            };

            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId)).ReturnsAsync(giftListEntity);
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");

            // Act
            var result = await _controller.Index(eventName, giftListId, "unreserve", giftItemId, null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("GiftLists", redirectResult.ControllerName);
            
            _mockGiftItemService.Verify(s => s.UnreserveGiftItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Index_Post_WithMissingAction_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateGiftItemRequest
            {
                Name = "Gift Item",
                Description = "A great gift",
                GiftListId = Guid.NewGuid().ToString()
            };

            // Act
            var result = await _controller.Index("Birthday", Guid.NewGuid().ToString(), null, null, request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Action parameter is required", badRequest.Value);
        }

        [Fact]
        public async Task Index_Post_WithMissingEventName_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateGiftItemRequest
            {
                Name = "Gift Item",
                Description = "A great gift",
                GiftListId = Guid.NewGuid().ToString()
            };

            // Act
            var result = await _controller.Index(null!, Guid.NewGuid().ToString(), "create", null, request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Event name and gift list ID parameters are required", badRequest.Value);
        }

        [Fact]
        public async Task Index_Post_WithUnknownAction_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateGiftItemRequest
            {
                Name = "Gift Item",
                Description = "A great gift",
                GiftListId = Guid.NewGuid().ToString()
            };

            var giftListId = Guid.NewGuid().ToString();
            
            // Mock the gift list service to return a valid gift list so the controller can proceed to handle the unknown action
            _mockGiftListService.Setup(s => s.GetGiftListAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new GiftList 
            { 
                Id = Guid.Parse(giftListId),
                Name = "Test Gift List",
                EventName = "Birthday",
                Owner = "testuser",
                OwnerDisplayName = "Test User"
            });

            // Act
            var result = await _controller.Index("Birthday", giftListId, "unknown", null, request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Unknown action: unknown", badRequest.Value);
        }

        [Fact]
        public async Task Index_Post_Edit_WithMissingId_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateGiftItemRequest
            {
                Name = "Gift Item",
                Description = "A great gift",
                GiftListId = Guid.NewGuid().ToString()
            };

            var giftListId = Guid.NewGuid().ToString();
            _mockGiftListService.Setup(s => s.GetGiftListAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new GiftList 
            { 
                Id = Guid.Parse(giftListId),
                Name = "Test Gift List",
                EventName = "Birthday",
                Owner = "testuser",
                OwnerDisplayName = "Test User"
            });

            // Act
            var result = await _controller.Index("Birthday", giftListId, "edit", null, request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Gift item ID is required for edit action", badRequest.Value);
        }

        [Fact]
        public async Task Index_Post_Delete_WithMissingId_ReturnsBadRequest()
        {
            // Arrange
            var giftListId = Guid.NewGuid().ToString();
            _mockGiftListService.Setup(s => s.GetGiftListAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new GiftList 
            { 
                Id = Guid.Parse(giftListId),
                Name = "Test Gift List",
                EventName = "Birthday",
                Owner = "testuser",
                OwnerDisplayName = "Test User"
            });

            // Act
            var result = await _controller.Index("Birthday", giftListId, "delete", null, null);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Gift item ID is required for delete action", badRequest.Value);
        }

        [Fact]
        public async Task Index_Post_Reserve_WithMissingId_ReturnsBadRequest()
        {
            // Arrange
            var giftListId = Guid.NewGuid().ToString();
            _mockGiftListService.Setup(s => s.GetGiftListAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new GiftList 
            { 
                Id = Guid.Parse(giftListId),
                Name = "Test Gift List",
                EventName = "Birthday",
                Owner = "otheruser", // Different owner to avoid owner check
                OwnerDisplayName = "Other User"
            });

            // Act
            var result = await _controller.Index("Birthday", giftListId, "reserve", null, null);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Gift item ID is required for reserve action", badRequest.Value);
        }

        [Fact]
        public async Task Index_Post_Unreserve_WithMissingId_ReturnsBadRequest()
        {
            // Arrange
            var giftListId = Guid.NewGuid().ToString();
            _mockGiftListService.Setup(s => s.GetGiftListAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new GiftList 
            { 
                Id = Guid.Parse(giftListId),
                Name = "Test Gift List",
                EventName = "Birthday",
                Owner = "otheruser", // Different owner to avoid owner check
                OwnerDisplayName = "Other User"
            });

            // Act
            var result = await _controller.Index("Birthday", giftListId, "unreserve", null, null);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Gift item ID is required for unreserve action", badRequest.Value);
        }

        [Fact]
        public async Task Index_Post_WithNonExistentGiftList_ReturnsNotFound()
        {
            // Arrange
            var request = new CreateGiftItemRequest
            {
                Name = "Gift Item",
                Description = "A great gift",
                GiftListId = Guid.NewGuid().ToString()
            };

            _mockGiftListService.Setup(s => s.GetGiftListAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((GiftList)null!);

            // Act
            var result = await _controller.Index("Birthday", Guid.NewGuid().ToString(), "create", null, request);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // Error Handling Tests

        [Fact]
        public async Task Index_Get_WithException_ReturnsCreateViewWithErrorMessage()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();
            _mockGiftListService.Setup(s => s.GetGiftListAsync(It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Index(eventName, giftListId, "create", null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Create", viewResult.ViewName);
            Assert.IsType<CreateGiftItemRequest>(viewResult.Model);
            Assert.Equal(giftListId, (viewResult.Model as CreateGiftItemRequest)!.GiftListId);
            Assert.Equal("An error occurred while loading the create gift item form.", _controller.ViewBag.ErrorMessage);
        }

        [Fact]
        public async Task Index_Get_WithExceptionOnEdit_ReturnsRedirectWithError()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();
            var giftItemId = Guid.NewGuid().ToString();
            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Index(eventName, giftListId, "edit", giftItemId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("GiftLists", redirectResult.ControllerName);
            Assert.Equal(eventName, redirectResult.RouteValues!["eventName"]);
            Assert.Equal(giftListId, redirectResult.RouteValues!["giftListId"]);
            Assert.Equal("An error occurred while loading the gift item for editing.", _controller.ViewBag.ErrorMessage);
        }

        [Fact]
        public async Task Index_Post_WithException_ReturnsAppropriateResponse()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();
            var request = new CreateGiftItemRequest
            {
                Name = "Gift Item",
                Description = "A great gift",
                GiftListId = giftListId
            };

            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId)).ReturnsAsync(new GiftList { Id = Guid.Parse(giftListId) });
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");
            _mockGiftItemService.Setup(s => s.CreateGiftItemAsync(eventName, request, "user123"))
                               .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Index(eventName, giftListId, "create", null, request);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Create", viewResult.ViewName);
            Assert.Equal(request, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Equal(eventName, _controller.ViewBag.EventName);
            Assert.Equal(giftListId, _controller.ViewBag.GiftListId);
        }

        [Fact]
        public async Task Index_Post_Delete_WithException_ReturnsRedirectToGiftListDetails()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();
            var giftItemId = Guid.NewGuid().ToString();
            
            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId)).ReturnsAsync(new GiftList { Id = Guid.Parse(giftListId) });
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");
            _mockGiftItemService.Setup(s => s.DeleteGiftItemAsync(eventName, giftListId, giftItemId, "user123"))
                               .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Index(eventName, giftListId, "delete", giftItemId, null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("GiftLists", redirectResult.ControllerName);
            Assert.Equal(eventName, redirectResult.RouteValues!["eventName"]);
            Assert.Equal(giftListId, redirectResult.RouteValues!["giftListId"]);
        }

        [Fact]
        public async Task Index_Get_WithExceptionOnDefaultView_ReturnsRedirectWithError()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();
            _mockGiftListService.Setup(s => s.GetGiftListAsync(It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Index(eventName, giftListId, null, null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("GiftLists", redirectResult.ControllerName);
            Assert.Equal(eventName, redirectResult.RouteValues!["eventName"]);
            Assert.Equal(giftListId, redirectResult.RouteValues!["giftListId"]);
            Assert.Equal("An error occurred while loading gift items. Please try again.", _controller.ViewBag.ErrorMessage);
        }

        [Fact]
        public async Task HandleReserve_WithNullCurrentUser_ReturnsUnauthorized()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();
            var giftItemId = Guid.NewGuid().ToString();
            
            var giftListEntity = new GiftList
            {
                Id = Guid.Parse(giftListId),
                Name = "My Gift List",
                EventName = eventName,
                Owner = "owner123",
                OwnerDisplayName = "Owner Doe"
            };

            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId)).ReturnsAsync(giftListEntity);
            _mockAzureUserService.Setup(s => s.GetCurrentUser()).Returns((AzureUser)null!);

            // Act
            var result = await _controller.Index(eventName, giftListId, "reserve", giftItemId, null);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Unable to retrieve current user information.", unauthorizedResult.Value);
        }
    }
}
