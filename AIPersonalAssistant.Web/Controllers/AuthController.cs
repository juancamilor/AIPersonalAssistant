using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIPersonalAssistant.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login(string returnUrl = "/tools.html")
    {
        var redirectUrl = Url.Content($"~{returnUrl}");
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
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
    public IActionResult GetUser()
    {
        var email = User.FindFirst("preferred_username")?.Value ?? 
                    User.FindFirst("email")?.Value ?? 
                    User.Identity?.Name ?? 
                    "Unknown";
        
        var name = User.FindFirst("name")?.Value ?? email;
        
        var adminEmails = _configuration.GetSection("Authorization:AdminEmails").Get<List<string>>() ?? new List<string>();
        var isAdmin = adminEmails.Any(a => a.Equals(email, StringComparison.OrdinalIgnoreCase));

        return Ok(new
        {
            email,
            name,
            isAuthenticated = User.Identity?.IsAuthenticated ?? false,
            isAdmin
        });
    }
}
