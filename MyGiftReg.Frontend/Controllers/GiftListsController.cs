using Microsoft.AspNetCore.Mvc;
using MyGiftReg.Backend.Interfaces;
using MyGiftReg.Backend.Models.DTOs;
using MyGiftReg.Backend.Models;

namespace MyGiftReg.Frontend.Controllers
{
    [Route("api/events/{eventName}/[controller]")]
    [ApiController]
    public class GiftListsController : ControllerBase
    {
        private readonly IGiftListService _giftListService;
        private readonly ILogger<GiftListsController> _logger;

        // Temporary development user ID until Entra authentication is implemented
        private const string DevelopmentUserId = "development-user";

        public GiftListsController(IGiftListService giftListService, ILogger<GiftListsController> logger)
        {
            _giftListService = giftListService;
            _logger = logger;
        }

        // GET: api/events/{eventName}/giftlists
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GiftList>>> GetGiftLists(string eventName)
        {
            try
            {
                var giftLists = await _giftListService.GetGiftListsByEventAsync(eventName);
                return Ok(giftLists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting gift lists for event {EventName}", eventName);
                return StatusCode(500, new { error = "An error occurred while retrieving gift lists" });
            }
        }

        // GET: api/events/{eventName}/giftlists/mylist
        [HttpGet("mylist")]
        public async Task<ActionResult<IEnumerable<GiftList>>> GetMyGiftLists(string eventName)
        {
            try
            {
                var giftLists = await _giftListService.GetGiftListsByEventAndUserAsync(eventName, DevelopmentUserId);
                return Ok(giftLists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my gift lists for event {EventName}", eventName);
                return StatusCode(500, new { error = "An error occurred while retrieving your gift lists" });
            }
        }

        // GET: api/events/{eventName}/giftlists/others
        [HttpGet("others")]
        public async Task<ActionResult<IEnumerable<GiftList>>> GetOthersGiftLists(string eventName)
        {
            try
            {
                var giftLists = await _giftListService.GetGiftListsByEventForOthersAsync(eventName, DevelopmentUserId);
                return Ok(giftLists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting others' gift lists for event {EventName}", eventName);
                return StatusCode(500, new { error = "An error occurred while retrieving others' gift lists" });
            }
        }

        // GET: api/events/{eventName}/giftlists/{giftListId}
        [HttpGet("{giftListId}")]
        public async Task<ActionResult<GiftList>> GetGiftList(string eventName, string giftListId)
        {
            try
            {
                var giftList = await _giftListService.GetGiftListAsync(eventName, giftListId);
                if (giftList == null)
                {
                    return NotFound(new { error = $"Gift list '{giftListId}' not found for event '{eventName}'" });
                }

                return Ok(giftList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting gift list {GiftListId} for event {EventName}", giftListId, eventName);
                return StatusCode(500, new { error = "An error occurred while retrieving the gift list" });
            }
        }

        // POST: api/events/{eventName}/giftlists
        [HttpPost]
        public async Task<ActionResult<GiftList>> CreateGiftList(string eventName, [FromBody] CreateGiftListRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdGiftList = await _giftListService.CreateGiftListAsync(request, DevelopmentUserId);
                return CreatedAtAction(nameof(GetGiftList), new { eventName = eventName, giftListId = createdGiftList.Id }, createdGiftList);
            }
            catch (MyGiftReg.Backend.Exceptions.ValidationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating gift list for event {EventName}", eventName);
                return StatusCode(500, new { error = "An error occurred while creating the gift list" });
            }
        }

        // PUT: api/events/{eventName}/giftlists/{giftListId}
        [HttpPut("{giftListId}")]
        public async Task<IActionResult> UpdateGiftList(string eventName, string giftListId, [FromBody] CreateGiftListRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                await _giftListService.UpdateGiftListAsync(eventName, giftListId, request, DevelopmentUserId);
                return NoContent();
            }
            catch (MyGiftReg.Backend.Exceptions.NotFoundException)
            {
                return NotFound(new { error = $"Gift list '{giftListId}' not found for event '{eventName}'" });
            }
            catch (MyGiftReg.Backend.Exceptions.ValidationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (MyGiftReg.Backend.Exceptions.ConcurrencyException)
            {
                return Conflict(new { error = "Gift list was modified by another user. Please refresh and try again." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating gift list {GiftListId} for event {EventName}", giftListId, eventName);
                return StatusCode(500, new { error = "An error occurred while updating the gift list" });
            }
        }

        // DELETE: api/events/{eventName}/giftlists/{giftListId}
        [HttpDelete("{giftListId}")]
        public async Task<IActionResult> DeleteGiftList(string eventName, string giftListId)
        {
            try
            {
                await _giftListService.DeleteGiftListAsync(eventName, giftListId, DevelopmentUserId);
                return NoContent();
            }
            catch (MyGiftReg.Backend.Exceptions.NotFoundException)
            {
                return NotFound(new { error = $"Gift list '{giftListId}' not found for event '{eventName}'" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting gift list {GiftListId} for event {EventName}", giftListId, eventName);
                return StatusCode(500, new { error = "An error occurred while deleting the gift list" });
            }
        }
    }
}
