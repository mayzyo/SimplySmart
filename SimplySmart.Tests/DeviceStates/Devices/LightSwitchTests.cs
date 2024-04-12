using Moq;
using SimplySmart.Core.Abstractions;
using SimplySmart.DeviceStates.Devices;
using SimplySmart.Homebridge.Services;
using SimplySmart.Zwave.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Tests.DeviceStates.Devices;

public class LightSwitchTests
{
    readonly Mock<IStateStore> stateStorageServiceMock;
    readonly Mock<IHomebridgeEventSender> homebridgeEventSenderMock;
    readonly Mock<IZwaveEventSender> zwaveEventSenderMock;
    readonly LightSwitch lightSwitch;

    string mockState = LightSwitchState.OFF.ToString();

    public LightSwitchTests()
    {
        stateStorageServiceMock = new Mock<IStateStore>();
        stateStorageServiceMock.Setup(s => s.GetState("TestLightSwitch"))
            .Returns(() => mockState);
        stateStorageServiceMock.Setup(s => s.UpdateState("TestLightSwitch", It.IsAny<string>()))
            .Callback((string a, string b) => mockState = b);

        homebridgeEventSenderMock = new Mock<IHomebridgeEventSender>();
        zwaveEventSenderMock = new Mock<IZwaveEventSender>();
        lightSwitch = new LightSwitch(stateStorageServiceMock.Object, homebridgeEventSenderMock.Object, zwaveEventSenderMock.Object, "TestLightSwitch", null);
        lightSwitch.Connect();
    }

    [Fact]
    public async Task Publish_ShouldActivateStateMachine()
    {
        // Arrange
        mockState = LightSwitchState.ON.ToString();

        // Act
        await lightSwitch.Publish();

        // Assert
        Assert.Equal(LightSwitchState.ON, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOn("TestLightSwitch"), Times.Once);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestLightSwitch"), Times.Once);
        stateStorageServiceMock.Verify(x => x.UpdateState("TestLightSwitch", mockState), Times.Never);
    }

    [Fact]
    public async Task SetToOn_ShouldSendEvent()
    {
        // Arrange
        mockState = LightSwitchState.OFF.ToString();

        // Act
        await lightSwitch.SetToOn(true);

        // Assert
        Assert.Equal(LightSwitchState.ON, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOn("TestLightSwitch"), Times.Once);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestLightSwitch"), Times.Once);
    }

    [Fact]
    public async Task SetToOn_WhenAlreadyOn_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = LightSwitchState.ON.ToString();

        // Act
        await lightSwitch.SetToOn(true);

        // Assert
        Assert.Equal(LightSwitchState.ON, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOn("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestLightSwitch"), Times.Never);
    }

    [Fact]
    public async Task SetToOn_WhenAutoOn_ShouldBeForcedOn()
    {
        // Arrange
        mockState = LightSwitchState.AUTO_ON.ToString();

        // Act
        await lightSwitch.SetToOn(true);

        // Assert
        Assert.Equal(LightSwitchState.AUTO_FORCED_ON, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOn("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestLightSwitch"), Times.Never);
    }

    [Fact]
    public async Task SetToOn_WhenAutoOff_ShouldBeForcedOn()
    {
        // Arrange
        mockState = LightSwitchState.AUTO_OFF.ToString();

        // Act
        await lightSwitch.SetToOn(true);

        // Assert
        Assert.Equal(LightSwitchState.AUTO_FORCED_ON, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOn("TestLightSwitch"), Times.Once);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestLightSwitch"), Times.Once);
    }


    [Fact]
    public async Task SetToOn_WhenPendingOff_ShouldBeForcedOn()
    {
        // Arrange
        await ArrangeAutoPendingOff();

        // Act
        await lightSwitch.SetToOn(true);

        // Assert
        Assert.Equal(LightSwitchState.AUTO_FORCED_ON, lightSwitch.State);
        Assert.False(lightSwitch.PendingTrigger);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOn("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestLightSwitch"), Times.Never);
    }

    [Fact]
    public async Task SetToOn_WhenForcedOn_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = LightSwitchState.AUTO_FORCED_ON.ToString();

        // Act
        await lightSwitch.SetToOn(true);

        // Assert
        Assert.Equal(LightSwitchState.AUTO_FORCED_ON, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOn("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestLightSwitch"), Times.Never);
    }

    [Fact]
    public async Task SetToOff_WhenAlreadyOff_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = LightSwitchState.OFF.ToString();

        // Act
        await lightSwitch.SetToOn(false);

        // Assert
        Assert.Equal(LightSwitchState.OFF, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOff("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOff("TestLightSwitch"), Times.Never);
    }

    [Fact]
    public async Task SetToOff_WhenAutoOn_ShouldBeAutoOff()
    {
        // Arrange
        mockState = LightSwitchState.AUTO_ON.ToString();

        // Act
        await lightSwitch.SetToOn(false);

        // Assert
        Assert.Equal(LightSwitchState.AUTO_OFF, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOff("TestLightSwitch"), Times.Once);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOff("TestLightSwitch"), Times.Once);
    }

    [Fact]
    public async Task SetToOff_WhenAutoOff_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = LightSwitchState.AUTO_OFF.ToString();

        // Act
        await lightSwitch.SetToOn(false);

        // Assert
        Assert.Equal(LightSwitchState.AUTO_OFF, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOff("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOff("TestLightSwitch"), Times.Never);
    }

    [Fact]
    public async Task SetToOff_WhenPendingOff_ShouldBeAutoOff()
    {
        // Arrange
        await ArrangeAutoPendingOff();

        // Act
        await lightSwitch.SetToOn(false);

        // Assert
        Assert.Equal(LightSwitchState.AUTO_OFF, lightSwitch.State);
        Assert.False(lightSwitch.PendingTrigger);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOff("TestLightSwitch"), Times.Once);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOff("TestLightSwitch"), Times.Once);
    }

    [Fact]
    public async Task SetToOff_WhenForcedOn_ShouldBeAutoOff()
    {
        // Arrange
        mockState = LightSwitchState.AUTO_FORCED_ON.ToString();

        // Act
        await lightSwitch.SetToOn(false);

        // Assert
        Assert.Equal(LightSwitchState.AUTO_OFF, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOff("TestLightSwitch"), Times.Once);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOff("TestLightSwitch"), Times.Once);
    }

    [Fact]
    public async Task AutoSetToOn_ShouldSendEvent()
    {
        // Arrange
        mockState = LightSwitchState.AUTO_OFF.ToString();

        // Act
        await lightSwitch.AutoSetToOn();

        // Assert
        Assert.Equal(LightSwitchState.AUTO_ON, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOn("TestLightSwitch"), Times.Once);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestLightSwitch"), Times.Once);
    }

    [Fact]
    public async Task AutoSetToOn_WhenAlreadyAutoOn_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = LightSwitchState.AUTO_ON.ToString();

        // Act
        await lightSwitch.AutoSetToOn();

        // Assert
        Assert.Equal(LightSwitchState.AUTO_ON, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOn("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestLightSwitch"), Times.Never);
    }

    [Fact]
    public async Task AutoSetToOn_WhenOn_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = LightSwitchState.ON.ToString();

        // Act
        await lightSwitch.AutoSetToOn();

        // Assert
        Assert.Equal(LightSwitchState.ON, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOn("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestLightSwitch"), Times.Never);
    }

    [Fact]
    public async Task AutoSetToOn_WhenOff_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = LightSwitchState.OFF.ToString();

        // Act
        await lightSwitch.AutoSetToOn();

        // Assert
        Assert.Equal(LightSwitchState.OFF, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOn("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestLightSwitch"), Times.Never);
    }

    [Fact]
    public async Task AutoSetToOn_WhenPendingOff_ShouldBeAutoOn()
    {
        // Arrange
        await ArrangeAutoPendingOff();

        // Act
        await lightSwitch.AutoSetToOn();

        // Assert
        Assert.Equal(LightSwitchState.AUTO_ON, lightSwitch.State);
        Assert.False(lightSwitch.PendingTrigger);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOn("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestLightSwitch"), Times.Never);
    }

    [Fact]
    public async Task AutoSetToOn_WhenForcedOn_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = LightSwitchState.AUTO_FORCED_ON.ToString();

        // Act
        await lightSwitch.AutoSetToOn();

        // Assert
        Assert.Equal(LightSwitchState.AUTO_FORCED_ON, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOn("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestLightSwitch"), Times.Never);
    }

    [Fact]
    public async Task AutoSetToOff_WhenAlreadyAutoOff_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = LightSwitchState.AUTO_OFF.ToString();

        // Act
        await lightSwitch.AutoSetToOff();

        // Assert
        Assert.Equal(LightSwitchState.AUTO_OFF, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOff("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOff("TestLightSwitch"), Times.Never);
    }

    [Fact]
    public async Task AutoSetToOff_WhenOn_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = LightSwitchState.ON.ToString();

        // Act
        await lightSwitch.AutoSetToOff();

        // Assert
        Assert.Equal(LightSwitchState.ON, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOff("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOff("TestLightSwitch"), Times.Never);
    }

    [Fact]
    public async Task AutoSetToOff_WhenOff_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = LightSwitchState.OFF.ToString();

        // Act
        await lightSwitch.AutoSetToOff();

        // Assert
        Assert.Equal(LightSwitchState.OFF, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOff("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOff("TestLightSwitch"), Times.Never);
    }

    [Fact]
    public async Task AutoSetToOff_WhenPendingOff_ShouldIgnoreCommand()
    {
        // Arrange
        await ArrangeAutoPendingOff();

        // Act
        await lightSwitch.AutoSetToOff();

        // Assert
        Assert.Equal(LightSwitchState.AUTO_PENDING_OFF, lightSwitch.State);
        Assert.True(lightSwitch.PendingTrigger);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOff("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOff("TestLightSwitch"), Times.Never);
    }

    [Fact]
    public async Task AutoSetToOff_WhenForcedOn_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = LightSwitchState.AUTO_FORCED_ON.ToString();

        // Act
        await lightSwitch.AutoSetToOff();

        // Assert
        Assert.Equal(LightSwitchState.AUTO_FORCED_ON, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOff("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOff("TestLightSwitch"), Times.Never);
    }

    async Task ArrangeAutoPendingOff()
    {
        mockState = LightSwitchState.AUTO_ON.ToString();
        await lightSwitch.AutoSetToOff();

        if(lightSwitch.State != LightSwitchState.AUTO_PENDING_OFF)
        {
            throw new Exception("Light Switch did not set to Auto Pending Off");
        }

        if (lightSwitch.PendingTrigger != true)
        {
            throw new Exception("Light Switch delay timer did not enable");
        }
    }
}