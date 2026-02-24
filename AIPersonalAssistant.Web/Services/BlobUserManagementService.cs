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

    private async Task<List<UserData>> LoadUsersAsync()
    {
        var blobClient = _containerClient.GetBlobClient(BlobName);
        try
        {
            if (!await blobClient.ExistsAsync())
            {
                var seed = _seedEmails.Union(_adminEmails, StringComparer.OrdinalIgnoreCase)
                    .Select(e => new UserData { Email = e, Permissions = new() { "*" } })
                    .ToList();
                await SaveUsersAsync(seed);
                return seed;
            }

            var response = await blobClient.DownloadContentAsync();
            var json = response.Value.Content.ToString();

            // Try new format first, fall back to old List<string> format
            try
            {
                return JsonSerializer.Deserialize<List<UserData>>(json) ?? new List<UserData>();
            }
            catch (JsonException)
            {
                var emails = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                var migrated = emails.Select(e => new UserData { Email = e, Permissions = new() { "*" } }).ToList();
                await SaveUsersAsync(migrated);
                return migrated;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading users from blob storage");
            return new List<UserData>();
        }
    }

    private async Task SaveUsersAsync(List<UserData> users)
    {
        var blobClient = _containerClient.GetBlobClient(BlobName);
        var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
        await blobClient.UploadAsync(BinaryData.FromString(json), overwrite: true);
    }

    public async Task<List<UserInfo>> GetAllUsersAsync()
    {
        var users = await LoadUsersAsync();
        return users.Select(u => new UserInfo
        {
            Email = u.Email,
            Role = _adminEmails.Any(a => a.Equals(u.Email, StringComparison.OrdinalIgnoreCase)) ? "Admin" : "User",
            Permissions = u.Permissions
        }).ToList();
    }

    public async Task AddUserAsync(string email)
    {
        var users = await LoadUsersAsync();
        if (!users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
        {
            users.Add(new UserData { Email = email, Permissions = new() { "*" } });
            await SaveUsersAsync(users);
        }
    }

    public async Task RemoveUserAsync(string email)
    {
        if (_adminEmails.Any(a => a.Equals(email, StringComparison.OrdinalIgnoreCase)))
            return;

        var users = await LoadUsersAsync();
        users.RemoveAll(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        await SaveUsersAsync(users);
    }

    public async Task<bool> IsAllowedUserAsync(string email)
    {
        if (_adminEmails.Any(a => a.Equals(email, StringComparison.OrdinalIgnoreCase)))
            return true;

        var users = await LoadUsersAsync();
        return users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<string>> GetUserPermissionsAsync(string email)
    {
        if (_adminEmails.Any(a => a.Equals(email, StringComparison.OrdinalIgnoreCase)))
            return new List<string> { "*" };

        var users = await LoadUsersAsync();
        var user = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        return user?.Permissions ?? new List<string>();
    }

    public async Task SetUserPermissionsAsync(string email, List<string> permissions)
    {
        var users = await LoadUsersAsync();
        var user = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (user != null)
        {
            user.Permissions = permissions;
            await SaveUsersAsync(users);
        }
    }
}
