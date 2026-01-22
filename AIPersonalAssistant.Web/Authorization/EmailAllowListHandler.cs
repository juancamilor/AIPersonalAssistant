using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AIPersonalAssistant.Web.Authorization;

public class EmailAllowListHandler : AuthorizationHandler<EmailAllowListRequirement>
{
    private readonly ILogger<EmailAllowListHandler> _logger;

    public EmailAllowListHandler(ILogger<EmailAllowListHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        EmailAllowListRequirement requirement)
    {
        var emailClaim = context.User.FindFirst(ClaimTypes.Email) 
            ?? context.User.FindFirst("preferred_username")
            ?? context.User.FindFirst("email");

        if (emailClaim == null)
        {
            _logger.LogWarning("User authenticated but no email claim found");
            return Task.CompletedTask;
        }

        var userEmail = emailClaim.Value;
        var isAllowed = requirement.AllowedEmails
            .Any(allowed => allowed.Equals(userEmail, StringComparison.OrdinalIgnoreCase));

        if (isAllowed)
        {
            _logger.LogInformation("User {Email} authorized via allow list", userEmail);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("User {Email} not in allow list", userEmail);
        }

        return Task.CompletedTask;
    }
}
