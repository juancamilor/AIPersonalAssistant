using System.Security.Claims;
using AIPersonalAssistant.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIPersonalAssistant.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ToolsController : ControllerBase
{
    private readonly IUserManagementService _userService;

    public ToolsController(IUserManagementService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTools()
    {
        var email = User.FindFirst("preferred_username")?.Value ??
                    User.FindFirst(ClaimTypes.Email)?.Value ??
                    User.FindFirst("email")?.Value ??
                    User.Identity?.Name ?? "";

        var permissions = await _userService.GetUserPermissionsAsync(email);
        var hasAll = permissions.Contains("*");

        var allTools = new[]
        {
            new { Id = 1, Name = "Rate Exchange", Description = "Convert currencies with live exchange rates", Icon = "💱", PermissionId = "rate-exchange" },
            new { Id = 2, Name = "Stocks", Description = "Analyze stock performance with interactive charts", Icon = "📈", PermissionId = "stocks" },
            new { Id = 3, Name = "Travel Map", Description = "Track and visualize places you've visited around the world", Icon = "🗺️", PermissionId = "travel-map" },
            new { Id = 4, Name = "Taxes Manager", Description = "Upload W2 and stock sales to estimate your federal tax refund", Icon = "🧾", PermissionId = "taxes-manager" },
            new { Id = 5, Name = "Chess Trainer", Description = "Learn chess strategies and practice with an AI opponent", Icon = "♟️", PermissionId = "chess-trainer" },
            new { Id = 6, Name = "Final Wishes", Description = "Store and share your final wishes with loved ones", Icon = "📜", PermissionId = "wishes" },
            new { Id = 7, Name = "Cooking Recipes", Description = "Save, organize, and share your favorite recipes", Icon = "🍳", PermissionId = "recipes" },
            new { Id = 8, Name = "Menopause Wellness", Description = "Track symptoms, mood, and wellness through menopause", Icon = "🌸", PermissionId = "menopause" }
        };

        var filtered = hasAll ? allTools : allTools.Where(t => permissions.Contains(t.PermissionId)).ToArray();
        return Ok(filtered);
    }
}
