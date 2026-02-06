using Azure.Storage.Blobs;
using System.Text.Json;

namespace AIPersonalAssistant.Web.Services;

public class BlobUserManagementService : IUserManagementService
{
    private readonly BlobContainerClient _containerClient;
    private readonly List<string> _adminEmails;
    private readonly List<string> _seedEmails;
    private readonly ILogger<BlobUserManagementService> _logger;
    private const string BlobName = "users.json";

    public BlobUserManagementService(IConfiguration configuration, ILogger<BlobUserManagementService> logger)
    {
        _logger = logger;
        _adminEmails = configuration.GetSection("Authorization:AdminEmails").Get<List<string>>() ?? new List<string>();
        _seedEmails = configuration.GetSection("Authorization:AllowedEmails").Get<List<string>>() ?? new List<string>();

        var connectionString = configuration["AzureStorage:ConnectionString"];
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("AzureStorage:ConnectionString is not configured");

        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient("user-management");
        _containerClient.CreateIfNotExists();
    }

    private async Task<List<string>> LoadEmailsAsync()
    {
        var blobClient = _containerClient.GetBlobClient(BlobName);
        try
        {
            if (!await blobClient.ExistsAsync())
            {
                var seed = _seedEmails.Union(_adminEmails, StringComparer.OrdinalIgnoreCase).ToList();
                await SaveEmailsAsync(seed);
                return seed;
            }

            var response = await blobClient.DownloadContentAsync();
            var json = response.Value.Content.ToString();
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading users from blob storage");
            return new List<string>();
        }
    }

    private async Task SaveEmailsAsync(List<string> emails)
    {
        var blobClient = _containerClient.GetBlobClient(BlobName);
        var json = JsonSerializer.Serialize(emails, new JsonSerializerOptions { WriteIndented = true });
        await blobClient.UploadAsync(BinaryData.FromString(json), overwrite: true);
    }

    public async Task<List<UserInfo>> GetAllUsersAsync()
    {
        var emails = await LoadEmailsAsync();
        return emails.Select(e => new UserInfo
        {
            Email = e,
            Role = _adminEmails.Any(a => a.Equals(e, StringComparison.OrdinalIgnoreCase)) ? "Admin" : "User"
        }).ToList();
    }

    public async Task AddUserAsync(string email)
    {
        var emails = await LoadEmailsAsync();
        if (!emails.Any(e => e.Equals(email, StringComparison.OrdinalIgnoreCase)))
        {
            emails.Add(email);
            await SaveEmailsAsync(emails);
        }
    }

    public async Task RemoveUserAsync(string email)
    {
        if (_adminEmails.Any(a => a.Equals(email, StringComparison.OrdinalIgnoreCase)))
            return;

        var emails = await LoadEmailsAsync();
        emails.RemoveAll(e => e.Equals(email, StringComparison.OrdinalIgnoreCase));
        await SaveEmailsAsync(emails);
    }

    public async Task<bool> IsAllowedUserAsync(string email)
    {
        if (_adminEmails.Any(a => a.Equals(email, StringComparison.OrdinalIgnoreCase)))
            return true;

        var emails = await LoadEmailsAsync();
        return emails.Any(e => e.Equals(email, StringComparison.OrdinalIgnoreCase));
    }
}
