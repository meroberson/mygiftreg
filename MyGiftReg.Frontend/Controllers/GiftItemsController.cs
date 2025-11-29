using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyGiftReg.Backend.Interfaces;
using MyGiftReg.Backend.Models;
using MyGiftReg.Backend.Models.DTOs;
using MyGiftReg.Frontend.Services;
using MyGiftReg.Frontend.Authorization;

namespace MyGiftReg.Frontend.Controllers
{
    [Route("Events/{eventName}/GiftLists/{giftListId}/GiftItems")]
    [Authorize(Policy = "RequireMyGiftRegRole")]
    public class GiftItemsController : Controller
    {
        private readonly IGiftItemService _giftItemService;
        private readonly IGiftListService _giftListService;
        private readonly IAzureUserService _azureUserService;
        private readonly ILogger<GiftItemsController> _logger;

        public GiftItemsController(
            IGiftItemService giftItemService, 
            IGiftListService giftListService,
            IAzureUserService azureUserService, 
            ILogger<GiftItemsController> logger)
        {
            _giftItemService = giftItemService;
            _giftListService = giftListService;
            _azureUserService = azureUserService;
            _logger = logger;
        }

        // GET: /Events/{eventName}/GiftLists/{giftListId}/GiftItems?view=create, view=edit
        [HttpGet]
        public async Task<IActionResult> Index(string eventName, string giftListId, string? view, string? id)
        {
            try
            {
                if (string.IsNullOrEmpty(eventName) || string.IsNullOrEmpty(giftListId))
                {
                    return NotFound();
                }

                // Verify the gift list exists and belongs to the current user (for create/edit)
                var giftListEntity = await _giftListService.GetGiftListAsync(eventName, giftListId);
                if (giftListEntity == null)
                {
                    return NotFound();
                }

                var currentUserId = _azureUserService.GetCurrentUserId();
                var isOwner = giftListEntity.Owner == currentUserId;

                // Handle different actions via query parameters
                switch (view?.ToLower())
                {
                    case "create":
                        var createRequest = new CreateGiftItemRequest 
                        { 
                            GiftListId = giftListId
                        };
                        ViewBag.EventName = eventName;
                        ViewBag.GiftListId = giftListId;
                        ViewBag.GiftListName = giftListEntity.Name;
                        return View("Create", createRequest);
                    
                    case "edit":
                        if (string.IsNullOrEmpty(id))
                        {
                            return NotFound();
                        }
                        
                        var giftItemEntity = await _giftItemService.GetGiftItemAsync(eventName, giftListId, id, currentUserId);
                        if (giftItemEntity == null)
                        {
                            return NotFound();
                        }

                        ViewBag.EventName = eventName;
                        ViewBag.GiftListId = giftListId;
                        ViewBag.GiftItemId = id;
                        ViewBag.GiftListName = giftListEntity.Name;
                        
                        return View("Edit", giftItemEntity);
                    
                    default:
                        // Redirect to gift list details if no valid view specified
                        return RedirectToAction("Details", "GiftLists", new { eventName = eventName, giftListId = giftListId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling gift items action {Action} with eventName {EventName}, giftListId {GiftListId} and id {Id}", view, eventName, giftListId, id);
                
                if (view == "create")
                {
                    ViewBag.EventName = eventName;
                    ViewBag.GiftListId = giftListId;
                    ViewBag.ErrorMessage = "An error occurred while loading the create gift item form.";
                    return View("Create", new CreateGiftItemRequest { GiftListId = giftListId });
                }
                else if (view == "edit")
                {
                    ViewBag.EventName = eventName;
                    ViewBag.GiftListId = giftListId;
                    ViewBag.ErrorMessage = "An error occurred while loading the gift item for editing.";
                    return RedirectToAction("Details", "GiftLists", new { eventName, giftListId });
                }
                else
                {
                    ViewBag.EventName = eventName;
                    ViewBag.GiftListId = giftListId;
                    ViewBag.ErrorMessage = "An error occurred while loading gift items. Please try again.";
                    return RedirectToAction("Details", "GiftLists", new { eventName, giftListId });
                }
            }
        }

        // POST: /Events/{eventName}/GiftLists/{giftListId}/GiftItems (handles create, edit, delete, reserve, unreserve actions)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string eventName, string giftListId, string? action, string? id, CreateGiftItemRequest? request)
        {
            try
            {
                if (string.IsNullOrEmpty(action))
                {
                    return BadRequest("Action parameter is required");
                }

                if (string.IsNullOrEmpty(eventName) || string.IsNullOrEmpty(giftListId))
                {
                    return BadRequest("Event name and gift list ID parameters are required");
                }

                // Verify the gift list exists
                var giftListEntity = await _giftListService.GetGiftListAsync(eventName, giftListId);
                if (giftListEntity == null)
                {
                    return NotFound();
                }

                var currentUserId = _azureUserService.GetCurrentUserId();
                var isOwner = giftListEntity.Owner == currentUserId;

                switch (action.ToLower())
                {
                    case "create":
                        return await HandleCreate(eventName, giftListId, request);
                    
                    case "edit":
                        if (string.IsNullOrEmpty(id))
                        {
                            return BadRequest("Gift item ID is required for edit action");
                        }
                        return await HandleEdit(eventName, giftListId, id, request);
                    
                    case "delete":
                        if (string.IsNullOrEmpty(id))
                        {
                            return BadRequest("Gift item ID is required for delete action");
                        }
                        return await HandleDelete(eventName, giftListId, id);
                    
                    case "reserve":
                        // Prevent owners from reserving items
                        if (isOwner)
                        {
                            return RedirectToAction("Details", "GiftLists", new { eventName = eventName, giftListId = giftListId });
                        }
                        
                        if (string.IsNullOrEmpty(id))
                        {
                            return BadRequest("Gift item ID is required for reserve action");
                        }
                        return await HandleReserve(eventName, giftListId, id);
                    
                    case "unreserve":
                        // Prevent owners from unreserving items
                        if (isOwner)
                        {
                            return RedirectToAction("Details", "GiftLists", new { eventName = eventName, giftListId = giftListId });
                        }
                        
                        if (string.IsNullOrEmpty(id))
                        {
                            return BadRequest("Gift item ID is required for unreserve action");
                        }
                        return await HandleUnreserve(eventName, giftListId, id);
                    
                    default:
                        return BadRequest($"Unknown action: {action}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling POST action {Action} with eventName {EventName}, giftListId {GiftListId} and id {Id}", action, eventName, giftListId, id);
                
                // Set ViewBag values for error view
                ViewBag.EventName = eventName;
                ViewBag.GiftListId = giftListId;
                
                // Handle different action types for error cases
                if (action?.ToLower() == "create" || action?.ToLower() == "edit")
                {
                    if (request != null)
                    {
                        ModelState.AddModelError("", "An error occurred while processing the request. Please try again.");
                        return View(action.ToLower() == "create" ? "Create" : "Edit", request);
                    }
                }
                
                return RedirectToAction("Details", "GiftLists", new { eventName = eventName, giftListId = giftListId });
            }
        }

        private async Task<IActionResult> HandleCreate(string eventName, string giftListId, CreateGiftItemRequest? request)
        {
            if (request == null)
            {
                return BadRequest("Create request is required");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var currentUserId = _azureUserService.GetCurrentUserId();
                    var createdGiftItem = await _giftItemService.CreateGiftItemAsync(eventName, request, currentUserId);
                    return RedirectToAction("Details", "GiftLists", new { eventName = eventName, giftListId = giftListId });
                }
                catch (MyGiftReg.Backend.Exceptions.ValidationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            // Preserve context for the view
            ViewBag.EventName = eventName;
            ViewBag.GiftListId = giftListId;
            return View("Create", request);
        }

        private async Task<IActionResult> HandleEdit(string eventName, string giftListId, string itemId, CreateGiftItemRequest? request)
        {
            if (request == null)
            {
                return BadRequest("Edit request is required");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var currentUserId = _azureUserService.GetCurrentUserId();
                    await _giftItemService.UpdateGiftItemAsync(eventName, giftListId, itemId, request, currentUserId);
                    return RedirectToAction("Details", "GiftLists", new { eventName, giftListId });
                }
                catch (MyGiftReg.Backend.Exceptions.NotFoundException)
                {
                    return NotFound();
                }
                catch (MyGiftReg.Backend.Exceptions.ValidationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            // If we get here, there was a validation error, reload the gift item for editing
            var giftItemEntity = await _giftItemService.GetGiftItemAsync(eventName, giftListId, itemId, _azureUserService.GetCurrentUserId());
            if (giftItemEntity == null)
            {
                return NotFound();
            }

            request.Name = giftItemEntity.Name; // Preserve the original name
            request.Description = giftItemEntity.Description; // Preserve the original description
            request.Url = giftItemEntity.Url; // Preserve the original URL
            request.Quantity = giftItemEntity.Quantity; // Preserve the original quantity

            ViewBag.EventName = eventName;
            ViewBag.GiftListId = giftListId;
            ViewBag.GiftItemId = itemId;

            return View("Edit", request);
        }

        private async Task<IActionResult> HandleDelete(string eventName, string giftListId, string itemId)
        {
            try
            {
                var currentUserId = _azureUserService.GetCurrentUserId();
                await _giftItemService.DeleteGiftItemAsync(eventName, giftListId, itemId, currentUserId);
                return RedirectToAction("Details", "GiftLists", new { eventName, giftListId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting gift item {GiftItemId} for gift list {GiftListId} in event {EventName}", itemId, giftListId, eventName);
                return RedirectToAction("Details", "GiftLists", new { eventName, giftListId });
            }
        }

        private async Task<IActionResult> HandleReserve(string eventName, string giftListId, string itemId)
        {
            try
            {
                var currentUser = _azureUserService.GetCurrentUser();
                if (currentUser == null)
                {
                    return Unauthorized("Unable to retrieve current user information.");
                }

                await _giftItemService.ReserveGiftItemAsync(eventName, giftListId, itemId, currentUser.Id, currentUser.DisplayName);
                return RedirectToAction("Details", "GiftLists", new { eventName, giftListId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reserving gift item {GiftItemId} for gift list {GiftListId} in event {EventName}", itemId, giftListId, eventName);
                return RedirectToAction("Details", "GiftLists", new { eventName, giftListId });
            }
        }

        private async Task<IActionResult> HandleUnreserve(string eventName, string giftListId, string itemId)
        {
            try
            {
                var currentUserId = _azureUserService.GetCurrentUserId();
                await _giftItemService.UnreserveGiftItemAsync(eventName, giftListId, itemId, currentUserId);
                return RedirectToAction("Details", "GiftLists", new { eventName, giftListId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unreserving gift item {GiftItemId} for gift list {GiftListId} in event {EventName}", itemId, giftListId, eventName);
                return RedirectToAction("Details", "GiftLists", new { eventName, giftListId });
            }
        }
    }
}
