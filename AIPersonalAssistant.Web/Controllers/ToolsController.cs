using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIPersonalAssistant.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ToolsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetTools()
    {
        var tools = new[]
        {
            new { Id = 1, Name = "Rate Exchange", Description = "Convert currencies with live exchange rates", Icon = "💱" },
            new { Id = 2, Name = "Stock Tools", Description = "Analyze stock performance with interactive charts", Icon = "📈" }
        };

        return Ok(tools);
    }
}
