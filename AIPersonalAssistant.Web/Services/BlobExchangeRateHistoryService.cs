using AIPersonalAssistant.Web.Models;
using Azure.Storage.Blobs;
using System.Text.Json;

namespace AIPersonalAssistant.Web.Services;

public class BlobExchangeRateHistoryService : IExchangeRateHistoryService
{
    private readonly BlobContainerClient _containerClient;
    private const string BlobName = "rate-history.json";
    private static readonly object _lock = new();

    public BlobExchangeRateHistoryService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureStorage:ConnectionString"];
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("AzureStorage:ConnectionString is not configured");
        }

        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient("rate-history");
        _containerClient.CreateIfNotExists();
    }

    private async Task<List<ExchangeRateHistoryEntry>> LoadAsync()
    {
        var blobClient = _containerClient.GetBlobClient(BlobName);

        try
        {
            if (!await blobClient.ExistsAsync())
            {
                return new List<ExchangeRateHistoryEntry>();
            }

            var response = await blobClient.DownloadContentAsync();
            var json = response.Value.Content.ToString();
            return JsonSerializer.Deserialize<List<ExchangeRateHistoryEntry>>(json) ?? new List<ExchangeRateHistoryEntry>();
        }
        catch (Exception)
        {
            return new List<ExchangeRateHistoryEntry>();
        }
    }

    private async Task SaveAsync(List<ExchangeRateHistoryEntry> entries)
    {
        var blobClient = _containerClient.GetBlobClient(BlobName);
        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        var content = BinaryData.FromString(json);
        await blobClient.UploadAsync(content, overwrite: true);
    }

    public async Task SaveRateAsync(DateTime date, string fromCurrency, string toCurrency, decimal rate)
    {
        lock (_lock)
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
