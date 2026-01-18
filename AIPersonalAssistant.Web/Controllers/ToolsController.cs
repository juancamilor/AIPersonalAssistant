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
            new { Id = 1, Name = "Code Generator", Description = "Generate code snippets and templates", Icon = "🔧" },
            new { Id = 2, Name = "Task Manager", Description = "Manage your daily tasks and reminders", Icon = "📋" },
            new { Id = 3, Name = "Note Keeper", Description = "Keep and organize your notes", Icon = "📝" },
            new { Id = 4, Name = "Calculator", Description = "Perform calculations and conversions", Icon = "🔢" },
            new { Id = 5, Name = "Timer", Description = "Set timers and track time", Icon = "⏱️" },
            new { Id = 6, Name = "Rate Exchange", Description = "Convert currencies with live exchange rates", Icon = "💱" }
        };

        return Ok(tools);
    }
}
