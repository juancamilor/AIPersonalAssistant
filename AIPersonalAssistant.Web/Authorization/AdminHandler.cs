using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AIPersonalAssistant.Web.Authorization;

public class AdminHandler : AuthorizationHandler<AdminRequirement>
{
    private readonly ILogger<AdminHandler> _logger;

    public AdminHandler(ILogger<AdminHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminRequirement requirement)
    {
        var emailClaim = context.User.FindFirst(ClaimTypes.Email)
            ?? context.User.FindFirst("preferred_username")
            ?? context.User.FindFirst("email");

        if (emailClaim == null)
        {
            return Task.CompletedTask;
        }

        var userEmail = emailClaim.Value;
        var isAdmin = requirement.AdminEmails
            .Any(admin => admin.Equals(userEmail, StringComparison.OrdinalIgnoreCase));

        if (isAdmin)
        {
            _logger.LogInformation("Admin access granted for {Email}", userEmail);
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
