using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using AIPersonalAssistant.Web.Services;

namespace AIPersonalAssistant.Tests;

public class StartupTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public StartupTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Environment", "Development");
        });
    }

    [Fact]
    public void Application_Starts_Successfully()
    {
        // Creating a client boots the entire app - will throw on DI failures
        var client = _factory.CreateClient();
        Assert.NotNull(client);
    }

    [Theory]
    [InlineData(typeof(IExchangeRateService))]
    [InlineData(typeof(IStockService))]
    [InlineData(typeof(ITravelService))]
    [InlineData(typeof(ITravelImageService))]
    [InlineData(typeof(IExchangeRateHistoryService))]
    [InlineData(typeof(IUserManagementService))]
    public void Service_Can_Be_Resolved(Type serviceType)
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService(serviceType);
        Assert.NotNull(service);
    }
}
