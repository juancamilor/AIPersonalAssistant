using AIPersonalAssistant.Web.Models;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace AIPersonalAssistant.Web.Services;

public class ExchangeRateService : IExchangeRateService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ExchangeRateService> _logger;

    public ExchangeRateService(
        IHttpClientFactory httpClientFactory, 
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<ExchangeRateService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<AggregatedRate>> GetExchangeRatesAsync(string fromCurrency, List<string> toCurrencies, DateTime date)
    {
        var result = new List<AggregatedRate>();
        var baseCurrency = ConvertToStandardCode(fromCurrency);

        foreach (var toCurrency in toCurrencies)
        {
            var targetCurrency = ConvertToStandardCode(toCurrency);
            var cacheKey = $"rate_{baseCurrency}_{targetCurrency}_{date:yyyyMMdd}";

            if (_cache.TryGetValue<AggregatedRate>(cacheKey, out var cachedRate))
            {
                result.Add(cachedRate!);
                continue;
            }

            var isHistorical = date.Date < DateTime.Today;
            List<Task<SourceRate>> tasks;

            if (isHistorical)
            {
                // Frankfurter supports USD, CAD, MXN but NOT COP
                var frankfurterSupported = new HashSet<string> { "USD", "CAD", "MXN", "EUR", "GBP", "JPY", "CHF", "AUD", "NZD" };
                
                tasks = new List<Task<SourceRate>>();
                
                if (frankfurterSupported.Contains(targetCurrency) && frankfurterSupported.Contains(baseCurrency))
                {
                    tasks.Add(FetchFromFrankfurter(baseCurrency, targetCurrency, date));
                }
                
                // CurrencyAPI historical works for all currencies including COP
                tasks.Add(FetchHistoricalFromCurrencyApi(baseCurrency, targetCurrency, date));
            }
            else
            {
                tasks = new List<Task<SourceRate>>
                {
                    FetchFromExchangeRateApi(baseCurrency, targetCurrency),
                    FetchFromOpenExchangeRates(baseCurrency, targetCurrency),
                    FetchFromCurrencyApi(baseCurrency, targetCurrency)
                };
            }

            var sourceRates = await Task.WhenAll(tasks);
            var successfulRates = sourceRates.Where(r => r.Success).ToList();

            var aggregatedRate = new AggregatedRate
            {
                ToCurrency = toCurrency,
                Sources = sourceRates.ToList(),
                SuccessfulSourcesCount = successfulRates.Count,
                AverageRate = successfulRates.Any() ? successfulRates.Average(r => r.Rate) : 0,
                CalculationMethod = $"Average of {successfulRates.Count} source{(successfulRates.Count != 1 ? "s" : "")}"
            };

            _cache.Set(cacheKey, aggregatedRate, TimeSpan.FromMinutes(10));
            result.Add(aggregatedRate);
        }

        return result;
    }

    private async Task<SourceRate> FetchFromExchangeRateApi(string fromCurrency, string toCurrency)
    {
        var sourceRate = new SourceRate
        {
            Source = "ExchangeRate-API",
            Timestamp = DateTime.UtcNow,
            Success = false
        };

        try
        {
            var apiKey = _configuration["ExchangeRateAPIs:ExchangeRateApi:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                sourceRate.ErrorMessage = "API key not configured";
                return sourceRate;
            }

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            
            var url = $"https://v6.exchangerate-api.com/v6/{apiKey}/pair/{fromCurrency}/{toCurrency}";
            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(content);
                
                if (json.RootElement.TryGetProperty("conversion_rate", out var rateElement))
                {
                    sourceRate.Rate = rateElement.GetDecimal();
                    sourceRate.Success = true;
                }
            }
            else
            {
                sourceRate.ErrorMessage = $"HTTP {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch from ExchangeRate-API");
            sourceRate.ErrorMessage = ex.Message;
        }

        return sourceRate;
    }

    private async Task<SourceRate> FetchFromOpenExchangeRates(string fromCurrency, string toCurrency)
    {
        var sourceRate = new SourceRate
        {
            Source = "OpenExchangeRates",
            Timestamp = DateTime.UtcNow,
            Success = false
        };

        // Free plan only supports USD as base currency
        if (fromCurrency != "USD")
        {
            sourceRate.ErrorMessage = "Currency not supported";
            _logger.LogInformation("OpenExchangeRates: Free plan only supports USD as base currency. Skipping for {FromCurrency}", fromCurrency);
            return sourceRate;
        }

        try
        {
            var apiKey = _configuration["ExchangeRateAPIs:OpenExchangeRates:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                sourceRate.ErrorMessage = "API key not configured";
                return sourceRate;
            }

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            
            var url = $"https://openexchangerates.org/api/latest.json?app_id={apiKey}&base={fromCurrency}&symbols={toCurrency}";
            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(content);
                
                if (json.RootElement.TryGetProperty("rates", out var ratesElement) &&
                    ratesElement.TryGetProperty(toCurrency, out var rateElement))
                {
                    sourceRate.Rate = rateElement.GetDecimal();
                    sourceRate.Success = true;
                }
            }
            else
            {
                sourceRate.ErrorMessage = $"HTTP {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch from OpenExchangeRates");
            sourceRate.ErrorMessage = ex.Message;
        }

        return sourceRate;
    }

    private async Task<SourceRate> FetchFromCurrencyApi(string fromCurrency, string toCurrency)
    {
        var sourceRate = new SourceRate
        {
            Source = "CurrencyAPI",
            Timestamp = DateTime.UtcNow,
            Success = false
        };

        try
        {
            var apiKey = _configuration["ExchangeRateAPIs:CurrencyApi:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                sourceRate.ErrorMessage = "API key not configured";
                return sourceRate;
            }

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            
            var url = $"https://api.currencyapi.com/v3/latest?apikey={apiKey}&base_currency={fromCurrency}&currencies={toCurrency}";
            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(content);
                
                if (json.RootElement.TryGetProperty("data", out var dataElement) &&
                    dataElement.TryGetProperty(toCurrency, out var currencyElement) &&
                    currencyElement.TryGetProperty("value", out var rateElement))
                {
                    sourceRate.Rate = rateElement.GetDecimal();
                    sourceRate.Success = true;
                }
            }
            else
            {
                sourceRate.ErrorMessage = $"HTTP {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch from CurrencyAPI");
            sourceRate.ErrorMessage = ex.Message;
        }

        return sourceRate;
    }

    private async Task<SourceRate> FetchFromFrankfurter(string fromCurrency, string toCurrency, DateTime date)
    {
        var sourceRate = new SourceRate
        {
            Source = "Frankfurter (Historical)",
            Timestamp = DateTime.UtcNow,
            Success = false
        };

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            
            var dateStr = date.ToString("yyyy-MM-dd");
            var url = $"https://api.frankfurter.app/{dateStr}?from={fromCurrency}&to={toCurrency}";
            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var json = System.Text.Json.JsonDocument.Parse(content);
                
                if (json.RootElement.TryGetProperty("rates", out var ratesElement) &&
                    ratesElement.TryGetProperty(toCurrency, out var rateElement))
                {
                    sourceRate.Rate = rateElement.GetDecimal();
                    sourceRate.Success = true;
                }
            }
            else
            {
                sourceRate.ErrorMessage = $"HTTP {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch from Frankfurter for {From}->{To} on {Date}", fromCurrency, toCurrency, date);
            sourceRate.ErrorMessage = ex.Message;
        }

        return sourceRate;
    }

    private async Task<SourceRate> FetchHistoricalFromCurrencyApi(string fromCurrency, string toCurrency, DateTime date)
    {
        var sourceRate = new SourceRate
        {
            Source = "CurrencyAPI (Historical)",
            Timestamp = DateTime.UtcNow,
            Success = false
        };

        try
        {
            var apiKey = _configuration["ExchangeRateAPIs:CurrencyApi:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                sourceRate.ErrorMessage = "API key not configured";
                return sourceRate;
            }

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            
            var dateStr = date.ToString("yyyy-MM-dd");
            var url = $"https://api.freecurrencyapi.com/v1/historical?apikey={apiKey}&date={dateStr}&base_currency={fromCurrency}&currencies={toCurrency}";
            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var json = System.Text.Json.JsonDocument.Parse(content);
                
                if (json.RootElement.TryGetProperty("data", out var dataElement))
                {
                    // Historical response has date key inside data
                    foreach (var dateEntry in dataElement.EnumerateObject())
                    {
                        if (dateEntry.Value.TryGetProperty(toCurrency, out var rateElement))
                        {
                            sourceRate.Rate = rateElement.GetDecimal();
                            sourceRate.Success = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                sourceRate.ErrorMessage = $"HTTP {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch historical from CurrencyAPI for {From}->{To} on {Date}", fromCurrency, toCurrency, date);
            sourceRate.ErrorMessage = ex.Message;
        }

        return sourceRate;
    }

    private string ConvertToStandardCode(string currencyCode)
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
