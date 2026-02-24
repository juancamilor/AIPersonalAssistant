using System.Text.Json;

namespace AIPersonalAssistant.Web.Services;

public class LocalUserManagementService : IUserManagementService
{
    private readonly string _filePath;
    private readonly List<string> _adminEmails;
    private readonly List<string> _seedEmails;
    private readonly ILogger<LocalUserManagementService> _logger;
    private static readonly object _fileLock = new();

    public LocalUserManagementService(IWebHostEnvironment environment, IConfiguration configuration, ILogger<LocalUserManagementService> logger)
    {
        _logger = logger;
        var dataDir = Path.Combine(environment.ContentRootPath, "App_Data");
        if (!Directory.Exists(dataDir))
            Directory.CreateDirectory(dataDir);

        _filePath = Path.Combine(dataDir, "users.json");
        _adminEmails = configuration.GetSection("Authorization:AdminEmails").Get<List<string>>() ?? new List<string>();
        _seedEmails = configuration.GetSection("Authorization:AllowedEmails").Get<List<string>>() ?? new List<string>();
    }

    private List<UserData> LoadUsers()
    {
        lock (_fileLock)
        {
            if (!File.Exists(_filePath))
            {
                var seed = _seedEmails.Union(_adminEmails, StringComparer.OrdinalIgnoreCase)
                    .Select(e => new UserData { Email = e })
                    .ToList();
                File.WriteAllText(_filePath, JsonSerializer.Serialize(seed, new JsonSerializerOptions { WriteIndented = true }));
                return seed;
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                var trimmed = json.TrimStart();

                // Detect old format: array of strings like ["email@..."]
                if (trimmed.StartsWith("[") && trimmed.Length > 1)
                {
                    var firstContentChar = trimmed.AsSpan(1).TrimStart();
                    if (firstContentChar.Length > 0 && firstContentChar[0] == '"')
                    {
                        var emails = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                        var migrated = emails.Select(e => new UserData { Email = e }).ToList();
                        // Auto-migrate to new format
                        File.WriteAllText(_filePath, JsonSerializer.Serialize(migrated, new JsonSerializerOptions { WriteIndented = true }));
                        _logger.LogInformation("Migrated users.json from string[] to UserData[] format ({Count} users)", migrated.Count);
                        return migrated;
                    }
                }

                return JsonSerializer.Deserialize<List<UserData>>(json) ?? new List<UserData>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users.json");
                return new List<UserData>();
            }
        }
    }

    private void SaveUsers(List<UserData> users)
    {
        lock (_fileLock)
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    public Task<List<UserInfo>> GetAllUsersAsync()
    {
        var users = LoadUsers();
        var result = users.Select(u => new UserInfo
        {
            Email = u.Email,
            Role = _adminEmails.Any(a => a.Equals(u.Email, StringComparison.OrdinalIgnoreCase)) ? "Admin" : "User",
            Permissions = u.Permissions
        }).ToList();

        return Task.FromResult(result);
    }

    public Task AddUserAsync(string email)
    {
        var users = LoadUsers();
        if (!users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
        {
            users.Add(new UserData { Email = email });
            SaveUsers(users);
        }
        return Task.CompletedTask;
    }

    public Task RemoveUserAsync(string email)
    {
        if (_adminEmails.Any(a => a.Equals(email, StringComparison.OrdinalIgnoreCase)))
            return Task.CompletedTask;

        var users = LoadUsers();
        users.RemoveAll(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        SaveUsers(users);
        return Task.CompletedTask;
    }

    public Task<bool> IsAllowedUserAsync(string email)
    {
        if (_adminEmails.Any(a => a.Equals(email, StringComparison.OrdinalIgnoreCase)))
            return Task.FromResult(true);

        var users = LoadUsers();
        return Task.FromResult(users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<List<string>> GetUserPermissionsAsync(string email)
    {
        if (_adminEmails.Any(a => a.Equals(email, StringComparison.OrdinalIgnoreCase)))
            return Task.FromResult(new List<string> { "*" });

        var users = LoadUsers();
        var user = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user?.Permissions ?? new List<string>());
    }

    public Task SetUserPermissionsAsync(string email, List<string> permissions)
    {
        var users = LoadUsers();
        var user = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (user != null)
        {
            user.Permissions = permissions;
            SaveUsers(users);
        }
        return Task.CompletedTask;
    }
}
