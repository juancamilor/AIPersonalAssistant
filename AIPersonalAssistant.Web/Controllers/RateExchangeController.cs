using Microsoft.AspNetCore.Mvc;

namespace AIPersonalAssistant.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RateExchangeController : ControllerBase
{
    [HttpPost("convert")]
    public IActionResult ConvertCurrency([FromBody] ConversionRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.FromCurrency) || request.ToCurrencies == null || !request.ToCurrencies.Any())
        {
            return BadRequest(new { error = "Invalid request. Please provide from currency and at least one to currency." });
        }

        if (request.ToCurrencies.Contains(request.FromCurrency))
        {
            return BadRequest(new { error = "Cannot convert a currency to itself." });
        }

        var rates = GetExchangeRates(request.Date, request.FromCurrency);
        var results = new List<ConversionResult>();

        foreach (var toCurrency in request.ToCurrencies)
        {
            if (rates.TryGetValue(toCurrency, out var rate))
            {
                results.Add(new ConversionResult
                {
                    FromCurrency = request.FromCurrency,
                    ToCurrency = toCurrency,
                    Rate = rate,
                    Date = request.Date
                });
            }
        }

        return Ok(new
        {
            date = request.Date.ToString("yyyy-MM-dd"),
            fromCurrency = request.FromCurrency,
            conversions = results
        });
    }

    private Dictionary<string, decimal> GetExchangeRates(DateTime date, string fromCurrency)
    {
        // Mock exchange rates - In production, this would call a real exchange rate API
        var baseRates = new Dictionary<string, Dictionary<string, decimal>>
        {
            ["US"] = new Dictionary<string, decimal>
            {
                ["CAD"] = 1.35m,
                ["MX"] = 17.25m,
                ["CO"] = 4250.50m
            },
            ["CAD"] = new Dictionary<string, decimal>
            {
                ["US"] = 0.74m,
                ["MX"] = 12.78m,
                ["CO"] = 3148.15m
            },
            ["MX"] = new Dictionary<string, decimal>
            {
                ["US"] = 0.058m,
                ["CAD"] = 0.078m,
                ["CO"] = 246.38m
            },
            ["CO"] = new Dictionary<string, decimal>
            {
                ["US"] = 0.00024m,
                ["CAD"] = 0.00032m,
                ["MX"] = 0.0041m
            }
        };

        // Add slight variation based on date for realism
        var dayOffset = (date.DayOfYear % 10) / 100m;

        if (baseRates.TryGetValue(fromCurrency, out var rates))
        {
            return rates.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value + (kvp.Value * dayOffset)
            );
        }

        return new Dictionary<string, decimal>();
    }
}

public class ConversionRequest
{
    public DateTime Date { get; set; }
    public string FromCurrency { get; set; } = string.Empty;
    public List<string> ToCurrencies { get; set; } = new();
}

public class ConversionResult
{
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateTime Date { get; set; }
}
