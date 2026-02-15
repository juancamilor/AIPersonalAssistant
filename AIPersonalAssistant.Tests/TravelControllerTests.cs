using AIPersonalAssistant.Web.Controllers;
using AIPersonalAssistant.Web.Models;
using AIPersonalAssistant.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace AIPersonalAssistant.Tests;

public class TravelControllerTests
{
    private readonly Mock<ITravelService> _mockTravelService;
    private readonly Mock<ITravelImageService> _mockImageService;
    private readonly Mock<IWebHostEnvironment> _mockEnv;
    private readonly Mock<ILogger<TravelController>> _mockLogger;
    private readonly TravelController _controller;
    private const string TestUserId = "test-user-123";

    public TravelControllerTests()
    {
        _mockTravelService = new Mock<ITravelService>();
        _mockImageService = new Mock<ITravelImageService>();
        _mockEnv = new Mock<IWebHostEnvironment>();
        _mockLogger = new Mock<ILogger<TravelController>>();
        _mockEnv.Setup(e => e.EnvironmentName).Returns("Development");
        _controller = new TravelController(_mockTravelService.Object, _mockImageService.Object, _mockEnv.Object, _mockLogger.Object);
        
        // Setup user claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task GetPins_ReturnsOkWithPins()
    {
        // Arrange
        var pins = new List<TravelPin>
        {
            new TravelPin { Id = "1", UserId = TestUserId, PlaceName = "Paris", Latitude = 48.8566, Longitude = 2.3522 },
            new TravelPin { Id = "2", UserId = TestUserId, PlaceName = "London", Latitude = 51.5074, Longitude = -0.1278 }
        };
        _mockTravelService.Setup(s => s.GetPinsAsync(TestUserId)).ReturnsAsync(pins);

        // Act
        var result = await _controller.GetPins();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPins = Assert.IsType<List<TravelPin>>(okResult.Value);
        Assert.Equal(2, returnedPins.Count);
    }

    [Fact]
    public async Task GetPin_ReturnsOkWhenPinExists()
    {
        // Arrange
        var pin = new TravelPin { Id = "1", UserId = TestUserId, PlaceName = "Tokyo", Latitude = 35.6762, Longitude = 139.6503 };
        _mockTravelService.Setup(s => s.GetPinAsync(TestUserId, "1")).ReturnsAsync(pin);

        // Act
        var result = await _controller.GetPin("1");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPin = Assert.IsType<TravelPin>(okResult.Value);
        Assert.Equal("Tokyo", returnedPin.PlaceName);
    }

    [Fact]
    public async Task GetPin_ReturnsNotFoundWhenPinDoesNotExist()
    {
        // Arrange
        _mockTravelService.Setup(s => s.GetPinAsync(TestUserId, "999")).ReturnsAsync((TravelPin?)null);

        // Act
        var result = await _controller.GetPin("999");

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreatePin_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new TravelPinRequest { PlaceName = "New York", Latitude = 40.7128, Longitude = -74.0060 };
        var createdPin = new TravelPin { Id = "new-id", UserId = TestUserId, PlaceName = "New York", Latitude = 40.7128, Longitude = -74.0060 };
        _mockTravelService.Setup(s => s.CreatePinAsync(TestUserId, request)).ReturnsAsync(createdPin);

        // Act
        var result = await _controller.CreatePin(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal("GetPin", createdResult.ActionName);
        var returnedPin = Assert.IsType<TravelPin>(createdResult.Value);
        Assert.Equal("New York", returnedPin.PlaceName);
    }

    [Fact]
    public async Task CreatePin_ReturnsBadRequestForInvalidLatitude()
    {
        // Arrange
        var request = new TravelPinRequest { PlaceName = "Invalid", Latitude = 100, Longitude = 0 };

        // Act
        var result = await _controller.CreatePin(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreatePin_ReturnsBadRequestForInvalidLongitude()
    {
        // Arrange
        var request = new TravelPinRequest { PlaceName = "Invalid", Latitude = 0, Longitude = 200 };

        // Act
        var result = await _controller.CreatePin(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdatePin_ReturnsOkWhenSuccessful()
    {
        // Arrange
        var request = new TravelPinRequest { PlaceName = "Updated Place", Latitude = 40.0, Longitude = -70.0 };
        var updatedPin = new TravelPin { Id = "1", UserId = TestUserId, PlaceName = "Updated Place", Latitude = 40.0, Longitude = -70.0 };
        _mockTravelService.Setup(s => s.UpdatePinAsync(TestUserId, "1", request)).ReturnsAsync(updatedPin);

        // Act
        var result = await _controller.UpdatePin("1", request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPin = Assert.IsType<TravelPin>(okResult.Value);
        Assert.Equal("Updated Place", returnedPin.PlaceName);
    }

    [Fact]
    public async Task UpdatePin_ReturnsNotFoundWhenPinDoesNotExist()
    {
        // Arrange
        var request = new TravelPinRequest { PlaceName = "Test", Latitude = 0, Longitude = 0 };
        _mockTravelService.Setup(s => s.UpdatePinAsync(TestUserId, "999", request)).ReturnsAsync((TravelPin?)null);

        // Act
        var result = await _controller.UpdatePin("999", request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeletePin_ReturnsNoContentWhenSuccessful()
    {
        // Arrange
        _mockTravelService.Setup(s => s.DeletePinAsync(TestUserId, "1")).ReturnsAsync(true);

        // Act
        var result = await _controller.DeletePin("1");

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeletePin_ReturnsNotFoundWhenPinDoesNotExist()
    {
        // Arrange
        _mockTravelService.Setup(s => s.DeletePinAsync(TestUserId, "999")).ReturnsAsync(false);

        // Act
        var result = await _controller.DeletePin("999");

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
}
