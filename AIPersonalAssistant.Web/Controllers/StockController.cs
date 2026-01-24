using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIPersonalAssistant.Web.Services;
using AIPersonalAssistant.Web.Models;

namespace AIPersonalAssistant.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StockController : ControllerBase
{
    private readonly IStockService _stockService;

    private static readonly HashSet<string> AllowedSymbols = new() { "MSFT", "META", "GOOGL" };

    public StockController(IStockService stockService)
    {
        _stockService = stockService;
    }

    [HttpPost("data")]
    public async Task<IActionResult> GetStockData([FromBody] StockDataRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Symbol))
        {
            return BadRequest(new { error = "Invalid request. Please provide a stock symbol." });
        }

        if (!AllowedSymbols.Contains(request.Symbol.ToUpper()))
        {
            return BadRequest(new { error = "Invalid stock symbol. Allowed symbols: MSFT, META, GOOGL" });
        }

        if (request.StartDate > request.EndDate)
        {
            return BadRequest(new { error = "Start date must be before end date." });
        }

        if (request.EndDate > DateTime.Today)
        {
            request.EndDate = DateTime.Today;
        }

        try
        {
            var response = await _stockService.GetStockDataAsync(
                request.Symbol.ToUpper(),
                request.StartDate,
                request.EndDate);

            return Ok(response);
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "Failed to fetch stock data. Please try again later." });
        }
    }

    [HttpGet("symbols")]
    public IActionResult GetAvailableSymbols()
    {
        var symbols = new[]
        {
            new { Symbol = "MSFT", Name = "Microsoft Corporation" },
            new { Symbol = "META", Name = "Meta Platforms, Inc." },
            new { Symbol = "GOOGL", Name = "Alphabet Inc." }
        };

        return Ok(symbols);
    }
}
