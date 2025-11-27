using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyGiftReg.Backend.Interfaces;
using MyGiftReg.Backend.Models;
using MyGiftReg.Backend.Models.DTOs;
using MyGiftReg.Frontend.Services;
using MyGiftReg.Frontend.Authorization;

namespace MyGiftReg.Frontend.Controllers
{
    [Route("Events")]
    [Authorize(Policy = "RequireMyGiftRegRole")]
    public class EventsController : Controller
    {
        private readonly IEventService _eventService;
        private readonly IGiftListService _giftListService;
        private readonly IAzureUserService _azureUserService;
        private readonly ILogger<EventsController> _logger;

        public EventsController(IEventService eventService, IGiftListService giftListService, IAzureUserService azureUserService, ILogger<EventsController> logger)
        {
            _eventService = eventService;
            _giftListService = giftListService;
            _azureUserService = azureUserService;
            _logger = logger;
        }

        // GET: /Events or /Events?view=create or /Events?view=edit&id=eventName
        [HttpGet]
        public async Task<IActionResult> Index(string? view, string? id)
        {
            try
            {
                // Handle different actions via query parameters
                switch (view?.ToLower())
                {
                    case "create":
                        return View("Create", new CreateEventRequest());
                    
                    case "edit":
                        if (string.IsNullOrEmpty(id))
                        {
                            return NotFound();
                        }
                        
                        var eventEntity = await _eventService.GetEventAsync(id);
                        if (eventEntity == null)
                        {
                            return NotFound();
                        }

                        var editRequest = new CreateEventRequest
                        {
                            Name = eventEntity.Name,
                            Description = eventEntity.Description,
                            EventDate = eventEntity.EventDate
                        };

                        return View("Edit", editRequest);
                    
                    default:
                        // Default action: show events list
                        var events = await _eventService.GetAllEventsAsync();
                        return View(events);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling events action {Action} with id {Id}", view, id);
                
                if (view == "create")
                {
                    ViewBag.ErrorMessage = "An error occurred while loading the create event form.";
                    return View("Create", new CreateEventRequest());
                }
                else if (view == "edit")
                {
                    ViewBag.ErrorMessage = "An error occurred while loading the event for editing.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ViewBag.ErrorMessage = "An error occurred while loading events. Please try again.";
                    return View(new List<Event>());
                }
            }
        }

        // POST: /Events (handles create, edit, delete actions)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string? action, string? id, CreateEventRequest? request)
        {
            try
            {
                if (string.IsNullOrEmpty(action))
                {
                    return BadRequest("Action parameter is required");
                }

                switch (action.ToLower())
                {
                    case "create":
                        return await HandleCreate(request);
                    
                    case "edit":
                        if (string.IsNullOrEmpty(id))
                        {
                            return BadRequest("Event name is required for edit action");
                        }
                        return await HandleEdit(id, request);
                    
                    case "delete":
                        if (string.IsNullOrEmpty(id))
                        {
                            return BadRequest("Event name is required for delete action");
                        }
                        return await HandleDelete(id);
                    
                    default:
                        return BadRequest($"Unknown action: {action}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling POST action {Action} with id {Id}", action, id);
                
                // Handle different action types for error cases
                if (action?.ToLower() == "create" || action?.ToLower() == "edit")
                {
                    if (request != null)
                    {
                        ModelState.AddModelError("", "An error occurred while processing the request. Please try again.");
                        return View(action.ToLower() == "create" ? "Create" : "Edit", request);
                    }
                }
                
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Events/MyEvent (clean path-based URL for event details)
        [HttpGet("{eventName}")]
        public async Task<IActionResult> Details(string eventName)
        {
            try
            {
                if (string.IsNullOrEmpty(eventName))
                {
                    return NotFound();
                }

                var eventEntity = await _eventService.GetEventAsync(eventName);
                if (eventEntity == null)
                {
                    return NotFound();
                }

                var currentUserId = _azureUserService.GetCurrentUserId();
                
                // Get user's gift lists and others' gift lists for this event
                var myGiftLists = await _giftListService.GetGiftListsByEventAndUserAsync(eventName, currentUserId);
                var othersGiftLists = await _giftListService.GetGiftListsByEventForOthersAsync(eventName, currentUserId);

                // Combine the lists with a flag to indicate ownership
                var allGiftLists = myGiftLists.Select(gl => new { GiftList = gl, IsOwnedByCurrentUser = true })
                                             .Concat(othersGiftLists.Select(gl => new { GiftList = gl, IsOwnedByCurrentUser = false }))
                                             .ToList();

                ViewBag.EventName = eventName;
                ViewBag.MyGiftLists = myGiftLists;
                ViewBag.OthersGiftLists = othersGiftLists;
                ViewBag.AllGiftLists = allGiftLists;

                return View(eventEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event details for {EventName}", eventName);
                ViewBag.ErrorMessage = "An error occurred while loading event details. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task<IActionResult> HandleCreate(CreateEventRequest? request)
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
                    var createdEvent = await _eventService.CreateEventAsync(request, currentUserId);
                    return RedirectToAction(nameof(Details), new { eventName = createdEvent!.Name });
                }
                catch (MyGiftReg.Backend.Exceptions.ValidationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return View("Create", request);
        }

        private async Task<IActionResult> HandleEdit(string eventName, CreateEventRequest? request)
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
                    await _eventService.UpdateEventAsync(eventName, request, currentUserId);
                    return RedirectToAction(nameof(Details), new { eventName = eventName });
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

            // If we get here, there was a validation error, reload the event for editing
            var eventEntity = await _eventService.GetEventAsync(eventName);
            if (eventEntity == null)
            {
                return NotFound();
            }

            request.Name = eventEntity.Name; // Preserve the original name

            return View("Edit", request);
        }

        private async Task<IActionResult> HandleDelete(string eventName)
        {
            try
            {
                var currentUserId = _azureUserService.GetCurrentUserId();
                await _eventService.DeleteEventAsync(eventName, currentUserId);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event {EventName}", eventName);
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
