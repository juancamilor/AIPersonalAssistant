using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIPersonalAssistant.Web.Services;
using AIPersonalAssistant.Web.Models;

namespace AIPersonalAssistant.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WishesController : ControllerBase
{
    private readonly IWishesService _wishesService;
    private readonly ILogger<WishesController> _logger;

    public WishesController(IWishesService wishesService, ILogger<WishesController> logger)
    {
        _wishesService = wishesService;
        _logger = logger;
    }

    private string GetUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? "anonymous";
    }

    [HttpGet]
    public async Task<IActionResult> GetWishes()
    {
        var userId = GetUserId();
        var wishes = await _wishesService.GetByUserIdAsync(userId);
        return Ok(wishes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetWish(string id)
    {
        var userId = GetUserId();
        var wish = await _wishesService.GetByIdAsync(userId, id);

        if (wish == null)
        {
            return NotFound(new { error = "Wish not found" });
        }

        return Ok(wish);
    }

    [HttpPost]
    public async Task<IActionResult> CreateWish([FromBody] WishesRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Invalid request" });
        }

        var userId = GetUserId();
        var wish = await _wishesService.CreateAsync(userId, request);

        return CreatedAtAction(nameof(GetWish), new { id = wish.Id }, wish);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWish(string id, [FromBody] WishesRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Invalid request" });
        }

        var userId = GetUserId();
        var wish = await _wishesService.UpdateAsync(userId, id, request);

        if (wish == null)
        {
            return NotFound(new { error = "Wish not found" });
        }

        return Ok(wish);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWish(string id)
    {
        var userId = GetUserId();
        var result = await _wishesService.DeleteAsync(userId, id);

        if (!result)
        {
            return NotFound(new { error = "Wish not found" });
        }

        return NoContent();
    }

    [HttpGet("shared/{shareToken}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSharedWish(string shareToken)
    {
        var wish = await _wishesService.GetByShareTokenAsync(shareToken);

        if (wish == null)
        {
            return NotFound(new { error = "Shared wish not found" });
        }

        return Ok(new { wish.Title, wish.Content });
    }
}
