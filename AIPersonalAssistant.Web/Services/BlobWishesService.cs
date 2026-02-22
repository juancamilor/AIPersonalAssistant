using AIPersonalAssistant.Web.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text.Json;

namespace AIPersonalAssistant.Web.Services;

public class BlobWishesService : IWishesService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobWishesService> _logger;

    public BlobWishesService(IConfiguration configuration, ILogger<BlobWishesService> logger)
    {
        _logger = logger;

        var connectionString = configuration["AzureStorage:ConnectionString"];
        var containerName = "wishes";

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
        return $"{safeUserId}.json";
    }

    private async Task<List<WishesDocument>> LoadDocumentsAsync(string userId)
    {
        var blobName = GetBlobName(userId);
        var blobClient = _containerClient.GetBlobClient(blobName);

        try
        {
            if (!await blobClient.ExistsAsync())
            {
                return new List<WishesDocument>();
            }

            var response = await blobClient.DownloadContentAsync();
            var json = response.Value.Content.ToString();
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
        var blobName = GetBlobName(userId);
        var blobClient = _containerClient.GetBlobClient(blobName);

        var json = JsonSerializer.Serialize(documents, new JsonSerializerOptions { WriteIndented = true });
        var content = BinaryData.FromString(json);

        await blobClient.UploadAsync(content, overwrite: true);
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
            await foreach (var blobItem in _containerClient.GetBlobsAsync())
            {
                var blobClient = _containerClient.GetBlobClient(blobItem.Name);
                var response = await blobClient.DownloadContentAsync();
                var json = response.Value.Content.ToString();
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
