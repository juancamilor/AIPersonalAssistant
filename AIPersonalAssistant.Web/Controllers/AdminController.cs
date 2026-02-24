using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIPersonalAssistant.Web.Services;

namespace AIPersonalAssistant.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly IUserManagementService _userService;

    public AdminController(IUserManagementService userService)
    {
        _userService = userService;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpPost("users")]
    public async Task<IActionResult> AddUser([FromBody] AddUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Email))
            return BadRequest("Email is required");

        await _userService.AddUserAsync(request.Email.Trim().ToLowerInvariant());
        return Ok();
    }

    [HttpDelete("users/{email}")]
    public async Task<IActionResult> RemoveUser(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("Email is required");

        await _userService.RemoveUserAsync(email.Trim().ToLowerInvariant());
        return Ok();
    }

    [HttpGet("users/{email}/permissions")]
    public async Task<IActionResult> GetUserPermissions(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("Email is required");

        var permissions = await _userService.GetUserPermissionsAsync(email.Trim().ToLowerInvariant());
        return Ok(permissions);
    }

    [HttpPut("users/{email}/permissions")]
    public async Task<IActionResult> SetUserPermissions(string email, [FromBody] SetPermissionsRequest request)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("Email is required");

        if (request?.Permissions == null)
            return BadRequest("Permissions list is required");

        await _userService.SetUserPermissionsAsync(email.Trim().ToLowerInvariant(), request.Permissions);
        return Ok();
    }

    [HttpGet("tools")]
    public IActionResult GetToolsList()
    {
        var tools = new[]
        {
            new { Id = "rate-exchange", Name = "Rate Exchange" },
            new { Id = "stocks", Name = "Stocks" },
            new { Id = "travel-map", Name = "Travel Map" },
            new { Id = "taxes-manager", Name = "Taxes Manager" },
            new { Id = "chess-trainer", Name = "Chess Trainer" },
            new { Id = "wishes", Name = "Final Wishes" },
            new { Id = "recipes", Name = "Cooking Recipes" },
            new { Id = "menopause", Name = "Menopause Wellness" }
        };
        return Ok(tools);
    }
}

public class AddUserRequest
{
    public string Email { get; set; } = string.Empty;
}

public class SetPermissionsRequest
{
    public List<string> Permissions { get; set; } = new();
}
