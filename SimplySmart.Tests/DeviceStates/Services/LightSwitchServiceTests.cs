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

public class LightSwitchServiceTests
{
    readonly Mock<IOptions<ApplicationConfig>> optionsMock;
    readonly Mock<ILightSwitchFactory> lightSwitchFactoryMock;
    readonly LightSwitchService lightSwitchService;

    public LightSwitchServiceTests()
    {
        optionsMock = new Mock<IOptions<ApplicationConfig>>();
        var loggerMock = new Mock<ILogger>();
        lightSwitchFactoryMock = new Mock<ILightSwitchFactory>();
        lightSwitchService = new LightSwitchService(optionsMock.Object, loggerMock.Object, lightSwitchFactoryMock.Object);
    }

    [Fact]
    public void Indexer_WithValidLightSwitch_ReturnsLightSwitch()
    {
        // Arrange
        var key = "light1";
        var lightSwitch = new SimplySmart.Core.Models.LightSwitch { name = key, isDimmer = false, stayOn = 14000 };
        optionsMock.Setup(x => x.Value.lightSwitches).Returns([lightSwitch]);
        var expectedLightSwitch = new Mock<ILightSwitch>();
        lightSwitchFactoryMock.Setup(x => x.CreateLightSwitch(lightSwitch, lightSwitch.stayOn)).Returns(expectedLightSwitch.Object);

        // Act
        var result = lightSwitchService[key];

        // Assert
        Assert.Equal(expectedLightSwitch.Object, result);
    }

    [Fact]
    public void Indexer_WithValidDimmerLightSwitch_ReturnsDimmerLightSwitch()
    {
        // Arrange
        var key = "dimmer1";
        var lightSwitch = new SimplySmart.Core.Models.LightSwitch { name = key, isDimmer = true, stayOn = 14000 };
        optionsMock.Setup(x => x.Value.lightSwitches).Returns([lightSwitch]);
        var expectedLightSwitch = new Mock<IDimmerLightSwitch>();
        lightSwitchFactoryMock.Setup(x => x.CreateDimmerLightSwitch(lightSwitch, lightSwitch.stayOn)).Returns(expectedLightSwitch.Object);

        // Act
        var result = lightSwitchService[key];

        // Assert
        Assert.Equal(expectedLightSwitch.Object, result);
    }

    [Fact]
    public void Indexer_WithValidPowerSwitch_ReturnsLightSwitch()
    {
        // Arrange
        var key = "power1";
        var powerSwitch = new PowerSwitch { name = key };
        optionsMock.Setup(x => x.Value.powerSwitches).Returns([powerSwitch]);
        var expectedLightSwitch = new Mock<ILightSwitch>();
        lightSwitchFactoryMock.Setup(x => x.CreateLightSwitch(powerSwitch, null)).Returns(expectedLightSwitch.Object);

        // Act
        var result = lightSwitchService[key];

        // Assert
        Assert.Equal(expectedLightSwitch.Object, result);
    }

    [Fact]
    public void Indexer_WithInvalidKey_ReturnsNull()
    {
        // Arrange
        var key = "invalid";
        var applicationConfig = new ApplicationConfig();
        optionsMock.Setup(x => x.Value).Returns(applicationConfig);

        // Act
        var result = lightSwitchService[key];

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task PublishAll_WithLightSwitchesAndPowerSwitches_PublishesAllSwitches()
    {
        // Arrange
        var lightSwitch1 = new SimplySmart.Core.Models.LightSwitch { name = "light1", isDimmer = false, stayOn = 13000 };
        var lightSwitch2 = new SimplySmart.Core.Models.LightSwitch { name = "dimmer1", isDimmer = true, stayOn = 14000 };
        var powerSwitch1 = new PowerSwitch { name = "power1", type = "light" };
        var applicationConfig = new ApplicationConfig
        {
            lightSwitches = [lightSwitch1, lightSwitch2],
            powerSwitches = [powerSwitch1]
        };
        optionsMock.Setup(x => x.Value).Returns(applicationConfig);
        var lightSwitchMock1 = new Mock<ILightSwitch>();
        var dimmerLightSwitchMock = new Mock<IDimmerLightSwitch>();
        var lightSwitchMock2 = new Mock<ILightSwitch>();
        lightSwitchFactoryMock.Setup(x => x.CreateLightSwitch(lightSwitch1, lightSwitch1.stayOn)).Returns(lightSwitchMock1.Object);
        lightSwitchFactoryMock.Setup(x => x.CreateDimmerLightSwitch(lightSwitch2, lightSwitch2.stayOn)).Returns(dimmerLightSwitchMock.Object);
        lightSwitchFactoryMock.Setup(x => x.CreateLightSwitch(powerSwitch1, null)).Returns(lightSwitchMock2.Object);

        // Act
        await lightSwitchService.PublishAll();

        // Assert
        lightSwitchMock1.Verify(x => x.Publish(), Times.Once);
        dimmerLightSwitchMock.Verify(x => x.Publish(), Times.Once);
        lightSwitchMock2.Verify(x => x.Publish(), Times.Once);
    }

    [Fact]
    public void SetAllToAuto_WithTrueCommand_EnablesAutoOnAllSwitches()
    {
        // Arrange
        var lightSwitch1 = new SimplySmart.Core.Models.LightSwitch { name = "light1", isDimmer = false, stayOn = 13000 };
        var lightSwitch2 = new SimplySmart.Core.Models.LightSwitch { name = "dimmer1", isDimmer = true, stayOn = 14000 };
        var powerSwitch1 = new PowerSwitch { name = "power1", type = "light" };
        var applicationConfig = new ApplicationConfig
        {
            lightSwitches = [lightSwitch1, lightSwitch2],
            powerSwitches = [powerSwitch1]
        };
        optionsMock.Setup(x => x.Value).Returns(applicationConfig);
        var lightSwitchMock1 = new Mock<ILightSwitch>();
        var dimmerLightSwitchMock = new Mock<IDimmerLightSwitch>();
        var lightSwitchMock2 = new Mock<ILightSwitch>();
        lightSwitchFactoryMock.Setup(x => x.CreateLightSwitch(lightSwitch1, lightSwitch1.stayOn)).Returns(lightSwitchMock1.Object);
        lightSwitchFactoryMock.Setup(x => x.CreateDimmerLightSwitch(lightSwitch2, lightSwitch2.stayOn)).Returns(dimmerLightSwitchMock.Object);
        lightSwitchFactoryMock.Setup(x => x.CreateLightSwitch(powerSwitch1, null)).Returns(lightSwitchMock2.Object);

        // Act
        lightSwitchService.SetAllToAuto(true);

        // Assert
        lightSwitchMock1.Verify(x => x.EnableAuto(), Times.Once);
        dimmerLightSwitchMock.Verify(x => x.EnableAuto(), Times.Once);
        lightSwitchMock2.Verify(x => x.EnableAuto(), Times.Once);
    }

    [Fact]
    public void SetAllToAuto_WithFalseCommand_DisablesAutoOnAllSwitches()
    {
        // Arrange
        var lightSwitch1 = new SimplySmart.Core.Models.LightSwitch { name = "light1", isDimmer = false, stayOn = 13000 };
        var lightSwitch2 = new SimplySmart.Core.Models.LightSwitch { name = "dimmer1", isDimmer = true, stayOn = 14000 };
        var powerSwitch1 = new PowerSwitch { name = "power1", type = "light" };
        var applicationConfig = new ApplicationConfig
        {
            lightSwitches = [lightSwitch1, lightSwitch2],
            powerSwitches = [powerSwitch1]
        };
        optionsMock.Setup(x => x.Value).Returns(applicationConfig);
        var lightSwitchMock1 = new Mock<ILightSwitch>();
        var dimmerLightSwitchMock = new Mock<IDimmerLightSwitch>();
        var lightSwitchMock2 = new Mock<ILightSwitch>();
        lightSwitchFactoryMock.Setup(x => x.CreateLightSwitch(lightSwitch1, lightSwitch1.stayOn)).Returns(lightSwitchMock1.Object);
        lightSwitchFactoryMock.Setup(x => x.CreateDimmerLightSwitch(lightSwitch2, lightSwitch2.stayOn)).Returns(dimmerLightSwitchMock.Object);
        lightSwitchFactoryMock.Setup(x => x.CreateLightSwitch(powerSwitch1, null)).Returns(lightSwitchMock2.Object);
        // Act
        lightSwitchService.SetAllToAuto(false);

        // Assert
        lightSwitchMock1.Verify(x => x.DisableAuto(), Times.Once);
        dimmerLightSwitchMock.Verify(x => x.DisableAuto(), Times.Once);
        lightSwitchMock2.Verify(x => x.DisableAuto(), Times.Once);
    }
}
