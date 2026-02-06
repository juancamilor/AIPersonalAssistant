using Microsoft.AspNetCore.Authorization;

namespace AIPersonalAssistant.Web.Authorization;

public class AdminRequirement : IAuthorizationRequirement
{
    public List<string> AdminEmails { get; }

    public AdminRequirement(List<string> adminEmails)
    {
        AdminEmails = adminEmails ?? new List<string>();
    }
}
