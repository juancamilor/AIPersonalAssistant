using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AIPersonalAssistant.Web.Authorization;

public class BypassAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public BypassAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim(ClaimTypes.Name, "LocalDev"), new Claim(ClaimTypes.Email, "dev@localhost") };
        var identity = new ClaimsIdentity(claims, "BypassScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "BypassScheme");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
