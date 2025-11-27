using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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
    public class GiftListsControllerTests
    {
        private readonly Mock<IGiftListService> _mockGiftListService;
        private readonly Mock<IGiftItemService> _mockGiftItemService;
        private readonly Mock<IAzureUserService> _mockAzureUserService;
        private readonly Mock<ILogger<GiftListsController>> _mockLogger;
        private readonly GiftListsController _controller;
        private readonly DefaultHttpContext _httpContext;

        public GiftListsControllerTests()
        {
            _mockGiftListService = new Mock<IGiftListService>();
            _mockGiftItemService = new Mock<IGiftItemService>();
            _mockAzureUserService = new Mock<IAzureUserService>();
            _mockLogger = new Mock<ILogger<GiftListsController>>();
            
            _controller = new GiftListsController(
                _mockGiftListService.Object,
                _mockGiftItemService.Object,
                _mockAzureUserService.Object,
                _mockLogger.Object);

            _httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };
            
            // Initialize TempData and ViewData to prevent NullReferenceException
            _controller.TempData = new TempDataDictionary(_httpContext, Mock.Of<ITempDataProvider>());
        }

        [Fact]
        public async Task Index_Get_DefaultAction_RedirectsToEventDetails()
        {
            // Arrange
            var eventName = "Birthday";

            // Act
            var result = await _controller.Index(eventName, null, null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Events", redirectResult.ControllerName);
            Assert.Equal(eventName, redirectResult.RouteValues!["eventName"]);
        }

        [Fact]
        public async Task Index_Get_CreateView_ReturnsCreateView()
        {
            // Arrange
            var eventName = "Birthday";

            // Act
            var result = await _controller.Index(eventName, "create", null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Create", viewResult.ViewName);
            var createRequest = Assert.IsType<CreateGiftListRequest>(viewResult.Model);
            Assert.Equal(eventName, createRequest.EventName);
        }

        [Fact]
        public async Task Index_Get_EditView_WithValidGiftListId_ReturnsEditView()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid();
            var giftListEntity = new GiftList
            {
                Id = giftListId,
                Name = "My Gift List",
                EventName = eventName,
                Owner = "user123",
                OwnerDisplayName = "John Doe"
            };

            var giftItems = new List<GiftItem>
            {
                new GiftItem { Id = Guid.NewGuid(), Name = "Gift 1", Description = "Description 1", GiftListId = giftListId }
            };

            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId.ToString())).ReturnsAsync(giftListEntity);
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");
            _mockGiftItemService.Setup(s => s.GetGiftItemsByListAsync(eventName, giftListId.ToString(), "user123")).ReturnsAsync(giftItems);

            // Act
            var result = await _controller.Index(eventName, "edit", giftListId.ToString());

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Edit", viewResult.ViewName);
            Assert.Equal(giftListId.ToString(), _controller.ViewBag.GiftListId);
            Assert.Equal(eventName, _controller.ViewBag.EventName);
            Assert.Equal(giftItems, _controller.ViewBag.GiftItems);
            Assert.Equal(giftListEntity, viewResult.Model);
        }

        [Fact]
        public async Task Index_Get_EditView_WithMissingGiftListId_ReturnsNotFound()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();

            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId)).ReturnsAsync((GiftList)null!);

            // Act
            var result = await _controller.Index(eventName, "edit", giftListId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Index_Get_WithMissingEventName_ReturnsNotFound()
        {
            // Act
            var result = await _controller.Index(null!, "create", null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Index_Get_EditView_WithMissingGiftListIdInRequest_ReturnsNotFound()
        {
            // Act
            var result = await _controller.Index("Birthday", "edit", null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // POST Tests

        [Fact]
        public async Task Index_Post_Create_WithValidRequest_CreatesGiftListAndRedirects()
        {
            // Arrange
            var eventName = "Birthday";
            var createRequest = new CreateGiftListRequest
            {
                Name = "My Gift List",
                EventName = eventName
            };

            var createdGiftList = new GiftList
            {
                Id = Guid.NewGuid(),
                Name = createRequest.Name,
                EventName = eventName,
                Owner = "user123",
                OwnerDisplayName = "John Doe"
            };

            var currentUser = new AzureUser("user123", "John Doe", "john@example.com", true);

            _mockAzureUserService.Setup(s => s.GetCurrentUser()).Returns(currentUser);
            _mockGiftListService.Setup(s => s.CreateGiftListAsync(It.IsAny<CreateGiftListRequest>(), "user123", "John Doe")).ReturnsAsync(createdGiftList);

            // Act
            var result = await _controller.Index(eventName, "create", null, createRequest);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal(eventName, redirectResult.RouteValues!["eventName"]);
            Assert.Equal(createdGiftList.Id.ToString(), redirectResult.RouteValues!["giftListId"]);
        }

        [Fact]
        public async Task Index_Post_Create_WithInvalidModelState_ReturnsCreateView()
        {
            // Arrange
            var eventName = "Birthday";
            var createRequest = new CreateGiftListRequest
            {
                Name = "My Gift List",
                EventName = eventName
            };

            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await _controller.Index(eventName, "create", null, createRequest);

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
            var eventName = "Birthday";
            var createRequest = new CreateGiftListRequest
            {
                Name = "My Gift List",
                EventName = eventName
            };

            var validationException = new MyGiftReg.Backend.Exceptions.ValidationException("Gift list name already exists");
            
            var currentUser = new AzureUser("user123", "John Doe", "john@example.com", true);

            _mockAzureUserService.Setup(s => s.GetCurrentUser()).Returns(currentUser);
            _mockGiftListService.Setup(s => s.CreateGiftListAsync(It.IsAny<CreateGiftListRequest>(), "user123", "John Doe"))
                               .ThrowsAsync(validationException);

            // Act
            var result = await _controller.Index(eventName, "create", null, createRequest);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Create", viewResult.ViewName);
            Assert.Equal(createRequest, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
            Assert.NotNull(_controller.ModelState[""]);
            Assert.Contains("Gift list name already exists", _controller.ModelState[""]!.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Index_Post_Create_WithNullUser_ReturnsCreateViewWithError()
        {
            // Arrange
            var eventName = "Birthday";
            var createRequest = new CreateGiftListRequest
            {
                Name = "My Gift List",
                EventName = eventName
            };

            _mockAzureUserService.Setup(s => s.GetCurrentUser()).Returns((AzureUser)null!);

            // Act
            var result = await _controller.Index(eventName, "create", null, createRequest);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Create", viewResult.ViewName);
            Assert.Equal(createRequest, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
            Assert.NotNull(_controller.ModelState[""]);
            Assert.Contains("Unable to retrieve current user information.", _controller.ModelState[""]!.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Index_Post_Edit_WithValidRequest_UpdatesGiftListAndRedirects()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid();
            var editRequest = new CreateGiftListRequest
            {
                Name = "Updated Gift List",
                EventName = eventName
            };

            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");

            // Act
            var result = await _controller.Index(eventName, "edit", giftListId.ToString(), editRequest);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal(eventName, redirectResult.RouteValues!["eventName"]);
            Assert.Equal(giftListId.ToString(), redirectResult.RouteValues!["giftListId"]);
            
            _mockGiftListService.Verify(s => s.UpdateGiftListAsync(eventName, giftListId.ToString(), editRequest, "user123"), Times.Once);
        }

        [Fact]
        public async Task Index_Post_Edit_WithNotFoundException_ReturnsNotFound()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid();
            var editRequest = new CreateGiftListRequest
            {
                Name = "Updated Gift List",
                EventName = eventName
            };

            var notFoundException = new MyGiftReg.Backend.Exceptions.NotFoundException("Gift list not found");
            
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");
            _mockGiftListService.Setup(s => s.UpdateGiftListAsync(eventName, giftListId.ToString(), editRequest, "user123"))
                               .ThrowsAsync(notFoundException);

            // Act
            var result = await _controller.Index(eventName, "edit", giftListId.ToString(), editRequest);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Index_Post_Edit_WithValidationError_RepresentsEditViewWithGiftList()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid();
            var editRequest = new CreateGiftListRequest
            {
                Name = "Updated Gift List",
                EventName = eventName
            };

            var originalGiftList = new GiftList
            {
                Id = giftListId,
                Name = "Original Gift List",
                EventName = eventName,
                Owner = "user123",
                OwnerDisplayName = "John Doe"
            };

            var validationException = new MyGiftReg.Backend.Exceptions.ValidationException("Name too short");
            
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");
            _mockGiftListService.Setup(s => s.UpdateGiftListAsync(eventName, giftListId.ToString(), editRequest, "user123"))
                               .ThrowsAsync(validationException);
            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId.ToString())).ReturnsAsync(originalGiftList);

            // Act
            var result = await _controller.Index(eventName, "edit", giftListId.ToString(), editRequest);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Edit", viewResult.ViewName);
            Assert.Equal(editRequest, viewResult.Model);
            Assert.Equal(giftListId.ToString(), _controller.ViewBag.GiftListId);
            Assert.Equal(originalGiftList.Name, editRequest.Name); // Name should be preserved
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Index_Post_Delete_WithValidGiftListId_DeletesGiftListAndRedirects()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid();
            
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");

            // Act
            var result = await _controller.Index(eventName, "delete", giftListId.ToString(), null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Events", redirectResult.ControllerName);
            Assert.Equal(eventName, redirectResult.RouteValues!["eventName"]);
            
            _mockGiftListService.Verify(s => s.DeleteGiftListAsync(eventName, giftListId.ToString(), "user123"), Times.Once);
        }

        [Fact]
        public async Task Index_Post_WithMissingAction_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateGiftListRequest
            {
                Name = "My Gift List",
                EventName = "Birthday"
            };

            // Act
            var result = await _controller.Index("Birthday", null, null, request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Action parameter is required", badRequest.Value);
        }

        [Fact]
        public async Task Index_Post_WithMissingEventName_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateGiftListRequest
            {
                Name = "My Gift List",
                EventName = "Birthday"
            };

            // Act
            var result = await _controller.Index(null!, "create", null, request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Event name parameter is required", badRequest.Value);
        }

        [Fact]
        public async Task Index_Post_WithUnknownAction_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateGiftListRequest
            {
                Name = "My Gift List",
                EventName = "Birthday"
            };

            // Act
            var result = await _controller.Index("Birthday", "unknown", null, request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Unknown action: unknown", badRequest.Value);
        }

        [Fact]
        public async Task Index_Post_Edit_WithMissingId_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateGiftListRequest
            {
                Name = "My Gift List",
                EventName = "Birthday"
            };

            // Act
            var result = await _controller.Index("Birthday", "edit", null, request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Gift list ID is required for edit action", badRequest.Value);
        }

        [Fact]
        public async Task Index_Post_Delete_WithMissingId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.Index("Birthday", "delete", null, null);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Gift list ID is required for delete action", badRequest.Value);
        }

        // Details Action Tests

        [Fact]
        public async Task Details_Get_WithValidParameters_ReturnsGiftListDetails()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid();
            var giftListEntity = new GiftList
            {
                Id = giftListId,
                Name = "My Gift List",
                EventName = eventName,
                Owner = "user123",
                OwnerDisplayName = "John Doe"
            };

            var giftItems = new List<GiftItem>
            {
                new GiftItem { Id = Guid.NewGuid(), Name = "Gift 1", Description = "Description 1", GiftListId = giftListId }
            };

            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId.ToString())).ReturnsAsync(giftListEntity);
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");
            _mockGiftItemService.Setup(s => s.GetGiftItemsByListAsync(eventName, giftListId.ToString(), "user123")).ReturnsAsync(giftItems);

            // Act
            var result = await _controller.Details(eventName, giftListId.ToString());

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(eventName, _controller.ViewBag.EventName);
            Assert.Equal(giftListId.ToString(), _controller.ViewBag.GiftListId);
            Assert.Equal(giftItems, _controller.ViewBag.GiftItems);
            Assert.True(_controller.ViewBag.IsOwner);
            Assert.Equal("user123", _controller.ViewBag.CurrentUserId);
            Assert.Equal("John Doe", _controller.ViewBag.OwnerDisplayName);
            Assert.Equal(giftListEntity, viewResult.Model);
        }

        [Fact]
        public async Task Details_Get_WithMissingEventName_ReturnsNotFound()
        {
            // Arrange
            var giftListId = Guid.NewGuid().ToString();

            // Act
            var result = await _controller.Details(null!, giftListId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_Get_WithMissingGiftListId_ReturnsNotFound()
        {
            // Arrange
            var eventName = "Birthday";

            // Act
            var result = await _controller.Details(eventName, null!);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_Get_WithNonExistentGiftList_ReturnsNotFound()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();

            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId)).ReturnsAsync((GiftList)null!);

            // Act
            var result = await _controller.Details(eventName, giftListId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_Get_WithException_ReturnsRedirectToEventDetails()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();

            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId)).ThrowsAsync(new Exception("Database error"));
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");
            _mockGiftItemService.Setup(s => s.GetGiftItemsByListAsync(eventName, giftListId, "user123")).ReturnsAsync(new List<GiftItem>());

            // Act
            var result = await _controller.Details(eventName, giftListId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Events", redirectResult.ControllerName);
            Assert.Equal(eventName, redirectResult.RouteValues!["eventName"]);
            Assert.Equal("An error occurred while loading gift list details. Please try again.", _controller.TempData["ErrorMessage"]);
        }

        // Error Handling Tests

        [Fact]
        public async Task Index_Get_WithExceptionOnEdit_ReturnsRedirectWithError()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid().ToString();
            _mockGiftListService.Setup(s => s.GetGiftListAsync(eventName, giftListId)).ThrowsAsync(new Exception("Database error"));
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");
            _mockGiftItemService.Setup(s => s.GetGiftItemsByListAsync(eventName, giftListId, "user123")).ReturnsAsync(new List<GiftItem>());

            // Act
            var result = await _controller.Index(eventName, "edit", giftListId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Events", redirectResult.ControllerName);
            Assert.Equal(eventName, redirectResult.RouteValues!["eventName"]);
            Assert.Equal("An error occurred while loading the gift list for editing.", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task Index_Post_WithException_ReturnsAppropriateResponse()
        {
            // Arrange
            var eventName = "Birthday";
            var request = new CreateGiftListRequest
            {
                Name = "My Gift List",
                EventName = eventName
            };

            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");
            _mockGiftListService.Setup(s => s.CreateGiftListAsync(It.IsAny<CreateGiftListRequest>(), "user123", It.IsAny<string>()))
                               .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Index(eventName, "create", null, request);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Create", viewResult.ViewName);
            Assert.Equal(request, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Index_Post_Delete_WithException_ReturnsRedirectToGiftListDetails()
        {
            // Arrange
            var eventName = "Birthday";
            var giftListId = Guid.NewGuid();
            
            _mockAzureUserService.Setup(s => s.GetCurrentUserId()).Returns("user123");
            _mockGiftListService.Setup(s => s.DeleteGiftListAsync(eventName, giftListId.ToString(), "user123"))
                               .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Index(eventName, "delete", giftListId.ToString(), null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal(eventName, redirectResult.RouteValues!["eventName"]);
            Assert.Equal(giftListId.ToString(), redirectResult.RouteValues!["giftListId"]);
        }
    }
}
