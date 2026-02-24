using AIPersonalAssistant.Web.Services;
using System.Security.Claims;

namespace AIPersonalAssistant.Web.Authorization;

public class ToolPermissionMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly Dictionary<string, string> RouteToToolMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "/api/rateexchange", "rate-exchange" },
        { "/api/stock", "stocks" },
        { "/api/travel", "travel-map" },
        { "/api/taxes", "taxes-manager" },
        { "/api/wishes", "wishes" },
        { "/api/recipe", "recipes" },
        { "/api/menopause", "menopause" }
    };

    public ToolPermissionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserManagementService userService)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        
        var matchedTool = RouteToToolMap.FirstOrDefault(kvp => path.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase));
        
        if (matchedTool.Key != null && context.User.Identity?.IsAuthenticated == true)
        {
            var email = context.User.FindFirst("preferred_username")?.Value ??
                        context.User.FindFirst(ClaimTypes.Email)?.Value ??
                        context.User.FindFirst("email")?.Value ?? "";

            if (!string.IsNullOrEmpty(email))
            {
                var permissions = await userService.GetUserPermissionsAsync(email);
                if (!permissions.Contains("*") && !permissions.Contains(matchedTool.Value))
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Access denied: you do not have permission for this tool.");
                    return;
                }
            }
        }

        await _next(context);
    }
}
