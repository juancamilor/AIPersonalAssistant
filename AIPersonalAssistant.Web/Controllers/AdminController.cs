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
}

public class AddUserRequest
{
    public string Email { get; set; } = string.Empty;
}
