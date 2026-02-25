using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIPersonalAssistant.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccessDeniedController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var email = User.Identity?.IsAuthenticated == true 
            ? User.Claims.FirstOrDefault(c => c.Type == "preferred_username" || c.Type == "email")?.Value 
            : "Unknown";

        return Ok(new
        {
            message = "Your email is not authorized to access this application.",
            email = email,
            contactInfo = "Please contact the administrator at juan_camilo_r@hotmail.com to request access."
        });
    }
}
