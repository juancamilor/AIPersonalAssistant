using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIPersonalAssistant.Web.Services;
using AIPersonalAssistant.Web.Models;

namespace AIPersonalAssistant.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TravelController : ControllerBase
{
    private readonly ITravelService _travelService;

    public TravelController(ITravelService travelService)
    {
        _travelService = travelService;
    }

    private string GetUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? "anonymous";
    }

    [HttpGet("pins")]
    public async Task<IActionResult> GetPins()
    {
        var userId = GetUserId();
        var pins = await _travelService.GetPinsAsync(userId);
        return Ok(pins);
    }

    [HttpGet("pins/{id}")]
    public async Task<IActionResult> GetPin(string id)
    {
        var userId = GetUserId();
        var pin = await _travelService.GetPinAsync(userId, id);
        
        if (pin == null)
        {
            return NotFound(new { error = "Pin not found" });
        }
        
        return Ok(pin);
    }

    [HttpPost("pins")]
    public async Task<IActionResult> CreatePin([FromBody] TravelPinRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Invalid request" });
        }

        if (request.Latitude < -90 || request.Latitude > 90)
        {
            return BadRequest(new { error = "Latitude must be between -90 and 90" });
        }

        if (request.Longitude < -180 || request.Longitude > 180)
        {
            return BadRequest(new { error = "Longitude must be between -180 and 180" });
        }

        var userId = GetUserId();
        var pin = await _travelService.CreatePinAsync(userId, request);
        
        return CreatedAtAction(nameof(GetPin), new { id = pin.Id }, pin);
    }

    [HttpPut("pins/{id}")]
    public async Task<IActionResult> UpdatePin(string id, [FromBody] TravelPinRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Invalid request" });
        }

        var userId = GetUserId();
        var pin = await _travelService.UpdatePinAsync(userId, id, request);
        
        if (pin == null)
        {
            return NotFound(new { error = "Pin not found" });
        }
        
        return Ok(pin);
    }

    [HttpDelete("pins/{id}")]
    public async Task<IActionResult> DeletePin(string id)
    {
        var userId = GetUserId();
        var result = await _travelService.DeletePinAsync(userId, id);
        
        if (!result)
        {
            return NotFound(new { error = "Pin not found" });
        }
        
        return NoContent();
    }
}
