using Moq;
using Quartz;
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
    readonly Mock<ISchedulerFactory> schedulerFactoryMock;
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
        schedulerFactoryMock = new Mock<ISchedulerFactory>();
        lightSwitch = new LightSwitch(
            stateStorageServiceMock.Object,
            schedulerFactoryMock.Object,
            homebridgeEventSenderMock.Object,
            zwaveEventSenderMock.Object,
            "TestLightSwitch"
        );
        lightSwitch.Connect();
    }

    //[Fact]
    //public async Task Publish_ShouldActivateStateMachine()
    //{
    //    // Arrange
    //    mockState = LightSwitchState.ON.ToString();

    //    // Act
    //    await lightSwitch.Publish();

    //    // Assert
    //    Assert.Equal(LightSwitchState.ON, lightSwitch.State);
    //    homebridgeEventSenderMock.Verify(x => x.LightSwitchOn("TestLightSwitch"), Times.Once);
    //    zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestLightSwitch"), Times.Once);
    //    stateStorageServiceMock.Verify(x => x.UpdateState("TestLightSwitch", mockState), Times.Never);
    //}

    /// <summary>
    /// Verifies that when the light switch is set to ON while in the OFF state, the state transitions 
    /// to ON and only homebridge event is sent. Because SET commands should only be used by
    /// events from the zwave device itself to avoid echoing.
    /// </summary>
    [Fact]
    public async Task SetCurrentValue_ShouldSendHomebridgeEvent()
    {
        // Arrange
        mockState = LightSwitchState.OFF.ToString();

        // Act
        await lightSwitch.SetCurrentValue(true);

        // Assert
        Assert.Equal(LightSwitchState.ON, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOn("TestLightSwitch"), Times.Once);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestLightSwitch"), Times.Never);
    }

    /// <summary>
    /// Verifies that when the light switch is requested to be turned ON while in the OFF state, the state transitions 
    /// to PENDING_ON and all events are sent.
    /// </summary>
    [Fact]
    public async Task SetToOn_ShouldSendAllEvent()
    {
        // Arrange
        mockState = LightSwitchState.OFF.ToString();

        // Act
        await lightSwitch.SetToOn(true);

        // Assert
        Assert.Equal(LightSwitchState.PENDING_ON, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOn("TestLightSwitch"), Times.Once);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestLightSwitch"), Times.Once);
    }

    /// <summary>
    /// Verifies that when the light switch is set to ON while it's already in the ON state
    /// does not trigger any further event sending or state changes, preventing redundant operations.
    /// </summary>
    [Fact]
    public async Task SetCurrentValue_WhenAlreadyOn_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = LightSwitchState.ON.ToString();

        // Act
        await lightSwitch.SetCurrentValue(true);

        // Assert
        Assert.Equal(LightSwitchState.ON, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOn("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestLightSwitch"), Times.Never);
    }

    /// <summary>
    /// Verifies that when the light switch is requested to be turned ON while it's already in the ON state
    /// does not trigger any further event sending or state changes, preventing redundant operations.
    /// </summary>
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

    /// <summary>
    /// Verifies that when the light switch is set to ON while in the OFF_AUTO state, the state transitions 
    /// to ON_SCHEDULED_OFF.
    /// </summary>
    [Fact]
    public async Task SetCurrentValue_WhenOffAuto_ShouldBeOnScheduledOff()
    {
        // Arrange
        mockState = LightSwitchState.OFF_AUTO.ToString();

        // Act
        await lightSwitch.SetCurrentValue(true);

        // Assert
        Assert.Equal(LightSwitchState.ON_SCHEDULED_OFF, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOn("TestLightSwitch"), Times.Once);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestLightSwitch"), Times.Never);
    }

    /// <summary>
    /// Verifies that when the light switch is requested to be turned ON while in the OFF_AUTO state, the state transitions 
    /// to PENDING_ON_SCHEDULED_OFF.
    /// </summary>
    [Fact]
    public async Task SetToOn_WhenOffAuto_ShouldBePendingOnScheduledOff()
    {
        // Arrange
        mockState = LightSwitchState.OFF_AUTO.ToString();

        // Act
        await lightSwitch.SetToOn(true);

        // Assert
        Assert.Equal(LightSwitchState.PENDING_ON_SCHEDULED_OFF, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOn("TestLightSwitch"), Times.Once);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestLightSwitch"), Times.Once);
    }

    /// <summary>
    /// Verifies that when the light switch is set to ON while it's already in the ON_SCHEDULED_OFF state
    /// does not trigger any further event sending or state changes, preventing redundant operations.
    /// </summary>
    [Fact]
    public async Task SetCurrentValue_WhenOnScheduledOff_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = LightSwitchState.ON_SCHEDULED_OFF.ToString();

        // Act
        await lightSwitch.SetCurrentValue(true);

        // Assert
        Assert.Equal(LightSwitchState.ON_SCHEDULED_OFF, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOn("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestLightSwitch"), Times.Never);
    }

    /// <summary>
    /// Verifies that when the light switch is requested to be turned ON
    /// while it's already in the ON_SCHEDULED_OFF state
    /// does not trigger any further event sending or state changes, preventing redundant operations.
    /// </summary>
    [Fact]
    public async Task SetToOn_WhenOnScheduledOff_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = LightSwitchState.ON_SCHEDULED_OFF.ToString();

        // Act
        await lightSwitch.SetToOn(true);

        // Assert
        Assert.Equal(LightSwitchState.ON_SCHEDULED_OFF, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOn("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestLightSwitch"), Times.Never);
    }

    /// <summary>
    /// Verifies that when the light switch is set to OFF while in the ON_AUTO state, the state transitions 
    /// to OFF_AUTO.
    /// </summary>
    [Fact]
    public async Task SetToOff_WhenOnAuto_ShouldBeOffAuto()
    {
        // Arrange
        mockState = LightSwitchState.ON_AUTO.ToString();

        // Act
        await lightSwitch.SetCurrentValue(false);

        // Assert
        Assert.Equal(LightSwitchState.OFF_AUTO, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOff("TestLightSwitch"), Times.Once);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOff("TestLightSwitch"), Times.Never);
    }

    /// <summary>
    /// Verifies that when the light switch is set to OFF while it's already in the OFF_AUTO state
    /// does not trigger any further event sending or state changes, preventing redundant operations.
    /// </summary>
    [Fact]
    public async Task SetToOff_WhenOffAuto_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = LightSwitchState.OFF_AUTO.ToString();

        // Act
        await lightSwitch.SetCurrentValue(false);

        // Assert
        Assert.Equal(LightSwitchState.OFF_AUTO, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOff("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOff("TestLightSwitch"), Times.Never);
    }

    [Fact]
    public async Task SetToOff_WhenPendingOff_ShouldBeOff()
    {
        // Arrange
        mockState = LightSwitchState.PENDING_OFF.ToString();

        // Act
        await lightSwitch.SetCurrentValue(false);

        // Assert
        Assert.Equal(LightSwitchState.OFF, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOff("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOff("TestLightSwitch"), Times.Once);
    }

    [Fact]
    public async Task SetToOff_WhenOnScheduledOff_ShouldBeOffAuto()
    {
        // Arrange
        mockState = LightSwitchState.ON_SCHEDULED_OFF.ToString();

        // Act
        await lightSwitch.SetCurrentValue(false);

        // Assert
        Assert.Equal(LightSwitchState.OFF_AUTO, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOff("TestLightSwitch"), Times.Once);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOff("TestLightSwitch"), Times.Once);
    }

    [Fact]
    public async Task AutoSetToOn_WhenAlreadyOnAuto_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = LightSwitchState.ON_AUTO.ToString();

        // Act
        await lightSwitch.AutoSetToOn();

        // Assert
        Assert.Equal(LightSwitchState.ON_AUTO, lightSwitch.State);
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
    public async Task AutoSetToOn_OnScheduledOff_ShouldBeOnAuto()
    {
        // Arrange
        mockState = LightSwitchState.ON_SCHEDULED_OFF.ToString();

        // Act
        await lightSwitch.AutoSetToOn();

        // Assert
        Assert.Equal(LightSwitchState.ON_AUTO, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOn("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestLightSwitch"), Times.Never);
    }

    [Fact]
    public async Task AutoSetToOn_WhenOnAuto_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = LightSwitchState.ON_AUTO.ToString();

        // Act
        await lightSwitch.AutoSetToOn();

        // Assert
        Assert.Equal(LightSwitchState.ON_AUTO, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOn("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestLightSwitch"), Times.Never);
    }

    [Fact]
    public async Task AutoSetToOff_WhenAlreadyOffAuto_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = LightSwitchState.OFF_AUTO.ToString();

        // Act
        await lightSwitch.AutoSetToOff();

        // Assert
        Assert.Equal(LightSwitchState.OFF_AUTO, lightSwitch.State);
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
    public async Task AutoSetToOff_WhenOnScheduledOff_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = LightSwitchState.ON_SCHEDULED_OFF.ToString();

        // Act
        await lightSwitch.AutoSetToOff();

        // Assert
        Assert.Equal(LightSwitchState.ON_SCHEDULED_OFF, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOff("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOff("TestLightSwitch"), Times.Never);
    }

    [Fact]
    public async Task AutoSetToOff_WhenOnAuto_ShouldBePendingOffAuto()
    {
        // Arrange
        mockState = LightSwitchState.ON_AUTO.ToString();

        // Act
        await lightSwitch.AutoSetToOff();

        // Assert
        Assert.Equal(LightSwitchState.PENDING_ON_SCHEDULED_OFF, lightSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOff("TestLightSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOff("TestLightSwitch"), Times.Never);
    }
}