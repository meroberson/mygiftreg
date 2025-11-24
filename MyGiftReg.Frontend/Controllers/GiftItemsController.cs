using Microsoft.AspNetCore.Mvc;
using MyGiftReg.Backend.Interfaces;
using MyGiftReg.Backend.Models;
using MyGiftReg.Backend.Models.DTOs;
using MyGiftReg.Frontend.Services;

namespace MyGiftReg.Frontend.Controllers
{
    [Route("Events/{eventName}/GiftLists/{giftListId}/GiftItems")]
    public class GiftItemsController : Controller
    {
        private readonly IGiftItemService _giftItemService;
        private readonly IGiftListService _giftListService;
        private readonly IDevelopmentUserService _developmentUserService;
        private readonly ILogger<GiftItemsController> _logger;

        public GiftItemsController(
            IGiftItemService giftItemService, 
            IGiftListService giftListService,
            IDevelopmentUserService developmentUserService, 
            ILogger<GiftItemsController> logger)
        {
            _giftItemService = giftItemService;
            _giftListService = giftListService;
            _developmentUserService = developmentUserService;
            _logger = logger;
        }

        // GET: /Events/{eventName}/GiftLists/{giftListId}/GiftItems or with view=create, view=edit, view=reserve
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

                var currentUserId = _developmentUserService.GetCurrentUserId();
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
                    
                    case "reserve":
                        // Prevent owners from accessing reserve view
                        if (isOwner)
                        {
                            return RedirectToAction("Details", "GiftLists", new { eventName = eventName, giftListId = giftListId });
                        }
                        
                        if (string.IsNullOrEmpty(id))
                        {
                            return NotFound();
                        }
                        
                        var reserveItemEntity = await _giftItemService.GetGiftItemAsync(eventName, giftListId, id, currentUserId);
                        if (reserveItemEntity == null)
                        {
                            return NotFound();
                        }

                        ViewBag.EventName = eventName;
                        ViewBag.GiftListId = giftListId;
                        ViewBag.GiftItemId = id;
                        ViewBag.GiftListName = giftListEntity.Name;
                        ViewBag.CurrentUserId = currentUserId;
                        ViewBag.IsOwner = isOwner;
                        
                        return View("Reserve", reserveItemEntity);
                    
                    default:
                        // Default action: show gift items for the gift list
                        var giftItems = await _giftItemService.GetGiftItemsByListAsync(eventName, giftListId, currentUserId);
                        ViewBag.EventName = eventName;
                        ViewBag.GiftListId = giftListId;
                        ViewBag.GiftListName = giftListEntity.Name;
                        ViewBag.IsOwner = isOwner;
                        ViewBag.GiftItems = giftItems;
                        return View(giftItems);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling gift items action {Action} with eventName {EventName}, giftListId {GiftListId} and id {Id}", view, eventName, giftListId, id);
                
                    if (view == "create")
                    {
                        ViewBag.ErrorMessage = "An error occurred while loading the create gift item form.";
                        return View("Create", new CreateGiftItemRequest { GiftListId = giftListId });
                    }
                else if (view == "edit")
                {
                    ViewBag.ErrorMessage = "An error occurred while loading the gift item for editing.";
                    return RedirectToAction(nameof(Index), new { eventName = eventName, giftListId = giftListId });
                }
                else if (view == "reserve")
                {
                    ViewBag.ErrorMessage = "An error occurred while loading the gift item for reservation.";
                    return RedirectToAction("Reserve", "GiftLists", new { eventName = eventName, giftListId = giftListId });
                }
                else
                {
                    ViewBag.ErrorMessage = "An error occurred while loading gift items. Please try again.";
                    return RedirectToAction("Details", "GiftLists", new { eventName = eventName, giftListId = giftListId });
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

                var currentUserId = _developmentUserService.GetCurrentUserId();
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

        // GET: /Events/{eventName}/GiftLists/{giftListId}/GiftItems/{giftItemGUID} (clean path-based URL for gift item details)
        [HttpGet("{itemId}")]
        public async Task<IActionResult> Details(string eventName, string giftListId, string itemId)
        {
            try
            {
                if (string.IsNullOrEmpty(eventName) || string.IsNullOrEmpty(giftListId) || string.IsNullOrEmpty(itemId))
                {
                    return NotFound();
                }

                var giftItemEntity = await _giftItemService.GetGiftItemAsync(eventName, giftListId, itemId, _developmentUserService.GetCurrentUserId());
                if (giftItemEntity == null)
                {
                    return NotFound();
                }

                // Verify the gift list exists
                var giftListEntity = await _giftListService.GetGiftListAsync(eventName, giftListId);
                if (giftListEntity == null)
                {
                    return NotFound();
                }

                var currentUserId = _developmentUserService.GetCurrentUserId();
                var isOwner = giftListEntity.Owner == currentUserId;

                ViewBag.EventName = eventName;
                ViewBag.GiftListId = giftListId;
                ViewBag.GiftItemId = itemId;
                ViewBag.GiftListName = giftListEntity.Name;
                ViewBag.CurrentUserId = currentUserId;
                ViewBag.IsOwner = isOwner;

                return View(giftItemEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting gift item details for {EventName}/{GiftListId}/{GiftItemId}", eventName, giftListId, itemId);
                ViewBag.ErrorMessage = "An error occurred while loading gift item details. Please try again.";
                return RedirectToAction("Details", "GiftLists", new { eventName, giftListId });
            }
        }

        // POST: Edit gift item
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Edit")]
        public async Task<IActionResult> Edit(string eventName, string giftListId, string itemId, GiftItem model)
        {
            var currentUserId = _developmentUserService.GetCurrentUserId();
            if (ModelState.IsValid)
            {
                try
                {
                    var request = new CreateGiftItemRequest
                    {
                        Name = model.Name,
                        Description = model.Description,
                        Url = model.Url,
                        GiftListId = giftListId
                    };

                    await _giftItemService.UpdateGiftItemAsync(eventName, giftListId, itemId, request, currentUserId);
                    return RedirectToAction(nameof(Details), new { eventName, giftListId, itemId });
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
            var giftItemEntity = await _giftItemService.GetGiftItemAsync(eventName, giftListId, itemId, currentUserId);
            if (giftItemEntity == null)
            {
                return NotFound();
            }

            // Preserve the original values
            model.Name = giftItemEntity.Name;
            model.Description = giftItemEntity.Description;
            model.Url = giftItemEntity.Url;
            model.Id = giftItemEntity.Id;
            model.GiftListId = giftItemEntity.GiftListId;
            model.CreatedDate = giftItemEntity.CreatedDate;
            model.ReservedBy = giftItemEntity.ReservedBy;
            
            ViewBag.EventName = eventName;
            ViewBag.GiftListId = giftListId;
            ViewBag.GiftItemId = itemId;

            return View("Edit", model);
        }

        // POST: Delete gift item
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Delete")]
        public async Task<IActionResult> Delete(string eventName, string giftListId, string itemId)
        {
            try
            {
                var currentUserId = _developmentUserService.GetCurrentUserId();
                await _giftItemService.DeleteGiftItemAsync(eventName, giftListId, itemId, currentUserId);
                return RedirectToAction("Details", "GiftLists", new { eventName, giftListId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting gift item {GiftItemId} for gift list {GiftListId} in event {EventName}", itemId, giftListId, eventName);
                return RedirectToAction(nameof(Details), new { eventName, giftListId, itemId });
            }
        }

        // POST: Reserve gift item
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Reserve")]
        public async Task<IActionResult> Reserve(string eventName, string giftListId, string itemId)
        {
            try
            {
                // Check if user owns the gift list
                var giftListEntity = await _giftListService.GetGiftListAsync(eventName, giftListId);
                if (giftListEntity == null)
                {
                    return NotFound();
                }

                var currentUserId = _developmentUserService.GetCurrentUserId();
                var isOwner = giftListEntity.Owner == currentUserId;

                // Prevent owners from reserving items
                if (isOwner)
                {
                    return RedirectToAction("Details", "GiftLists", new { eventName = eventName, giftListId = giftListId });
                }

                await _giftItemService.ReserveGiftItemAsync(eventName, giftListId, itemId, currentUserId);
                return RedirectToAction("Reserve", "GiftLists", new { eventName, giftListId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reserving gift item {GiftItemId} for gift list {GiftListId} in event {EventName}", itemId, giftListId, eventName);
                return RedirectToAction(nameof(Index), new { eventName, giftListId, view = "reserve", id = itemId });
            }
        }

        // POST: Unreserve gift item
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Unreserve")]
        public async Task<IActionResult> Unreserve(string eventName, string giftListId, string itemId)
        {
            try
            {
                // Check if user owns the gift list
                var giftListEntity = await _giftListService.GetGiftListAsync(eventName, giftListId);
                if (giftListEntity == null)
                {
                    return NotFound();
                }

                var currentUserId = _developmentUserService.GetCurrentUserId();
                var isOwner = giftListEntity.Owner == currentUserId;

                // Prevent owners from unreserving items
                if (isOwner)
                {
                    return RedirectToAction("Details", "GiftLists", new { eventName = eventName, giftListId = giftListId });
                }

                await _giftItemService.UnreserveGiftItemAsync(eventName, giftListId, itemId, currentUserId);
                return RedirectToAction("Reserve", "GiftLists", new { eventName, giftListId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unreserving gift item {GiftItemId} for gift list {GiftListId} in event {EventName}", itemId, giftListId, eventName);
                return RedirectToAction(nameof(Index), new { eventName, giftListId, view = "reserve", id = itemId });
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
                    var currentUserId = _developmentUserService.GetCurrentUserId();
                    var createdGiftItem = await _giftItemService.CreateGiftItemAsync(eventName, request, currentUserId);
                    return RedirectToAction(nameof(Details), new { eventName = eventName, giftListId = giftListId, itemId = createdGiftItem!.Id.ToString() });
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
                    var currentUserId = _developmentUserService.GetCurrentUserId();
                    await _giftItemService.UpdateGiftItemAsync(eventName, giftListId, itemId, request, currentUserId);
                    return RedirectToAction(nameof(Details), new { eventName, giftListId, itemId });
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
            var giftItemEntity = await _giftItemService.GetGiftItemAsync(eventName, giftListId, itemId, _developmentUserService.GetCurrentUserId());
            if (giftItemEntity == null)
            {
                return NotFound();
            }

            request.Name = giftItemEntity.Name; // Preserve the original name
            request.Description = giftItemEntity.Description; // Preserve the original description
            request.Url = giftItemEntity.Url; // Preserve the original URL

            ViewBag.EventName = eventName;
            ViewBag.GiftListId = giftListId;
            ViewBag.GiftItemId = itemId;

            return View("Edit", request);
        }

        private async Task<IActionResult> HandleDelete(string eventName, string giftListId, string itemId)
        {
            try
            {
                var currentUserId = _developmentUserService.GetCurrentUserId();
                await _giftItemService.DeleteGiftItemAsync(eventName, giftListId, itemId, currentUserId);
                return RedirectToAction("Details", "GiftLists", new { eventName, giftListId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting gift item {GiftItemId} for gift list {GiftListId} in event {EventName}", itemId, giftListId, eventName);
                return RedirectToAction(nameof(Details), new { eventName, giftListId, itemId });
            }
        }

        private async Task<IActionResult> HandleReserve(string eventName, string giftListId, string itemId)
        {
            try
            {
                var currentUserId = _developmentUserService.GetCurrentUserId();
                await _giftItemService.ReserveGiftItemAsync(eventName, giftListId, itemId, currentUserId);
                return RedirectToAction("Reserve", "GiftLists", new { eventName, giftListId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reserving gift item {GiftItemId} for gift list {GiftListId} in event {EventName}", itemId, giftListId, eventName);
                return RedirectToAction(nameof(Index), new { eventName, giftListId, view = "reserve", id = itemId });
            }
        }

        private async Task<IActionResult> HandleUnreserve(string eventName, string giftListId, string itemId)
        {
            try
            {
                var currentUserId = _developmentUserService.GetCurrentUserId();
                await _giftItemService.UnreserveGiftItemAsync(eventName, giftListId, itemId, currentUserId);
                return RedirectToAction("Reserve", "GiftLists", new { eventName, giftListId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unreserving gift item {GiftItemId} for gift list {GiftListId} in event {EventName}", itemId, giftListId, eventName);
                return RedirectToAction(nameof(Index), new { eventName, giftListId, view = "reserve", id = itemId });
            }
        }
    }
}
