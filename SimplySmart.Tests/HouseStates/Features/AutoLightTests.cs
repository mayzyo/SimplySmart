using Moq;
using SimplySmart.Core.Abstractions;
using SimplySmart.DeviceStates.Services;
using SimplySmart.Homebridge.Services;
using SimplySmart.HouseStates.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Tests.HouseStates.Features;

public class AutoLightTests
{
    private readonly Mock<IStateStore> stateStorageServiceMock;
    private readonly Mock<IHomebridgeEventSender> homebridgeEventSenderMock;
    private readonly Mock<ILightSwitchService> lightSwitchServiceMock;
    private readonly AutoLight autoLight;

    string mockState = AutoLightState.OFF.ToString();

    public AutoLightTests()
    {
        stateStorageServiceMock = new Mock<IStateStore>();
        stateStorageServiceMock.Setup(s => s.GetState("auto_light"))
            .Returns(() => mockState);
        stateStorageServiceMock.Setup(s => s.UpdateState("auto_light", It.IsAny<string>()))
            .Callback((string a, string b) => mockState = b);
        homebridgeEventSenderMock = new Mock<IHomebridgeEventSender>();
        lightSwitchServiceMock = new Mock<ILightSwitchService>();
        autoLight = new AutoLight(stateStorageServiceMock.Object, homebridgeEventSenderMock.Object, lightSwitchServiceMock.Object);
        autoLight.Connect();
    }

    [Fact]
    public async Task Publish_ShouldActivateStateMachine()
    {
        // Arrange
        mockState = AutoLightState.ON.ToString();

        // Act
        await autoLight.Publish();

        // Assert
        Assert.Equal(AutoLightState.ON, autoLight.State);
        homebridgeEventSenderMock.Verify(x => x.SwitchOn("auto_light"), Times.Once);
        lightSwitchServiceMock.Verify(x => x.SetAllToAuto(true), Times.Once);
        stateStorageServiceMock.Verify(x => x.UpdateState("auto_light", mockState), Times.Never);
    }

    [Fact]
    public async Task Trigger_ON_ShouldSendEvents()
    {
        // Arrange
        mockState = AutoLightState.OFF.ToString();

        // Act
        await autoLight.Trigger(AutoLightCommand.ON);

        // Assert
        Assert.Equal(AutoLightState.ON, autoLight.State);
        lightSwitchServiceMock.Verify(x => x.SetAllToAuto(true), Times.Once);
        homebridgeEventSenderMock.Verify(x => x.SwitchOn("auto_light"), Times.Once);
    }

    [Fact]
    public async Task Trigger_ON_WhenAlreadyOn_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = AutoLightState.ON.ToString();

        // Act
        await autoLight.Trigger(AutoLightCommand.ON);

        // Assert
        Assert.Equal(AutoLightState.ON, autoLight.State);
        lightSwitchServiceMock.Verify(x => x.SetAllToAuto(true), Times.Never);
        homebridgeEventSenderMock.Verify(x => x.SwitchOn("auto_light"), Times.Never);
    }

    [Fact]
    public async Task Trigger_OFF_ShouldSendEvents()
    {
        // Arrange
        mockState = AutoLightState.ON.ToString();

        // Act
        await autoLight.Trigger(AutoLightCommand.OFF);

        // Assert
        Assert.Equal(AutoLightState.OFF, autoLight.State);
        lightSwitchServiceMock.Verify(x => x.SetAllToAuto(false), Times.Once);
        homebridgeEventSenderMock.Verify(x => x.SwitchOff("auto_light"), Times.Once);
    }

    [Fact]
    public async Task Trigger_OFF_WhenAlreadyOff_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = AutoLightState.OFF.ToString();

        // Act
        await autoLight.Trigger(AutoLightCommand.OFF);

        // Assert
        Assert.Equal(AutoLightState.OFF, autoLight.State);
        lightSwitchServiceMock.Verify(x => x.SetAllToAuto(false), Times.Never);
        homebridgeEventSenderMock.Verify(x => x.SwitchOff("auto_light"), Times.Never);
    }
}
