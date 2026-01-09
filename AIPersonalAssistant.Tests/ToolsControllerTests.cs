using AIPersonalAssistant.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace AIPersonalAssistant.Tests;

public class ToolsControllerTests
{
    private readonly ToolsController _controller;

    public ToolsControllerTests()
    {
        _controller = new ToolsController();
    }

    private static object? GetPropertyValue(object obj, string propertyName)
    {
        return obj.GetType().GetProperty(propertyName)?.GetValue(obj);
    }

    [Fact]
    public void GetTools_ReturnsOkResult()
    {
        // Act
        var result = _controller.GetTools();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void GetTools_ReturnsFiveTools()
    {
        // Act
        var result = _controller.GetTools() as OkObjectResult;
        var tools = result?.Value as Array;

        // Assert
        Assert.NotNull(tools);
        Assert.Equal(5, tools.Length);
    }

    [Fact]
    public void GetTools_AllToolsHaveRequiredProperties()
    {
        // Act
        var result = _controller.GetTools() as OkObjectResult;
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
    public void GetTools_FirstToolIsCodeGenerator()
    {
        // Act
        var result = _controller.GetTools() as OkObjectResult;
        var tools = result?.Value as Array;

        // Assert
        Assert.NotNull(tools);
        var firstTool = tools.GetValue(0);
        Assert.NotNull(firstTool);
        Assert.Equal(1, GetPropertyValue(firstTool, "Id"));
        Assert.Equal("Code Generator", GetPropertyValue(firstTool, "Name"));
        Assert.Equal("Generate code snippets and templates", GetPropertyValue(firstTool, "Description"));
        Assert.Equal("🔧", GetPropertyValue(firstTool, "Icon"));
    }

    [Fact]
    public void GetTools_LastToolIsTimer()
    {
        // Act
        var result = _controller.GetTools() as OkObjectResult;
        var tools = result?.Value as Array;

        // Assert
        Assert.NotNull(tools);
        var lastTool = tools.GetValue(4);
        Assert.NotNull(lastTool);
        Assert.Equal(5, GetPropertyValue(lastTool, "Id"));
        Assert.Equal("Timer", GetPropertyValue(lastTool, "Name"));
        Assert.Equal("Set timers and track time", GetPropertyValue(lastTool, "Description"));
        Assert.Equal("⏱️", GetPropertyValue(lastTool, "Icon"));
    }

    [Fact]
    public void GetTools_AllToolsHaveUniqueIds()
    {
        // Act
        var result = _controller.GetTools() as OkObjectResult;
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
    public void GetTools_AllToolNamesAreNotEmpty()
    {
        // Act
        var result = _controller.GetTools() as OkObjectResult;
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
