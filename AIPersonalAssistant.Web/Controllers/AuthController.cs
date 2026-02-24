using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIPersonalAssistant.Web.Services;

namespace AIPersonalAssistant.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IUserManagementService _userService;

    public AuthController(IConfiguration configuration, IUserManagementService userService)
    {
        _configuration = configuration;
        _userService = userService;
    }

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login(string returnUrl = "/tools.html")
    {
        var redirectUrl = Url.Content($"~{returnUrl}");
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet("login-google")]
    [AllowAnonymous]
    public IActionResult LoginGoogle(string returnUrl = "/tools.html")
    {
        var redirectUrl = Url.Content($"~{returnUrl}");
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, "Google");
    }

    [HttpGet("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("Cookies");
        return Redirect("~/login.html");
    }

    [HttpGet("user")]
    [Authorize]
    public async Task<IActionResult> GetUser()
    {
        var email = User.FindFirst("preferred_username")?.Value ?? 
                    User.FindFirst(ClaimTypes.Email)?.Value ??
                    User.FindFirst("email")?.Value ?? 
                    User.Identity?.Name ?? 
                    "Unknown";
        
        var name = User.FindFirst("name")?.Value ?? email;
        
        var adminEmails = _configuration.GetSection("Authorization:AdminEmails").Get<List<string>>() ?? new List<string>();
        var isAdmin = adminEmails.Any(a => a.Equals(email, StringComparison.OrdinalIgnoreCase));

        var permissions = await _userService.GetUserPermissionsAsync(email);

        return Ok(new
        {
            email,
            name,
            isAuthenticated = User.Identity?.IsAuthenticated ?? false,
            isAdmin,
            permissions
        });
    }
}
