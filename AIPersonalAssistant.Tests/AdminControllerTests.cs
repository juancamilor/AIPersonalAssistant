using AIPersonalAssistant.Web.Controllers;
using AIPersonalAssistant.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AIPersonalAssistant.Tests;

public class AdminControllerTests
{
    private readonly Mock<IUserManagementService> _mockUserService;
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        _mockUserService = new Mock<IUserManagementService>();
        _controller = new AdminController(_mockUserService.Object);
    }

    [Fact]
    public void GetToolsList_ReturnsOkResult()
    {
        var result = _controller.GetToolsList();
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void GetToolsList_ReturnsEightTools()
    {
        var result = _controller.GetToolsList() as OkObjectResult;
        var tools = result?.Value as Array;
        Assert.NotNull(tools);
        Assert.Equal(8, tools.Length);
    }

    [Fact]
    public void GetToolsList_ToolsHaveIdAndNameProperties()
    {
        var result = _controller.GetToolsList() as OkObjectResult;
        var tools = result?.Value as Array;
        Assert.NotNull(tools);
        foreach (var tool in tools)
        {
            Assert.NotNull(tool);
            var idProp = tool.GetType().GetProperty("Id");
            var nameProp = tool.GetType().GetProperty("Name");
            Assert.NotNull(idProp);
            Assert.NotNull(nameProp);
            var id = idProp.GetValue(tool) as string;
            var name = nameProp.GetValue(tool) as string;
            Assert.False(string.IsNullOrEmpty(id), "Tool Id should not be empty");
            Assert.False(string.IsNullOrEmpty(name), "Tool Name should not be empty");
        }
    }

    [Fact]
    public void GetToolsList_ToolIdsAreKebabCase()
    {
        var result = _controller.GetToolsList() as OkObjectResult;
        var tools = result?.Value as Array;
        Assert.NotNull(tools);
        foreach (var tool in tools)
        {
            var id = tool!.GetType().GetProperty("Id")!.GetValue(tool) as string;
            Assert.Matches("^[a-z][a-z0-9-]*$", id!);
        }
    }
}
