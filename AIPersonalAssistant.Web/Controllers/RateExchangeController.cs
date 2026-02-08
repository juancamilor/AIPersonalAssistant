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
    private readonly IExchangeRateHistoryService _historyService;

    public RateExchangeController(IExchangeRateService exchangeRateService, IExchangeRateHistoryService historyService)
    {
        _exchangeRateService = exchangeRateService;
        _historyService = historyService;
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
                Amount = request.Amount,
                Conversions = conversions
            };

            var standardFrom = ConvertToStandardCode(request.FromCurrency);
            foreach (var conversion in conversions)
            {
                if (conversion.AverageRate > 0)
                {
                    var standardTo = ConvertToStandardCode(conversion.ToCurrency);
                    _ = _historyService.SaveRateAsync(request.Date, standardFrom, standardTo, conversion.AverageRate);
                }
            }

            return Ok(response);
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "Failed to fetch exchange rates. Please try again later." });
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] string from, [FromQuery] string to)
    {
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
        {
            return BadRequest(new { error = "Please provide both 'from' and 'to' currency codes." });
        }

        var standardFrom = ConvertToStandardCode(from);
        var standardTo = ConvertToStandardCode(to);
        var history = await _historyService.GetHistoryAsync(standardFrom, standardTo);
        return Ok(new { rates = history });
    }

    [HttpGet("timeseries")]
    public async Task<IActionResult> GetTimeSeries(
        [FromQuery] string from,
        [FromQuery] string to,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
            return BadRequest(new { error = "Please provide 'from' and 'to' currency codes." });
        
        if (startDate >= endDate)
            return BadRequest(new { error = "Start date must be before end date." });
        
        if ((endDate - startDate).TotalDays > 365)
            return BadRequest(new { error = "Maximum date range is 1 year." });
        
        var toCurrencies = to.Split(',').Select(c => c.Trim()).ToList();
        var series = await _exchangeRateService.GetTimeSeriesAsync(from, toCurrencies, startDate, endDate);
        
        var dates = series.Values.FirstOrDefault(v => v.Any())?.Select(p => p.Date).ToList() ?? new List<string>();
        
        return Ok(new { dates, series });
    }

    private static string ConvertToStandardCode(string currencyCode)
    {
        return currencyCode switch
        {
            "US" => "USD",
            "CAD" => "CAD",
            "MX" => "MXN",
            "CO" => "COP",
            _ => currencyCode
        };
    }
}

public class ConversionRequest
{
    public DateTime Date { get; set; }
    public string FromCurrency { get; set; } = string.Empty;
    public decimal Amount { get; set; } = 1;
    public List<string> ToCurrencies { get; set; } = new();
}

