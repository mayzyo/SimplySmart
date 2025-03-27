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

public class FanServiceTests
{
    readonly Mock<IOptions<ApplicationConfig>> optionsMock;
    readonly Mock<IFanFactory> fanFactoryMock;
    readonly FanService fanService;

    public FanServiceTests()
    {
        optionsMock = new Mock<IOptions<ApplicationConfig>>();
        var loggerMock = new Mock<ILogger>();
        fanFactoryMock = new Mock<IFanFactory>();
        fanService = new FanService(optionsMock.Object, (ILogger<IFanService>)loggerMock.Object, fanFactoryMock.Object);
    }

    [Fact]
    public void Indexer_WithValidKey_ReturnsFan()
    {
        // Arrange
        var key = "fan1";
        var powerSwitch = new PowerSwitch { Name = key };
        optionsMock.Setup(x => x.Value.PowerSwitches).Returns([powerSwitch]);
        var fan = new Mock<IFan>();
        fanFactoryMock.Setup(x => x.CreateFan(powerSwitch)).Returns(fan.Object);

        // Act
        var result = fanService[key];

        // Assert
        Assert.Equal(fan.Object, result);
    }

    [Fact]
    public void Indexer_WithInvalidKey_ReturnsNull()
    {
        // Arrange
        var key = "invalidKey";
        optionsMock.Setup(x => x.Value.PowerSwitches).Returns([]);

        // Act
        var result = fanService[key];

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task PublishAll_WithFans_PublishesAllFans()
    {
        // Arrange
        var powerSwitches = new[]
        {
            new PowerSwitch { Name = "fan1", Type = "fan" },
            new PowerSwitch { Name = "fan2", Type = "fan" }
        }.ToList();
        var fans = new[]
        {
            new Mock<IFan>(),
            new Mock<IFan>()
        };
        optionsMock.Setup(x => x.Value.PowerSwitches).Returns(powerSwitches);
        fanFactoryMock.Setup(x => x.CreateFan(powerSwitches[0])).Returns(fans[0].Object);
        fanFactoryMock.Setup(x => x.CreateFan(powerSwitches[1])).Returns(fans[1].Object);

        // Act
        await fanService.PublishAll();

        // Assert
        fans[0].Verify(x => x.Publish(), Times.Once);
        fans[1].Verify(x => x.Publish(), Times.Once);
    }

    [Fact]
    public async Task PublishAll_WithoutFans_DoesNotPublish()
    {
        // Arrange
        optionsMock.Setup(x => x.Value.PowerSwitches).Returns([]);

        // Act
        await fanService.PublishAll();

        // Assert
        fanFactoryMock.Verify(x => x.CreateFan(It.IsAny<PowerSwitch>()), Times.Never);
    }
}
