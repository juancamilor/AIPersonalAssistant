using AIPersonalAssistant.Web.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text.Json;

namespace AIPersonalAssistant.Web.Services;

public class BlobTravelService : ITravelService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobTravelService> _logger;

    public BlobTravelService(IConfiguration configuration, ILogger<BlobTravelService> logger)
    {
        _logger = logger;
        
        var connectionString = configuration["AzureStorage:ConnectionString"];
        var containerName = configuration["AzureStorage:ContainerName"] ?? "travel-pins";
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("AzureStorage:ConnectionString is not configured");
        }
        
        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        _containerClient.CreateIfNotExists();
    }

    private string GetBlobName(string userId)
    {
        var safeUserId = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(userId))
            .Replace("/", "_").Replace("+", "-");
        return $"user-{safeUserId}.json";
    }

    private async Task<List<TravelPin>> LoadPinsAsync(string userId)
    {
        var blobName = GetBlobName(userId);
        var blobClient = _containerClient.GetBlobClient(blobName);
        
        try
        {
            if (!await blobClient.ExistsAsync())
            {
                return new List<TravelPin>();
            }

            var response = await blobClient.DownloadContentAsync();
            var json = response.Value.Content.ToString();
            return JsonSerializer.Deserialize<List<TravelPin>>(json) ?? new List<TravelPin>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading travel pins for user {UserId}", userId);
            return new List<TravelPin>();
        }
    }

    private async Task SavePinsAsync(string userId, List<TravelPin> pins)
    {
        var blobName = GetBlobName(userId);
        var blobClient = _containerClient.GetBlobClient(blobName);
        
        var json = JsonSerializer.Serialize(pins, new JsonSerializerOptions { WriteIndented = true });
        var content = BinaryData.FromString(json);
        
        await blobClient.UploadAsync(content, overwrite: true);
    }

    public async Task<List<TravelPin>> GetPinsAsync(string userId)
    {
        return await LoadPinsAsync(userId);
    }

    public async Task<TravelPin?> GetPinAsync(string userId, string pinId)
    {
        var pins = await LoadPinsAsync(userId);
        return pins.FirstOrDefault(p => p.Id == pinId);
    }

    public async Task<TravelPin> CreatePinAsync(string userId, TravelPinRequest request)
    {
        var pins = await LoadPinsAsync(userId);
        
        var pin = new TravelPin
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            PlaceName = request.PlaceName,
            DateVisited = request.DateVisited,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        pins.Add(pin);
        await SavePinsAsync(userId, pins);
        
        return pin;
    }

    public async Task<TravelPin?> UpdatePinAsync(string userId, string pinId, TravelPinRequest request)
    {
        var pins = await LoadPinsAsync(userId);
        var pin = pins.FirstOrDefault(p => p.Id == pinId);
        
        if (pin == null)
        {
            return null;
        }
        
        pin.Latitude = request.Latitude;
        pin.Longitude = request.Longitude;
        pin.PlaceName = request.PlaceName;
        pin.DateVisited = request.DateVisited;
        pin.Notes = request.Notes;
        pin.UpdatedAt = DateTime.UtcNow;
        
        await SavePinsAsync(userId, pins);
        
        return pin;
    }

    public async Task<bool> DeletePinAsync(string userId, string pinId)
    {
        var pins = await LoadPinsAsync(userId);
        var pin = pins.FirstOrDefault(p => p.Id == pinId);
        
        if (pin == null)
        {
            return false;
        }
        
        pins.Remove(pin);
        await SavePinsAsync(userId, pins);
        
        return true;
    }
}
