using Microsoft.AspNetCore.Mvc;
using MyGiftReg.Backend.Interfaces;
using MyGiftReg.Backend.Models.DTOs;
using MyGiftReg.Backend.Models;

namespace MyGiftReg.Frontend.Controllers
{
    [Route("api/events/{eventName}/giftlists/{giftListId}/[controller]")]
    [ApiController]
    public class GiftItemsController : ControllerBase
    {
        private readonly IGiftItemService _giftItemService;
        private readonly ILogger<GiftItemsController> _logger;

        // Temporary development user ID until Entra authentication is implemented
        private const string DevelopmentUserId = "development-user";

        public GiftItemsController(IGiftItemService giftItemService, ILogger<GiftItemsController> logger)
        {
            _giftItemService = giftItemService;
            _logger = logger;
        }

        // GET: api/events/{eventName}/giftlists/{giftListId}/items
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GiftItem>>> GetGiftItems(string eventName, string giftListId)
        {
            try
            {
                var giftItems = await _giftItemService.GetGiftItemsByListAsync(giftListId, DevelopmentUserId);
                return Ok(giftItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting gift items for gift list {GiftListId} in event {EventName}", giftListId, eventName);
                return StatusCode(500, new { error = "An error occurred while retrieving gift items" });
            }
        }

        // GET: api/events/{eventName}/giftlists/{giftListId}/items/{itemId}
        [HttpGet("{itemId}")]
        public async Task<ActionResult<GiftItem>> GetGiftItem(string eventName, string giftListId, string itemId)
        {
            try
            {
                var giftItem = await _giftItemService.GetGiftItemAsync(giftListId, itemId);
                if (giftItem == null)
                {
                    return NotFound(new { error = $"Gift item '{itemId}' not found in gift list '{giftListId}'" });
                }

                return Ok(giftItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting gift item {ItemId} for gift list {GiftListId} in event {EventName}", itemId, giftListId, eventName);
                return StatusCode(500, new { error = "An error occurred while retrieving the gift item" });
            }
        }

        // POST: api/events/{eventName}/giftlists/{giftListId}/items
        [HttpPost]
        public async Task<ActionResult<GiftItem>> CreateGiftItem(string eventName, string giftListId, [FromBody] CreateGiftItemRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdGiftItem = await _giftItemService.CreateGiftItemAsync(request, DevelopmentUserId);
                return CreatedAtAction(nameof(GetGiftItem), new { eventName = eventName, giftListId = giftListId, itemId = createdGiftItem.Id }, createdGiftItem);
            }
            catch (MyGiftReg.Backend.Exceptions.ValidationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating gift item for gift list {GiftListId} in event {EventName}", giftListId, eventName);
                return StatusCode(500, new { error = "An error occurred while creating the gift item" });
            }
        }

        // PUT: api/events/{eventName}/giftlists/{giftListId}/items/{itemId}
        [HttpPut("{itemId}")]
        public async Task<IActionResult> UpdateGiftItem(string eventName, string giftListId, string itemId, [FromBody] CreateGiftItemRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                await _giftItemService.UpdateGiftItemAsync(giftListId, itemId, request, DevelopmentUserId);
                return NoContent();
            }
            catch (MyGiftReg.Backend.Exceptions.NotFoundException)
            {
                return NotFound(new { error = $"Gift item '{itemId}' not found in gift list '{giftListId}'" });
            }
            catch (MyGiftReg.Backend.Exceptions.ValidationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (MyGiftReg.Backend.Exceptions.ConcurrencyException)
            {
                return Conflict(new { error = "Gift item was modified by another user. Please refresh and try again." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating gift item {ItemId} for gift list {GiftListId} in event {EventName}", itemId, giftListId, eventName);
                return StatusCode(500, new { error = "An error occurred while updating the gift item" });
            }
        }

        // DELETE: api/events/{eventName}/giftlists/{giftListId}/items/{itemId}
        [HttpDelete("{itemId}")]
        public async Task<IActionResult> DeleteGiftItem(string eventName, string giftListId, string itemId)
        {
            try
            {
                await _giftItemService.DeleteGiftItemAsync(giftListId, itemId, DevelopmentUserId);
                return NoContent();
            }
            catch (MyGiftReg.Backend.Exceptions.NotFoundException)
            {
                return NotFound(new { error = $"Gift item '{itemId}' not found in gift list '{giftListId}'" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting gift item {ItemId} for gift list {GiftListId} in event {EventName}", itemId, giftListId, eventName);
                return StatusCode(500, new { error = "An error occurred while deleting the gift item" });
            }
        }

        // POST: api/events/{eventName}/giftlists/{giftListId}/items/{itemId}/reserve
        [HttpPost("{itemId}/reserve")]
        public async Task<ActionResult<GiftItem>> ReserveGiftItem(string eventName, string giftListId, string itemId)
        {
            try
            {
                var reservedGiftItem = await _giftItemService.ReserveGiftItemAsync(giftListId, itemId, DevelopmentUserId);
                return Ok(reservedGiftItem);
            }
            catch (MyGiftReg.Backend.Exceptions.NotFoundException)
            {
                return NotFound(new { error = $"Gift item '{itemId}' not found in gift list '{giftListId}'" });
            }
            catch (MyGiftReg.Backend.Exceptions.ValidationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reserving gift item {ItemId} for gift list {GiftListId} in event {EventName}", itemId, giftListId, eventName);
                return StatusCode(500, new { error = "An error occurred while reserving the gift item" });
            }
        }

        // POST: api/events/{eventName}/giftlists/{giftListId}/items/{itemId}/unreserve
        [HttpPost("{itemId}/unreserve")]
        public async Task<IActionResult> UnreserveGiftItem(string eventName, string giftListId, string itemId)
        {
            try
            {
                var success = await _giftItemService.UnreserveGiftItemAsync(giftListId, itemId, DevelopmentUserId);
                if (!success)
                {
                    return Conflict(new { error = "Unable to unreserve the gift item. It may not be reserved or you may not have permission to unreserve it." });
                }

                return NoContent();
            }
            catch (MyGiftReg.Backend.Exceptions.NotFoundException)
            {
                return NotFound(new { error = $"Gift item '{itemId}' not found in gift list '{giftListId}'" });
            }
            catch (MyGiftReg.Backend.Exceptions.ValidationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unreserving gift item {ItemId} for gift list {GiftListId} in event {EventName}", itemId, giftListId, eventName);
                return StatusCode(500, new { error = "An error occurred while unreserving the gift item" });
            }
        }
    }
}
