using Moq;
using SimplySmart.DeviceStates.Devices;
using SimplySmart.DeviceStates.Services;
using SimplySmart.Homebridge.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplySmart.Tests.Utils;

namespace SimplySmart.Tests.Homebridge.EventHandling;

public class DimmerLightSwitchEventHandlerTests
{
    private readonly Mock<ILightSwitchService> lightSwitchServiceMock;
    private readonly DimmerLightSwitchEventHandler eventHandler;

    public DimmerLightSwitchEventHandlerTests()
    {
        lightSwitchServiceMock = new Mock<ILightSwitchService>();
        eventHandler = new DimmerLightSwitchEventHandler(lightSwitchServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithBrightness_CallsSetLevel()
    {
        // Arrange
        var name = "test_switch";
        var brightness = 50;
        var eventArgs = MockBuilder.CreateMockEventArgs($"homebridge/light_switch/{name}/brightness", brightness.ToString());

        var dimmerSwitchMock = new Mock<IDimmerLightSwitch>();
        lightSwitchServiceMock.Setup(x => x[name]).Returns(dimmerSwitchMock.Object);

        // Act
        await eventHandler.Handle(eventArgs);

        // Assert
        dimmerSwitchMock.Verify(x => x.SetCurrentLevel((ushort)brightness), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutBrightness_CallsSetToOn()
    {
        // Arrange
        var name = "test_switch";
        var isOn = true;
        var eventArgs = MockBuilder.CreateMockEventArgs($"homebridge/light_switch/{name}", isOn.ToString());
        var dimmerSwitchMock = new Mock<IDimmerLightSwitch>();
        lightSwitchServiceMock.Setup(x => x[name]).Returns(dimmerSwitchMock.Object);

        // Act
        await eventHandler.Handle(eventArgs);

        // Assert
        dimmerSwitchMock.Verify(x => x.SetCurrentValue(isOn), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullDimmerSwitch_DoesNotThrowException()
    {
        // Arrange
        var name = "test_switch";
        var eventArgs = MockBuilder.CreateMockEventArgs($"homebridge/light_switch/{name}", "true");
        lightSwitchServiceMock.Setup(x => x[name]).Returns((IDimmerLightSwitch)null);

        // Act & Assert
        await eventHandler.Handle(eventArgs);
    }
}
