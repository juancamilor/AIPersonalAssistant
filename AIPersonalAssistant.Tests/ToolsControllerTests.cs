using System.Security.Claims;
using AIPersonalAssistant.Web.Controllers;
using AIPersonalAssistant.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Reflection;

namespace AIPersonalAssistant.Tests;

public class ToolsControllerTests
{
    private readonly ToolsController _controller;

    public ToolsControllerTests()
    {
        var mockUserService = new Mock<IUserManagementService>();
        mockUserService
            .Setup(s => s.GetUserPermissionsAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<string> { "*" });

        _controller = new ToolsController(mockUserService.Object);

        var claims = new List<Claim> { new Claim("preferred_username", "test@example.com") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private static object? GetPropertyValue(object obj, string propertyName)
    {
        return obj.GetType().GetProperty(propertyName)?.GetValue(obj);
    }

    [Fact]
    public async Task GetTools_ReturnsOkResult()
    {
        // Act
        var result = await _controller.GetTools();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetTools_ReturnsSevenTools()
    {
        // Act
        var result = await _controller.GetTools() as OkObjectResult;
        var tools = result?.Value as Array;

        // Assert
        Assert.NotNull(tools);
        Assert.Equal(8, tools.Length);
    }

    [Fact]
    public async Task GetTools_AllToolsHaveRequiredProperties()
    {
        // Act
        var result = await _controller.GetTools() as OkObjectResult;
        var tools = result?.Value as Array;

        // Assert
        Assert.NotNull(tools);
        foreach (var tool in tools)
        {
            Assert.NotNull(tool);
            Assert.NotNull(GetPropertyValue(tool, "Id"));
            Assert.NotNull(GetPropertyValue(tool, "Name"));
            Assert.NotNull(GetPropertyValue(tool, "Description"));
            Assert.NotNull(GetPropertyValue(tool, "Icon"));
        }
    }

    [Fact]
    public async Task GetTools_FirstToolIsRateExchange()
    {
        // Act
        var result = await _controller.GetTools() as OkObjectResult;
        var tools = result?.Value as Array;

        // Assert
        Assert.NotNull(tools);
        var firstTool = tools.GetValue(0);
        Assert.NotNull(firstTool);
        Assert.Equal(1, GetPropertyValue(firstTool, "Id"));
        Assert.Equal("Rate Exchange", GetPropertyValue(firstTool, "Name"));
        Assert.Equal("Convert currencies with live exchange rates", GetPropertyValue(firstTool, "Description"));
        Assert.Equal("💱", GetPropertyValue(firstTool, "Icon"));
    }



    [Fact]
    public async Task GetTools_AllToolsHaveUniqueIds()
    {
        // Act
        var result = await _controller.GetTools() as OkObjectResult;
        var tools = result?.Value as Array;

        // Assert
        Assert.NotNull(tools);
        var ids = new List<int>();
        foreach (var tool in tools)
        {
            var id = GetPropertyValue(tool!, "Id");
            Assert.NotNull(id);
            ids.Add((int)id);
        }
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public async Task GetTools_AllToolNamesAreNotEmpty()
    {
        // Act
        var result = await _controller.GetTools() as OkObjectResult;
        var tools = result?.Value as Array;

        // Assert
        Assert.NotNull(tools);
        foreach (var tool in tools)
        {
            var name = GetPropertyValue(tool!, "Name");
            Assert.NotNull(name);
            Assert.False(string.IsNullOrWhiteSpace(name.ToString()));
        }
    }
}
