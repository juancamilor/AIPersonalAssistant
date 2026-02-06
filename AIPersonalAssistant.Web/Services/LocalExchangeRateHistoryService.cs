using AIPersonalAssistant.Web.Models;
using System.Text.Json;

namespace AIPersonalAssistant.Web.Services;

public class LocalExchangeRateHistoryService : IExchangeRateHistoryService
{
    private readonly string _filePath;
    private static readonly object _fileLock = new();

    public LocalExchangeRateHistoryService(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        if (!Directory.Exists(dataDirectory))
        {
            Directory.CreateDirectory(dataDirectory);
        }
        _filePath = Path.Combine(dataDirectory, "rate-history.json");
    }

    private async Task<List<ExchangeRateHistoryEntry>> LoadAsync()
    {
        if (!File.Exists(_filePath))
        {
            return new List<ExchangeRateHistoryEntry>();
        }

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<ExchangeRateHistoryEntry>>(json) ?? new List<ExchangeRateHistoryEntry>();
    }

    private async Task SaveAsync(List<ExchangeRateHistoryEntry> entries)
    {
        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_filePath, json);
    }

    public async Task SaveRateAsync(DateTime date, string fromCurrency, string toCurrency, decimal rate)
    {
        lock (_fileLock)
        {
            var entries = LoadAsync().GetAwaiter().GetResult();

            var exists = entries.Any(e =>
                e.Date.Date == date.Date &&
                e.FromCurrency.Equals(fromCurrency, StringComparison.OrdinalIgnoreCase) &&
                e.ToCurrency.Equals(toCurrency, StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                entries.Add(new ExchangeRateHistoryEntry
                {
                    Date = date.Date,
                    FromCurrency = fromCurrency.ToUpperInvariant(),
                    ToCurrency = toCurrency.ToUpperInvariant(),
                    Rate = rate
                });

                entries = entries.OrderBy(e => e.Date).ToList();
                SaveAsync(entries).GetAwaiter().GetResult();
            }
        }

        await Task.CompletedTask;
    }

    public async Task<List<ExchangeRateHistoryEntry>> GetHistoryAsync(string fromCurrency, string toCurrency)
    {
        var entries = await LoadAsync();
        return entries
            .Where(e =>
                e.FromCurrency.Equals(fromCurrency, StringComparison.OrdinalIgnoreCase) &&
                e.ToCurrency.Equals(toCurrency, StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.Date)
            .ToList();
    }
}
