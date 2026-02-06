namespace AIPersonalAssistant.Web.Services;

public interface IUserManagementService
{
    Task<List<UserInfo>> GetAllUsersAsync();
    Task AddUserAsync(string email);
    Task RemoveUserAsync(string email);
    Task<bool> IsAllowedUserAsync(string email);
}

public class UserInfo
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}
