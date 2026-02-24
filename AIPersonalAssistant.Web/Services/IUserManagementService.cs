namespace AIPersonalAssistant.Web.Services;

public interface IUserManagementService
{
    Task<List<UserInfo>> GetAllUsersAsync();
    Task AddUserAsync(string email);
    Task RemoveUserAsync(string email);
    Task<bool> IsAllowedUserAsync(string email);
    Task<List<string>> GetUserPermissionsAsync(string email);
    Task SetUserPermissionsAsync(string email, List<string> permissions);
}

public class UserInfo
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public List<string> Permissions { get; set; } = new() { "*" };
}

public class UserData
{
    public string Email { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new() { "*" };
}
