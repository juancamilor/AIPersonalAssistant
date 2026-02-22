using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIPersonalAssistant.Web.Services;
using AIPersonalAssistant.Web.Models;

namespace AIPersonalAssistant.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MenopauseController : ControllerBase
{
    private readonly IMenopauseService _menopauseService;
    private readonly ILogger<MenopauseController> _logger;

    public MenopauseController(IMenopauseService menopauseService, ILogger<MenopauseController> logger)
    {
        _menopauseService = menopauseService;
        _logger = logger;
    }

    private string GetUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? "anonymous";
    }

    [HttpGet]
    public async Task<IActionResult> GetData()
    {
        var userId = GetUserId();
        var data = await _menopauseService.GetDataAsync(userId);
        return Ok(data);
    }

    [HttpPost("checkins")]
    public async Task<IActionResult> AddCheckIn([FromBody] DailyCheckInRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Invalid request" });
        }

        var userId = GetUserId();
        var checkIn = await _menopauseService.AddCheckInAsync(userId, request);

        return Ok(checkIn);
    }

    [HttpPut("checkins/{id}")]
    public async Task<IActionResult> UpdateCheckIn(string id, [FromBody] DailyCheckInRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Invalid request" });
        }

        var userId = GetUserId();
        var checkIn = await _menopauseService.UpdateCheckInAsync(userId, id, request);

        if (checkIn == null)
        {
            return NotFound(new { error = "Check-in not found" });
        }

        return Ok(checkIn);
    }

    [HttpDelete("checkins/{id}")]
    public async Task<IActionResult> DeleteCheckIn(string id)
    {
        var userId = GetUserId();
        var result = await _menopauseService.DeleteCheckInAsync(userId, id);

        if (!result)
        {
            return NotFound(new { error = "Check-in not found" });
        }

        return NoContent();
    }

    [HttpPost("symptoms")]
    public async Task<IActionResult> AddSymptom([FromBody] SymptomEntryRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Invalid request" });
        }

        var userId = GetUserId();
        var symptom = await _menopauseService.AddSymptomAsync(userId, request);

        return Ok(symptom);
    }

    [HttpPut("symptoms/{id}")]
    public async Task<IActionResult> UpdateSymptom(string id, [FromBody] SymptomEntryRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Invalid request" });
        }

        var userId = GetUserId();
        var symptom = await _menopauseService.UpdateSymptomAsync(userId, id, request);

        if (symptom == null)
        {
            return NotFound(new { error = "Symptom not found" });
        }

        return Ok(symptom);
    }

    [HttpDelete("symptoms/{id}")]
    public async Task<IActionResult> DeleteSymptom(string id)
    {
        var userId = GetUserId();
        var result = await _menopauseService.DeleteSymptomAsync(userId, id);

        if (!result)
        {
            return NotFound(new { error = "Symptom not found" });
        }

        return NoContent();
    }

    [HttpPost("hotflashes")]
    public async Task<IActionResult> AddHotFlash([FromBody] HotFlashEntryRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Invalid request" });
        }

        var userId = GetUserId();
        var hotFlash = await _menopauseService.AddHotFlashAsync(userId, request);

        return Ok(hotFlash);
    }

    [HttpPut("hotflashes/{id}")]
    public async Task<IActionResult> UpdateHotFlash(string id, [FromBody] HotFlashEntryRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Invalid request" });
        }

        var userId = GetUserId();
        var hotFlash = await _menopauseService.UpdateHotFlashAsync(userId, id, request);

        if (hotFlash == null)
        {
            return NotFound(new { error = "Hot flash not found" });
        }

        return Ok(hotFlash);
    }

    [HttpDelete("hotflashes/{id}")]
    public async Task<IActionResult> DeleteHotFlash(string id)
    {
        var userId = GetUserId();
        var result = await _menopauseService.DeleteHotFlashAsync(userId, id);

        if (!result)
        {
            return NotFound(new { error = "Hot flash not found" });
        }

        return NoContent();
    }

    [HttpPost("sleep")]
    public async Task<IActionResult> AddSleepLog([FromBody] SleepLogRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Invalid request" });
        }

        var userId = GetUserId();
        var sleepLog = await _menopauseService.AddSleepLogAsync(userId, request);

        return Ok(sleepLog);
    }

    [HttpPut("sleep/{id}")]
    public async Task<IActionResult> UpdateSleepLog(string id, [FromBody] SleepLogRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Invalid request" });
        }

        var userId = GetUserId();
        var sleepLog = await _menopauseService.UpdateSleepLogAsync(userId, id, request);

        if (sleepLog == null)
        {
            return NotFound(new { error = "Sleep log not found" });
        }

        return Ok(sleepLog);
    }

    [HttpDelete("sleep/{id}")]
    public async Task<IActionResult> DeleteSleepLog(string id)
    {
        var userId = GetUserId();
        var result = await _menopauseService.DeleteSleepLogAsync(userId, id);

        if (!result)
        {
            return NotFound(new { error = "Sleep log not found" });
        }

        return NoContent();
    }

    [HttpGet("shared/{shareToken}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSharedData(string shareToken)
    {
        var data = await _menopauseService.GetByShareTokenAsync(shareToken);

        if (data == null)
        {
            return NotFound(new { error = "Shared data not found" });
        }

        return Ok(new
        {
            Profile = new
            {
                data.Profile.Id,
                data.Profile.ShareToken,
                data.Profile.CreatedAt,
                data.Profile.UpdatedAt
            },
            data.CheckIns,
            data.Symptoms,
            data.HotFlashes,
            data.SleepLogs
        });
    }
}
