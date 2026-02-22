using AIPersonalAssistant.Web.Models;
using System.Text.Json;

namespace AIPersonalAssistant.Web.Services;

public class LocalWishesService : IWishesService
{
    private readonly string _dataDirectory;
    private readonly ILogger<LocalWishesService> _logger;
    private static readonly object _fileLock = new();

    public LocalWishesService(IWebHostEnvironment environment, ILogger<LocalWishesService> logger)
    {
        _dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "wishes");
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
        return Path.Combine(_dataDirectory, $"{safeUserId}.json");
    }

    private async Task<List<WishesDocument>> LoadDocumentsAsync(string userId)
    {
        var filePath = GetUserFilePath(userId);

        if (!File.Exists(filePath))
        {
            return new List<WishesDocument>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<WishesDocument>>(json) ?? new List<WishesDocument>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading wishes for user {UserId}", userId);
            return new List<WishesDocument>();
        }
    }

    private async Task SaveDocumentsAsync(string userId, List<WishesDocument> documents)
    {
        var filePath = GetUserFilePath(userId);
        var json = JsonSerializer.Serialize(documents, new JsonSerializerOptions { WriteIndented = true });

        lock (_fileLock)
        {
            File.WriteAllText(filePath, json);
        }

        await Task.CompletedTask;
    }

    public async Task<List<WishesDocument>> GetByUserIdAsync(string userId)
    {
        return await LoadDocumentsAsync(userId);
    }

    public async Task<WishesDocument?> GetByIdAsync(string userId, string id)
    {
        var documents = await LoadDocumentsAsync(userId);
        return documents.FirstOrDefault(d => d.Id == id);
    }

    public async Task<WishesDocument?> GetByShareTokenAsync(string shareToken)
    {
        try
        {
            var files = Directory.GetFiles(_dataDirectory, "*.json");
            foreach (var file in files)
            {
                var json = await File.ReadAllTextAsync(file);
                var documents = JsonSerializer.Deserialize<List<WishesDocument>>(json);
                if (documents != null)
                {
                    var match = documents.FirstOrDefault(d => d.ShareToken == shareToken);
                    if (match != null)
                    {
                        return match;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching wishes by share token");
        }

        return null;
    }

    public async Task<WishesDocument> CreateAsync(string userId, WishesRequest request)
    {
        var documents = await LoadDocumentsAsync(userId);

        var document = new WishesDocument
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Title = request.Title,
            Content = request.Content,
            ShareToken = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        documents.Add(document);
        await SaveDocumentsAsync(userId, documents);

        return document;
    }

    public async Task<WishesDocument?> UpdateAsync(string userId, string id, WishesRequest request)
    {
        var documents = await LoadDocumentsAsync(userId);
        var document = documents.FirstOrDefault(d => d.Id == id);

        if (document == null)
        {
            return null;
        }

        document.Title = request.Title;
        document.Content = request.Content;
        document.UpdatedAt = DateTime.UtcNow;

        await SaveDocumentsAsync(userId, documents);

        return document;
    }

    public async Task<bool> DeleteAsync(string userId, string id)
    {
        var documents = await LoadDocumentsAsync(userId);
        var document = documents.FirstOrDefault(d => d.Id == id);

        if (document == null)
        {
            return false;
        }

        documents.Remove(document);
        await SaveDocumentsAsync(userId, documents);

        return true;
    }
}
