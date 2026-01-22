using Microsoft.AspNetCore.Authorization;

namespace AIPersonalAssistant.Web.Authorization;

public class EmailAllowListRequirement : IAuthorizationRequirement
{
    public List<string> AllowedEmails { get; }

    public EmailAllowListRequirement(List<string> allowedEmails)
    {
        AllowedEmails = allowedEmails ?? new List<string>();
    }
}
