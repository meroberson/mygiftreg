using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyGiftReg.Backend.Interfaces;
using MyGiftReg.Backend.Models;
using MyGiftReg.Backend.Models.DTOs;
using MyGiftReg.Frontend.Services;
using MyGiftReg.Frontend.Authorization;

namespace MyGiftReg.Frontend.Controllers
{
    [Route("Events/{eventName}/GiftLists")]
    [Authorize(Policy = "RequireMyGiftRegRole")]
    public class GiftListsController : Controller
    {
        private readonly IGiftListService _giftListService;
        private readonly IGiftItemService _giftItemService;
        private readonly IAzureUserService _azureUserService;
        private readonly ILogger<GiftListsController> _logger;

        public GiftListsController(IGiftListService giftListService, IGiftItemService giftItemService, IAzureUserService azureUserService, ILogger<GiftListsController> logger)
        {
            _giftListService = giftListService;
            _giftItemService = giftItemService;
            _azureUserService = azureUserService;
            _logger = logger;
        }

        // GET: /Events/{eventName}/GiftLists?view=create or /Events/{eventName}/GiftLists?view=edit&id=giftListId
        [HttpGet]
        public async Task<IActionResult> Index(string eventName, string? view, string? id)
        {
            try
            {
                if (string.IsNullOrEmpty(eventName))
                {
                    return NotFound();
                }

                // Handle different actions via query parameters
                switch (view?.ToLower())
                {
                    case "create":
                        var createRequest = new CreateGiftListRequest { EventName = eventName };
                        return View("Create", createRequest);
                    
                    case "edit":
                        if (string.IsNullOrEmpty(id))
                        {
                            return NotFound();
                        }
                        
                        var giftListEntity = await _giftListService.GetGiftListAsync(eventName, id);
                        if (giftListEntity == null)
                        {
                            return NotFound();
                        }

                        ViewBag.GiftListId = id;
                        ViewBag.EventName = eventName;
                        
                        // Get gift items for this gift list
                        var currentUserId = _azureUserService.GetCurrentUserId();
                        var giftItems = await _giftItemService.GetGiftItemsByListAsync(eventName, id, currentUserId);
                        ViewBag.GiftItems = giftItems;
                        
                        return View("Edit", giftListEntity);
                    
                    default:
                        // Redirect to event details if no valid view specified
                        return RedirectToAction("Details", "Events", new { eventName });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling gift lists action {Action} with eventName {EventName} and id {Id}", view, eventName, id);
                
                if (view == "create")
                {
                    ViewBag.ErrorMessage = "An error occurred while loading the create gift list form.";
                    return View("Create", new CreateGiftListRequest { EventName = eventName });
                }
                else if (view == "edit")
                {
                    ViewBag.ErrorMessage = "An error occurred while loading the gift list for editing.";
                    return RedirectToAction("Details", "Events", new { eventName });
                }
                else
                {
                    ViewBag.ErrorMessage = "An error occurred while loading gift lists. Please try again.";
                    return RedirectToAction("Details", "Events", new { eventName });
                }
            }
        }

        // POST: /Events/{eventName}/GiftLists (handles create, edit, delete actions)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string eventName, string? action, string? id, CreateGiftListRequest? request)
        {
            try
            {
                if (string.IsNullOrEmpty(action))
                {
                    return BadRequest("Action parameter is required");
                }

                if (string.IsNullOrEmpty(eventName))
                {
                    return BadRequest("Event name parameter is required");
                }

                switch (action.ToLower())
                {
                    case "create":
                        return await HandleCreate(eventName, request);
                    
                    case "edit":
                        if (string.IsNullOrEmpty(id))
                        {
                            return BadRequest("Gift list ID is required for edit action");
                        }
                        return await HandleEdit(eventName, id, request);
                    
                    case "delete":
                        if (string.IsNullOrEmpty(id))
                        {
                            return BadRequest("Gift list ID is required for delete action");
                        }
                        return await HandleDelete(eventName, id);
                    
                    default:
                        return BadRequest($"Unknown action: {action}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling POST action {Action} with eventName {EventName} and id {Id}", action, eventName, id);
                
                // Handle different action types for error cases
                if (action?.ToLower() == "create" || action?.ToLower() == "edit")
                {
                    if (request != null)
                    {
                        ModelState.AddModelError("", "An error occurred while processing the request. Please try again.");
                        return View(action.ToLower() == "create" ? "Create" : "Edit", request);
                    }
                }
                
                return RedirectToAction("Details", "Events", new { eventName = eventName });
            }
        }

        // GET: /Events/{eventName}/GiftLists/{giftListGUID} (clean path-based URL for gift list details)
        [HttpGet("{giftListId}")]
        public async Task<IActionResult> Details(string eventName, string giftListId)
        {
            try
            {
                if (string.IsNullOrEmpty(eventName) || string.IsNullOrEmpty(giftListId))
                {
                    return NotFound();
                }

                var giftListEntity = await _giftListService.GetGiftListAsync(eventName, giftListId);
                if (giftListEntity == null)
                {
                    return NotFound();
                }

                var currentUserId = _azureUserService.GetCurrentUserId();
                
                // Get gift items for this gift list
                var giftItems = await _giftItemService.GetGiftItemsByListAsync(eventName, giftListId, currentUserId);

                ViewBag.EventName = eventName;
                ViewBag.GiftListId = giftListId;
                ViewBag.GiftItems = giftItems;
                ViewBag.IsOwner = giftListEntity.Owner == currentUserId;
                ViewBag.CurrentUserId = currentUserId;
                ViewBag.OwnerDisplayName = giftListEntity.OwnerDisplayName;

                return View(giftListEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting gift list details for {EventName}/{GiftListId}", eventName, giftListId);
                ViewBag.ErrorMessage = "An error occurred while loading gift list details. Please try again.";
                return RedirectToAction("Details", "Events", new { eventName });
            }
        }

        // GET: /Events/{eventName}/GiftLists/{giftListGUID}/Reserve (reservation interface)
        [HttpGet("{giftListId}/Reserve")]
        public async Task<IActionResult> Reserve(string eventName, string giftListId)
        {
            try
            {
                if (string.IsNullOrEmpty(eventName) || string.IsNullOrEmpty(giftListId))
                {
                    return NotFound();
                }

                var giftListEntity = await _giftListService.GetGiftListAsync(eventName, giftListId);
                if (giftListEntity == null)
                {
                    return NotFound();
                }

                // Check if the current user owns this gift list
                var currentUserId = _azureUserService.GetCurrentUserId();
                var isOwner = giftListEntity.Owner == currentUserId;

                // Prevent owners from accessing reservation interface
                if (isOwner)
                {
                    return RedirectToAction(nameof(Details), new { eventName = eventName, giftListId = giftListId });
                }

                // Get gift items for reservation
                var giftItems = await _giftItemService.GetGiftItemsByListAsync(eventName, giftListId, currentUserId);

                ViewBag.EventName = eventName;
                ViewBag.GiftListId = giftListId;
                ViewBag.GiftItems = giftItems;
                ViewBag.CurrentUserId = currentUserId;
                ViewBag.OwnerDisplayName = giftListEntity.OwnerDisplayName;

                return View(giftListEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reservation page for {EventName}/{GiftListId}", eventName, giftListId);
                ViewBag.ErrorMessage = "An error occurred while loading the reservation page. Please try again.";
                return RedirectToAction(nameof(Details), new { eventName, giftListId });
            }
        }

        private async Task<IActionResult> HandleCreate(string eventName, CreateGiftListRequest? request)
        {
            if (request == null)
            {
                return BadRequest("Create request is required");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var currentUser = _azureUserService.GetCurrentUser();
                    if (currentUser == null)
                    {
                        ModelState.AddModelError("", "Unable to retrieve current user information.");
                        return View("Create", request);
                    }

                    var createdGiftList = await _giftListService.CreateGiftListAsync(request, currentUser.Id, currentUser.DisplayName);
                    return RedirectToAction(nameof(Details), new { eventName = eventName, giftListId = createdGiftList!.Id });
                }
                catch (MyGiftReg.Backend.Exceptions.ValidationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return View("Create", request);
        }

        private async Task<IActionResult> HandleEdit(string eventName, string giftListId, CreateGiftListRequest? request)
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
                    await _giftListService.UpdateGiftListAsync(eventName, giftListId, request, currentUserId);
                    return RedirectToAction(nameof(Details), new { eventName, giftListId });
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

            // If we get here, there was a validation error, reload the gift list for editing
            var giftListEntity = await _giftListService.GetGiftListAsync(eventName, giftListId);
            if (giftListEntity == null)
            {
                return NotFound();
            }

            request.Name = giftListEntity.Name; // Preserve the original name
            ViewBag.GiftListId = giftListId;

            return View("Edit", request);
        }

        private async Task<IActionResult> HandleDelete(string eventName, string giftListId)
        {
            try
            {
                var currentUserId = _azureUserService.GetCurrentUserId();
                await _giftListService.DeleteGiftListAsync(eventName, giftListId, currentUserId);
                return RedirectToAction("Details", "Events", new { eventName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting gift list {GiftListId} for event {EventName}", giftListId, eventName);
                return RedirectToAction(nameof(Details), new { eventName, giftListId });
            }
        }
    }
}
