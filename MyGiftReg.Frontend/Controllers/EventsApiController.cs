using Microsoft.AspNetCore.Mvc;
using MyGiftReg.Backend.Interfaces;
using MyGiftReg.Backend.Models.DTOs;
using MyGiftReg.Backend.Models;

namespace MyGiftReg.Frontend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsApiController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly ILogger<EventsApiController> _logger;

        // Temporary development user ID until Entra authentication is implemented
        private const string DevelopmentUserId = "development-user";

        public EventsApiController(IEventService eventService, ILogger<EventsApiController> logger)
        {
            _eventService = eventService;
            _logger = logger;
        }

        // GET: api/Events
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
        {
            try
            {
                var events = await _eventService.GetAllEventsAsync();
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events");
                return StatusCode(500, new { error = "An error occurred while retrieving events" });
            }
        }

        // GET: api/Events/{eventName}
        [HttpGet("{eventName}")]
        public async Task<ActionResult<Event>> GetEvent(string eventName)
        {
            try
            {
                var eventEntity = await _eventService.GetEventAsync(eventName);
                if (eventEntity == null)
                {
                    return NotFound(new { error = $"Event '{eventName}' not found" });
                }

                return Ok(eventEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event {EventName}", eventName);
                return StatusCode(500, new { error = "An error occurred while retrieving the event" });
            }
        }

        // POST: api/Events
        [HttpPost]
        public async Task<ActionResult<Event>> CreateEvent([FromBody] CreateEventRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdEvent = await _eventService.CreateEventAsync(request, DevelopmentUserId);
                return CreatedAtAction(nameof(GetEvent), new { eventName = createdEvent.Name }, createdEvent);
            }
            catch (MyGiftReg.Backend.Exceptions.ValidationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event");
                return StatusCode(500, new { error = "An error occurred while creating the event" });
            }
        }

        // PUT: api/Events/{eventName}
        [HttpPut("{eventName}")]
        public async Task<IActionResult> UpdateEvent(string eventName, [FromBody] CreateEventRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                await _eventService.UpdateEventAsync(eventName, request, DevelopmentUserId);
                return NoContent();
            }
            catch (MyGiftReg.Backend.Exceptions.NotFoundException)
            {
                return NotFound(new { error = $"Event '{eventName}' not found" });
            }
            catch (MyGiftReg.Backend.Exceptions.ValidationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (MyGiftReg.Backend.Exceptions.ConcurrencyException)
            {
                return Conflict(new { error = "Event was modified by another user. Please refresh and try again." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event {EventName}", eventName);
                return StatusCode(500, new { error = "An error occurred while updating the event" });
            }
        }

        // DELETE: api/Events/{eventName}
        [HttpDelete("{eventName}")]
        public async Task<IActionResult> DeleteEvent(string eventName)
        {
            try
            {
                await _eventService.DeleteEventAsync(eventName, DevelopmentUserId);
                return NoContent();
            }
            catch (MyGiftReg.Backend.Exceptions.NotFoundException)
            {
                return NotFound(new { error = $"Event '{eventName}' not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event {EventName}", eventName);
                return StatusCode(500, new { error = "An error occurred while deleting the event" });
            }
        }
    }
}
