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
    private readonly ITravelImageService _imageService;

    public TravelController(ITravelService travelService, ITravelImageService imageService)
    {
        _travelService = travelService;
        _imageService = imageService;
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

    [HttpPost("pins/{pinId}/images")]
    public async Task<IActionResult> UploadImage(string pinId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest("Only JPEG, PNG, and WebP images are allowed");

        var userId = GetUserId();
        var pin = await _travelService.GetPinAsync(userId, pinId);
        if (pin == null)
            return NotFound("Pin not found");
        if (pin.ImageUrls?.Count >= 5)
            return BadRequest("Maximum 5 images per pin");

        using var stream = file.OpenReadStream();
        var imageId = await _imageService.SaveImageAsync(pinId, stream, file.FileName);

        pin.ImageUrls ??= new List<string>();
        pin.ImageUrls.Add(imageId);
        await _travelService.UpdatePinAsync(userId, pinId, new TravelPinRequest
        {
            Latitude = pin.Latitude,
            Longitude = pin.Longitude,
            PlaceName = pin.PlaceName,
            DateVisited = pin.DateVisited,
            Notes = pin.Notes
        });

        return Ok(new { imageId });
    }

    [HttpDelete("pins/{pinId}/images/{imageId}")]
    public async Task<IActionResult> DeleteImage(string pinId, string imageId)
    {
        var userId = GetUserId();
        var pin = await _travelService.GetPinAsync(userId, pinId);
        if (pin == null)
            return NotFound("Pin not found");

        await _imageService.DeleteImageAsync(pinId, imageId);
        pin.ImageUrls?.Remove(imageId);
        await _travelService.UpdatePinAsync(userId, pinId, new TravelPinRequest
        {
            Latitude = pin.Latitude,
            Longitude = pin.Longitude,
            PlaceName = pin.PlaceName,
            DateVisited = pin.DateVisited,
            Notes = pin.Notes
        });

        return Ok();
    }

    [HttpGet("pins/{pinId}/images/{imageId}")]
    public async Task<IActionResult> GetImage(string pinId, string imageId)
    {
        var result = await _imageService.GetImageAsync(pinId, imageId);
        if (result == null)
            return NotFound();

        return File(result.Value.stream, result.Value.contentType);
    }
}
