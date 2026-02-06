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

    private List<string> LoadEmails()
    {
        lock (_fileLock)
        {
            if (!File.Exists(_filePath))
            {
                var seed = _seedEmails.Union(_adminEmails, StringComparer.OrdinalIgnoreCase).ToList();
                File.WriteAllText(_filePath, JsonSerializer.Serialize(seed, new JsonSerializerOptions { WriteIndented = true }));
                return seed;
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users.json");
                return new List<string>();
            }
        }
    }

    private void SaveEmails(List<string> emails)
    {
        lock (_fileLock)
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(emails, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    public Task<List<UserInfo>> GetAllUsersAsync()
    {
        var emails = LoadEmails();
        var users = emails.Select(e => new UserInfo
        {
            Email = e,
            Role = _adminEmails.Any(a => a.Equals(e, StringComparison.OrdinalIgnoreCase)) ? "Admin" : "User"
        }).ToList();

        return Task.FromResult(users);
    }

    public Task AddUserAsync(string email)
    {
        var emails = LoadEmails();
        if (!emails.Any(e => e.Equals(email, StringComparison.OrdinalIgnoreCase)))
        {
            emails.Add(email);
            SaveEmails(emails);
        }
        return Task.CompletedTask;
    }

    public Task RemoveUserAsync(string email)
    {
        if (_adminEmails.Any(a => a.Equals(email, StringComparison.OrdinalIgnoreCase)))
            return Task.CompletedTask;

        var emails = LoadEmails();
        emails.RemoveAll(e => e.Equals(email, StringComparison.OrdinalIgnoreCase));
        SaveEmails(emails);
        return Task.CompletedTask;
    }

    public Task<bool> IsAllowedUserAsync(string email)
    {
        if (_adminEmails.Any(a => a.Equals(email, StringComparison.OrdinalIgnoreCase)))
            return Task.FromResult(true);

        var emails = LoadEmails();
        return Task.FromResult(emails.Any(e => e.Equals(email, StringComparison.OrdinalIgnoreCase)));
    }
}
