using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIPersonalAssistant.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpGet("login")]
    public IActionResult Login(string returnUrl = "/tools.html")
    {
        var redirectUrl = Url.Content($"~{returnUrl}");
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        var callbackUrl = Url.Content("~/index.html");
        return SignOut(
            new AuthenticationProperties { RedirectUri = callbackUrl },
            OpenIdConnectDefaults.AuthenticationScheme,
            "Cookies");
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

        return Ok(new
        {
            email,
            name,
            isAuthenticated = User.Identity?.IsAuthenticated ?? false
        });
    }
}
