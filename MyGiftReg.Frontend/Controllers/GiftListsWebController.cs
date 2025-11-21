using Microsoft.AspNetCore.Mvc;
using MyGiftReg.Backend.Interfaces;
using MyGiftReg.Backend.Models;
using MyGiftReg.Backend.Models.DTOs;

namespace MyGiftReg.Frontend.Controllers
{
    public class GiftListsWebController : Controller
    {
        private readonly IGiftListService _giftListService;
        private readonly IGiftItemService _giftItemService;
        private readonly ILogger<GiftListsWebController> _logger;

        // Temporary development user ID until Entra authentication is implemented
        private const string DevelopmentUserId = "development-user";

        public GiftListsWebController(IGiftListService giftListService, IGiftItemService giftItemService, ILogger<GiftListsWebController> logger)
        {
            _giftListService = giftListService;
            _giftItemService = giftItemService;
            _logger = logger;
        }

        // GET: /GiftLists/Edit/{eventName}/{giftListId}
        public async Task<IActionResult> Edit(string eventName, string giftListId)
        {
            try
            {
                if (string.IsNullOrEmpty(eventName) || string.IsNullOrEmpty(giftListId))
                {
                    return NotFound();
                }

                var giftList = await _giftListService.GetGiftListAsync(eventName, giftListId);
                if (giftList == null)
                {
                    return NotFound();
                }

                // Check if current user owns this gift list
                if (giftList.Owner != DevelopmentUserId)
                {
                    return Forbid("You can only edit your own gift lists");
                }

                // Get gift items for this list
                var giftItems = await _giftItemService.GetGiftItemsByListAsync(eventName, giftListId);

                ViewBag.EventName = eventName;
                ViewBag.GiftListId = giftListId;
                ViewBag.GiftList = giftList;
                ViewBag.GiftItems = giftItems;

                return View(giftList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting gift list for edit {EventName}/{GiftListId}", eventName, giftListId);
                ViewBag.ErrorMessage = "An error occurred while loading the gift list for editing.";
                return RedirectToAction("Details", "Events", new { eventName = eventName });
            }
        }

        // POST: /GiftLists/Edit/{eventName}/{giftListId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string eventName, string giftListId, CreateGiftListRequest request)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Verify ownership before updating
                    var existingGiftList = await _giftListService.GetGiftListAsync(eventName, giftListId);
                    if (existingGiftList == null || existingGiftList.Owner != DevelopmentUserId)
                    {
                        return Forbid("You can only edit your own gift lists");
                    }

                    await _giftListService.UpdateGiftListAsync(eventName, giftListId, request, DevelopmentUserId);
                    return RedirectToAction("Edit", new { eventName = eventName, giftListId = giftListId });
                }

                // Reload data for the view if model state is invalid
                var giftList = await _giftListService.GetGiftListAsync(eventName, giftListId);
                var giftItems = await _giftItemService.GetGiftItemsByListAsync(eventName, giftListId);

                ViewBag.EventName = eventName;
                ViewBag.GiftListId = giftListId;
                ViewBag.GiftList = giftList;
                ViewBag.GiftItems = giftItems;

                return View(giftList);
            }
            catch (MyGiftReg.Backend.Exceptions.NotFoundException)
            {
                return NotFound();
            }
            catch (MyGiftReg.Backend.Exceptions.ConcurrencyException)
            {
                ModelState.AddModelError("", "Gift list was modified by another user. Please refresh and try again.");
                
                // Reload data
                var giftList = await _giftListService.GetGiftListAsync(eventName, giftListId);
                var giftItems = await _giftItemService.GetGiftItemsByListAsync(eventName, giftListId);

                ViewBag.EventName = eventName;
                ViewBag.GiftListId = giftListId;
                ViewBag.GiftList = giftList;
                ViewBag.GiftItems = giftItems;

                return View(giftList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating gift list {EventName}/{GiftListId}", eventName, giftListId);
                ModelState.AddModelError("", "An error occurred while updating the gift list. Please try again.");
                
                // Reload data
                var giftList = await _giftListService.GetGiftListAsync(eventName, giftListId);
                var giftItems = await _giftItemService.GetGiftItemsByListAsync(eventName, giftListId);

                ViewBag.EventName = eventName;
                ViewBag.GiftListId = giftListId;
                ViewBag.GiftList = giftList;
                ViewBag.GiftItems = giftItems;

                return View(giftList);
            }
        }

        // GET: /GiftLists/Reserve/{eventName}/{giftListId}
        public async Task<IActionResult> Reserve(string eventName, string giftListId)
        {
            try
            {
                if (string.IsNullOrEmpty(eventName) || string.IsNullOrEmpty(giftListId))
                {
                    return NotFound();
                }

                var giftList = await _giftListService.GetGiftListAsync(eventName, giftListId);
                if (giftList == null)
                {
                    return NotFound();
                }

                // Check that current user doesn't own this gift list
                if (giftList.Owner == DevelopmentUserId)
                {
                    return Forbid("You cannot reserve items from your own gift list");
                }

                // Get gift items for this list with reservation status
                var giftItems = await _giftItemService.GetGiftItemsByListAsync(eventName, giftListId);

                ViewBag.EventName = eventName;
                ViewBag.GiftListId = giftListId;
                ViewBag.GiftList = giftList;
                ViewBag.GiftItems = giftItems;

                return View(giftList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting gift list for reserve {EventName}/{GiftListId}", eventName, giftListId);
                ViewBag.ErrorMessage = "An error occurred while loading the gift list for reservation.";
                return RedirectToAction("Details", "Events", new { eventName = eventName });
            }
        }

        // POST: /GiftLists/Create/{eventName}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string eventName, CreateGiftListRequest request)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var createdGiftList = await _giftListService.CreateGiftListAsync(request, DevelopmentUserId);
                    return RedirectToAction("Edit", new { eventName = eventName, giftListId = createdGiftList.Id });
                }

                return RedirectToAction("Details", "Events", new { eventName = eventName });
            }
            catch (MyGiftReg.Backend.Exceptions.ValidationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return RedirectToAction("Details", "Events", new { eventName = eventName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating gift list for event {EventName}", eventName);
                ModelState.AddModelError("", "An error occurred while creating the gift list. Please try again.");
                return RedirectToAction("Details", "Events", new { eventName = eventName });
            }
        }

        // POST: /GiftLists/Delete/{eventName}/{giftListId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string eventName, string giftListId)
        {
            try
            {
                await _giftListService.DeleteGiftListAsync(eventName, giftListId, DevelopmentUserId);
                return RedirectToAction("Details", "Events", new { eventName = eventName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting gift list {EventName}/{GiftListId}", eventName, giftListId);
                return RedirectToAction("Edit", new { eventName = eventName, giftListId = giftListId });
            }
        }
    }
}
