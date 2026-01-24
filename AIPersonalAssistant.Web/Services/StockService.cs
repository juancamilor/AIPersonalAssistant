using AIPersonalAssistant.Web.Models;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace AIPersonalAssistant.Web.Services;

public class StockService : IStockService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<StockService> _logger;

    private static readonly Dictionary<string, string> StockNames = new()
    {
        { "MSFT", "Microsoft Corporation" },
        { "META", "Meta Platforms, Inc." },
        { "GOOGL", "Alphabet Inc." }
    };

    public StockService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<StockService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
    }

    public async Task<StockDataResponse> GetStockDataAsync(string symbol, DateTime startDate, DateTime endDate)
    {
        var response = new StockDataResponse
        {
            Symbol = symbol,
            CompanyName = StockNames.GetValueOrDefault(symbol, symbol),
            Success = false
        };

        var cacheKey = $"stock_{symbol}";

        if (!_cache.TryGetValue<Dictionary<DateTime, decimal>>(cacheKey, out var cachedData))
        {
            cachedData = await FetchFromAlphaVantage(symbol);
            
            if (cachedData == null || cachedData.Count == 0)
            {
                response.ErrorMessage = "Failed to fetch stock data from Alpha Vantage";
                return response;
            }

            _cache.Set(cacheKey, cachedData, TimeSpan.FromHours(1));
        }

        var filteredData = cachedData!
            .Where(kvp => kvp.Key >= startDate && kvp.Key <= endDate)
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new StockDataPoint
            {
                Date = kvp.Key,
                Close = kvp.Value
            })
            .ToList();

        response.DataPoints = filteredData;
        response.Success = true;

        return response;
    }

    private async Task<Dictionary<DateTime, decimal>?> FetchFromAlphaVantage(string symbol)
    {
        try
        {
            var apiKey = _configuration["StockAPI:AlphaVantage:ApiKey"];
            _logger.LogInformation("Alpha Vantage API key present: {HasKey}", !string.IsNullOrEmpty(apiKey));
            
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Alpha Vantage API key not configured");
                return null;
            }

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            var url = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={symbol}&outputsize=compact&apikey={apiKey}";
            _logger.LogInformation("Fetching from Alpha Vantage for symbol: {Symbol}", symbol);
            var httpResponse = await client.GetAsync(url);

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Alpha Vantage returned {StatusCode} for {Symbol}", httpResponse.StatusCode, symbol);
                return null;
            }

            var content = await httpResponse.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            if (json.RootElement.TryGetProperty("Error Message", out var errorElement))
            {
                _logger.LogWarning("Alpha Vantage error: {Error}", errorElement.GetString());
                return null;
            }

            if (json.RootElement.TryGetProperty("Note", out var noteElement))
            {
                _logger.LogWarning("Alpha Vantage rate limit: {Note}", noteElement.GetString());
                return null;
            }

            if (!json.RootElement.TryGetProperty("Time Series (Daily)", out var timeSeries))
            {
                _logger.LogWarning("Alpha Vantage response missing Time Series (Daily) for {Symbol}", symbol);
                return null;
            }

            var result = new Dictionary<DateTime, decimal>();

            foreach (var dayProperty in timeSeries.EnumerateObject())
            {
                if (DateTime.TryParse(dayProperty.Name, out var date) &&
                    dayProperty.Value.TryGetProperty("4. close", out var closeElement))
                {
                    if (decimal.TryParse(closeElement.GetString(), out var closePrice))
                    {
                        result[date] = closePrice;
                    }
                }
            }

            _logger.LogInformation("Fetched {Count} data points for {Symbol}", result.Count, symbol);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch from Alpha Vantage for {Symbol}", symbol);
            return null;
        }
    }
}
