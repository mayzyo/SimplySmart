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

public class LightSwitchEventHandlerTests
{
    private readonly Mock<ILightSwitchService> lightSwitchServiceMock;
    private readonly LightSwitchEventHandler handler;

    public LightSwitchEventHandlerTests()
    {
        lightSwitchServiceMock = new Mock<ILightSwitchService>();
        handler = new LightSwitchEventHandler(lightSwitchServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidMessage_ShouldSetLightSwitchState()
    {
        // Arrange
        var lightSwitchName = "test_switch";
        var message = "true";
        var eventArgs = MockBuilder.CreateMockEventArgs($"homebridge/light_switch/{lightSwitchName}", message);
        var lightSwitchMock = new Mock<ILightSwitch>();
        lightSwitchServiceMock.Setup(x => x[lightSwitchName]).Returns(lightSwitchMock.Object);

        // Act
        await handler.Handle(eventArgs);

        // Assert
        lightSwitchMock.Verify(x => x.SetToOn(true), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidMessage_ShouldNotSetLightSwitchState()
    {
        // Arrange
        var lightSwitchName = "test_switch";
        var message = "invalid_message";
        var eventArgs = MockBuilder.CreateMockEventArgs($"homebridge/light_switch/{lightSwitchName}", message);
        var lightSwitchMock = new Mock<ILightSwitch>();
        lightSwitchServiceMock.Setup(x => x[lightSwitchName]).Returns(lightSwitchMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(() => handler.Handle(eventArgs));
        lightSwitchMock.Verify(x => x.SetToOn(It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NonExistentLightSwitch_ShouldNotThrowException()
    {
        // Arrange
        var lightSwitchName = "non_existent_switch";
        var message = "true";
        var eventArgs = MockBuilder.CreateMockEventArgs($"homebridge/light_switch/{lightSwitchName}", message);
        lightSwitchServiceMock.Setup(x => x[lightSwitchName]).Returns((ILightSwitch)null);

        // Act & Assert
        await handler.Handle(eventArgs);
    }
}
