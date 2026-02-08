using AIPersonalAssistant.Web.Models;
using System.Text.Json;

namespace AIPersonalAssistant.Web.Services;

public class TravelService : ITravelService
{
    private readonly string _dataDirectory;
    private readonly ILogger<TravelService> _logger;
    private static readonly object _fileLock = new();

    public TravelService(IWebHostEnvironment environment, ILogger<TravelService> logger)
    {
        _dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        _logger = logger;
        
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }
    }

    private string GetUserFilePath(string userId)
    {
        var safeUserId = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(userId))
            .Replace("/", "_").Replace("+", "-");
        return Path.Combine(_dataDirectory, $"travel-pins-{safeUserId}.json");
    }

    private async Task<List<TravelPin>> LoadPinsAsync(string userId)
    {
        var filePath = GetUserFilePath(userId);
        
        if (!File.Exists(filePath))
        {
            return new List<TravelPin>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
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
        var filePath = GetUserFilePath(userId);
        var json = JsonSerializer.Serialize(pins, new JsonSerializerOptions { WriteIndented = true });
        
        lock (_fileLock)
        {
            File.WriteAllText(filePath, json);
        }
        
        await Task.CompletedTask;
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
        if (request.ImageUrls != null)
            pin.ImageUrls = request.ImageUrls;
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
