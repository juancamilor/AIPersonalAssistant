using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIPersonalAssistant.Web.Services;

namespace AIPersonalAssistant.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public HealthController(IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    [HttpGet]
    public IActionResult GetHealth()
    {
        var checks = new Dictionary<string, object>();
        var allHealthy = true;

        // Check API keys
        var keys = new Dictionary<string, string>
        {
            { "AzureAd:ClientId", _configuration["AzureAd:ClientId"] ?? "" },
            { "AzureAd:ClientSecret", _configuration["AzureAd:ClientSecret"] ?? "" },
            { "AzureAd:TenantId", _configuration["AzureAd:TenantId"] ?? "" },
            { "ExchangeRateAPIs:ExchangeRateApi:ApiKey", _configuration["ExchangeRateAPIs:ExchangeRateApi:ApiKey"] ?? "" },
            { "ExchangeRateAPIs:OpenExchangeRates:ApiKey", _configuration["ExchangeRateAPIs:OpenExchangeRates:ApiKey"] ?? "" },
            { "ExchangeRateAPIs:CurrencyApi:ApiKey", _configuration["ExchangeRateAPIs:CurrencyApi:ApiKey"] ?? "" },
            { "StockAPI:AlphaVantage:ApiKey", _configuration["StockAPI:AlphaVantage:ApiKey"] ?? "" },
        };

        var keyChecks = new Dictionary<string, string>();
        foreach (var key in keys)
        {
            var status = string.IsNullOrEmpty(key.Value) ? "missing" : "configured";
            keyChecks[key.Key] = status;
            if (status == "missing") allHealthy = false;
        }
        checks["apiKeys"] = keyChecks;

        // Check DI services
        var services = new Dictionary<string, string>();
        var serviceTypes = new (string Name, Type Type)[]
        {
            ("IExchangeRateService", typeof(IExchangeRateService)),
            ("IStockService", typeof(IStockService)),
            ("ITravelService", typeof(ITravelService)),
            ("ITravelImageService", typeof(ITravelImageService)),
            ("IExchangeRateHistoryService", typeof(IExchangeRateHistoryService)),
            ("IUserManagementService", typeof(IUserManagementService)),
        };

        using var scope = _serviceProvider.CreateScope();
        foreach (var (name, type) in serviceTypes)
        {
            try
            {
                var service = scope.ServiceProvider.GetRequiredService(type);
                services[name] = "ok";
            }
            catch (Exception ex)
            {
                services[name] = $"failed: {ex.Message}";
                allHealthy = false;
            }
        }
        checks["services"] = services;

        // Check storage
        var storageConnection = _configuration["AzureStorage:ConnectionString"];
        checks["storage"] = new
        {
            type = string.IsNullOrEmpty(storageConnection) ? "local" : "azure-blob",
            status = "ok"
        };

        checks["status"] = allHealthy ? "healthy" : "unhealthy";
        checks["timestamp"] = DateTime.UtcNow.ToString("O");

        return allHealthy ? Ok(checks) : StatusCode(503, checks);
    }
}
