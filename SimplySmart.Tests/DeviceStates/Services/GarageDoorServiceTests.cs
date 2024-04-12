using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SimplySmart.Core.Models;
using SimplySmart.DeviceStates.Devices;
using SimplySmart.DeviceStates.Factories;
using SimplySmart.DeviceStates.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Tests.DeviceStates.Services;

public class GarageDoorServiceTests
{
    readonly Mock<IOptions<ApplicationConfig>> optionsMock;
    readonly Mock<IGarageDoorFactory> garageDoorFactoryMock;
    readonly GarageDoorService garageDoorService;

    public GarageDoorServiceTests()
    {
        optionsMock = new Mock<IOptions<ApplicationConfig>>();
        var loggerMock = new Mock<ILogger>();
        garageDoorFactoryMock = new Mock<IGarageDoorFactory>();
        garageDoorService = new GarageDoorService(optionsMock.Object, loggerMock.Object, garageDoorFactoryMock.Object);
    }

    [Fact]
    public void Indexer_WithValidKey_ReturnsGarageDoor()
    {
        // Arrange
        var key = "garageDoor1";
        var smartImplant = new SmartImplant { name = key };
        optionsMock.Setup(x => x.Value.smartImplants).Returns([smartImplant]);
        var garageDoor = new Mock<IGarageDoor>();
        garageDoorFactoryMock.Setup(x => x.CreateGarageDoor(smartImplant)).Returns(garageDoor.Object);

        // Act
        var result = garageDoorService[key];

        // Assert
        Assert.Equal(garageDoor.Object, result);
    }

    [Fact]
    public void Indexer_WithInvalidKey_ReturnsNull()
    {
        // Arrange
        var key = "invalidKey";
        optionsMock.Setup(x => x.Value.powerSwitches).Returns([]);

        // Act
        var result = garageDoorService[key];

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task PublishAll_WithGarageDoors_PublishesAllGarageDoors()
    {
        // Arrange
        var smartImplants = new[]
        {
            new SmartImplant { name = "garageDoor1", type = "garageDoor" },
            new SmartImplant { name = "garageDoor2", type = "garageDoor" }
        }.ToList();
        var garageDoors = new[]
        {
            new Mock<IGarageDoor>(),
            new Mock<IGarageDoor>()
        };
        optionsMock.Setup(x => x.Value.smartImplants).Returns(smartImplants);
        garageDoorFactoryMock.Setup(x => x.CreateGarageDoor(smartImplants[0])).Returns(garageDoors[0].Object);
        garageDoorFactoryMock.Setup(x => x.CreateGarageDoor(smartImplants[1])).Returns(garageDoors[1].Object);

        // Act
        await garageDoorService.PublishAll();

        // Assert
        garageDoors[0].Verify(x => x.Publish(), Times.Once);
        garageDoors[1].Verify(x => x.Publish(), Times.Once);
    }

    [Fact]
    public async Task PublishAll_WithoutFans_DoesNotPublish()
    {
        // Arrange
        optionsMock.Setup(x => x.Value.powerSwitches).Returns([]);

        // Act
        await garageDoorService.PublishAll();

        // Assert
        garageDoorFactoryMock.Verify(x => x.CreateGarageDoor(It.IsAny<SmartImplant>()), Times.Never);
    }
}
