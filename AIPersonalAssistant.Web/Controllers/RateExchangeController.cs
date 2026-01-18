using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIPersonalAssistant.Web.Services;
using AIPersonalAssistant.Web.Models;

namespace AIPersonalAssistant.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RateExchangeController : ControllerBase
{
    private readonly IExchangeRateService _exchangeRateService;

    public RateExchangeController(IExchangeRateService exchangeRateService)
    {
        _exchangeRateService = exchangeRateService;
    }

    [HttpPost("convert")]
    public async Task<IActionResult> ConvertCurrency([FromBody] ConversionRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.FromCurrency) || request.ToCurrencies == null || !request.ToCurrencies.Any())
        {
            return BadRequest(new { error = "Invalid request. Please provide from currency and at least one to currency." });
        }

        if (request.ToCurrencies.Contains(request.FromCurrency))
        {
            return BadRequest(new { error = "Cannot convert a currency to itself." });
        }

        try
        {
            var conversions = await _exchangeRateService.GetExchangeRatesAsync(
                request.FromCurrency, 
                request.ToCurrencies, 
                request.Date);

            var response = new ExchangeRateResponse
            {
                Date = request.Date.ToString("yyyy-MM-dd"),
                FromCurrency = request.FromCurrency,
                Conversions = conversions
            };

            return Ok(response);
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "Failed to fetch exchange rates. Please try again later." });
        }
    }
}

public class ConversionRequest
{
    public DateTime Date { get; set; }
    public string FromCurrency { get; set; } = string.Empty;
    public List<string> ToCurrencies { get; set; } = new();
}

