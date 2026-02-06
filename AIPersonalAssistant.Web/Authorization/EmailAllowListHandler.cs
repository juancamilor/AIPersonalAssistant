using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AIPersonalAssistant.Web.Services;

namespace AIPersonalAssistant.Web.Authorization;

public class EmailAllowListHandler : AuthorizationHandler<EmailAllowListRequirement>
{
    private readonly ILogger<EmailAllowListHandler> _logger;
    private readonly IUserManagementService _userManagementService;

    public EmailAllowListHandler(ILogger<EmailAllowListHandler> logger, IUserManagementService userManagementService)
    {
        _logger = logger;
        _userManagementService = userManagementService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        EmailAllowListRequirement requirement)
    {
        var emailClaim = context.User.FindFirst(ClaimTypes.Email) 
            ?? context.User.FindFirst("preferred_username")
            ?? context.User.FindFirst("email");

        if (emailClaim == null)
        {
            _logger.LogWarning("User authenticated but no email claim found");
            return;
        }

        var userEmail = emailClaim.Value;
        var isAllowed = await _userManagementService.IsAllowedUserAsync(userEmail);

        if (isAllowed)
        {
            _logger.LogInformation("User {Email} authorized via allow list", userEmail);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("User {Email} not in allow list", userEmail);
        }
    }
}
