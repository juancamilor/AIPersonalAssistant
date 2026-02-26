using System.Security.Claims;
using AIPersonalAssistant.Web.Authorization;
using AIPersonalAssistant.Web.Controllers;
using AIPersonalAssistant.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace AIPersonalAssistant.Tests;

public class GoogleAuthTests
{
    [Fact]
    public async Task EmailAllowListHandler_AllowsGoogleEmailClaim()
    {
        // Google uses ClaimTypes.Email for the email claim
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, "test@gmail.com"),
            new Claim(ClaimTypes.Name, "Test User")
        };
        var identity = new ClaimsIdentity(claims, "Google");
        var principal = new ClaimsPrincipal(identity);

        var mockLogger = new Mock<ILogger<EmailAllowListHandler>>();
        var mockUserService = new Mock<IUserManagementService>();
        mockUserService.Setup(s => s.IsAllowedUserAsync("test@gmail.com")).ReturnsAsync(true);

        var handler = new EmailAllowListHandler(mockLogger.Object, mockUserService.Object);
        var requirement = new EmailAllowListRequirement(new List<string>());
        var context = new AuthorizationHandlerContext(
            new[] { requirement }, principal, null);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task EmailAllowListHandler_DeniesUnlistedGoogleEmail()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, "unknown@gmail.com")
        };
        var identity = new ClaimsIdentity(claims, "Google");
        var principal = new ClaimsPrincipal(identity);

        var mockLogger = new Mock<ILogger<EmailAllowListHandler>>();
        var mockUserService = new Mock<IUserManagementService>();
        mockUserService.Setup(s => s.IsAllowedUserAsync("unknown@gmail.com")).ReturnsAsync(false);

        var handler = new EmailAllowListHandler(mockLogger.Object, mockUserService.Object);
        var requirement = new EmailAllowListRequirement(new List<string>());
        var context = new AuthorizationHandlerContext(
            new[] { requirement }, principal, null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task EmailAllowListHandler_HandlesNoEmailClaim()
    {
        // User authenticated but with no email claim at all
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "No Email User")
        };
        var identity = new ClaimsIdentity(claims, "Google");
        var principal = new ClaimsPrincipal(identity);

        var mockLogger = new Mock<ILogger<EmailAllowListHandler>>();
        var mockUserService = new Mock<IUserManagementService>();

        var handler = new EmailAllowListHandler(mockLogger.Object, mockUserService.Object);
        var requirement = new EmailAllowListRequirement(new List<string>());
        var context = new AuthorizationHandlerContext(
            new[] { requirement }, principal, null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public void LoginGoogle_RedirectsToLoginPage_WhenGoogleNotConfigured()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var mockUserService = new Mock<IUserManagementService>();
        var controller = new AuthController(config, mockUserService.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = controller.LoginGoogle();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("login.html", redirect.Url);
        Assert.Contains("error=Google", redirect.Url);
    }

    [Fact]
    public void LoginGoogle_ReturnsChallengeResult_WhenGoogleConfigured()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Google:ClientId"] = "test-client-id"
            })
            .Build();
        var mockUserService = new Mock<IUserManagementService>();
        var controller = new AuthController(config, mockUserService.Object);

        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper.Setup(u => u.Content(It.IsAny<string>())).Returns("/tools.html");
        controller.Url = mockUrlHelper.Object;

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = controller.LoginGoogle();

        Assert.IsType<ChallengeResult>(result);
    }
}
