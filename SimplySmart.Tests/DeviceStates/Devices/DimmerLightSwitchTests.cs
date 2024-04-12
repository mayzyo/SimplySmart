using Moq;
using SimplySmart.Core.Abstractions;
using SimplySmart.Core.Models;
using SimplySmart.DeviceStates.Devices;
using SimplySmart.Homebridge.Services;
using SimplySmart.Zwave.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Tests.DeviceStates.Devices;

public class DimmerLightSwitchTests
{
    readonly Mock<IStateStore> stateStorageServiceMock;
    readonly Mock<IHomebridgeEventSender> homebridgeEventSenderMock;
    readonly Mock<IZwaveEventSender> zwaveEventSenderMock;
    readonly DimmerLightSwitch dimmerSwitch;

    string mockState = LightSwitchState.OFF.ToString();
    string mockBrightness = "0";

    public DimmerLightSwitchTests()
    {
        stateStorageServiceMock = new Mock<IStateStore>();
        stateStorageServiceMock.Setup(s => s.GetState("TestDimmerSwitch"))
            .Returns(() => mockState);
        stateStorageServiceMock.Setup(s => s.UpdateState("TestDimmerSwitch", It.IsAny<string>()))
            .Callback((string a, string b) => mockState = b);
        stateStorageServiceMock.Setup(s => s.GetState("TestDimmerSwitch_brightness"))
            .Returns(() => mockBrightness);
        stateStorageServiceMock.Setup(s => s.UpdateState("TestDimmerSwitch_brightness", It.IsAny<string>()))
            .Callback((string a, string b) => mockBrightness = b);

        homebridgeEventSenderMock = new Mock<IHomebridgeEventSender>();
        zwaveEventSenderMock = new Mock<IZwaveEventSender>();
        dimmerSwitch = new DimmerLightSwitch(stateStorageServiceMock.Object, homebridgeEventSenderMock.Object, zwaveEventSenderMock.Object, "TestDimmerSwitch", null);
        dimmerSwitch.Connect();
    }

    [Fact]
    public async Task Publish_ShouldActivateStateMachine()
    {
        // Arrange
        ushort level = 35;
        mockState = LightSwitchState.ON.ToString();
        mockBrightness = level.ToString();

        // Act
        await dimmerSwitch.Publish();

        // Assert
        Assert.Equal(LightSwitchState.ON, dimmerSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.DimmerBrightness("TestDimmerSwitch", level), Times.Once);
        zwaveEventSenderMock.Verify(x => x.MultiLevelSwitchUpdate("TestDimmerSwitch", level), Times.Once);
        stateStorageServiceMock.Verify(x => x.UpdateState("TestDimmerSwitch", mockState), Times.Never);
    }

    [Fact]
    public async Task SetLevel_ShouldSendEvent()
    {
        // Arrange
        ushort level = 35;
        mockState = LightSwitchState.OFF.ToString();
        mockBrightness = 0.ToString();

        // Act
        await dimmerSwitch.SetLevel(level);

        // Assert
        Assert.Equal(LightSwitchState.ON, dimmerSwitch.State);
        Assert.Equal(level, dimmerSwitch.Brightness);
        homebridgeEventSenderMock.Verify(x => x.DimmerBrightness("TestDimmerSwitch", level), Times.Once);
        zwaveEventSenderMock.Verify(x => x.MultiLevelSwitchUpdate("TestDimmerSwitch", level), Times.Once);
    }

    [Fact]
    public async Task SetLevel_AtSameBrightness_ShouldIgnoreCommand()
    {
        // Arrange
        ushort level = 35;
        mockState = LightSwitchState.ON.ToString();
        mockBrightness = level.ToString();

        // Act
        await dimmerSwitch.SetLevel(level);

        // Assert
        Assert.Equal(LightSwitchState.ON, dimmerSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.DimmerBrightness("TestDimmerSwitch", level), Times.Never);
        zwaveEventSenderMock.Verify(x => x.MultiLevelSwitchUpdate("TestDimmerSwitch", level), Times.Never);
    }

    [Fact]
    public async Task SetLevel_AtDifferentBrightness_ShouldUpdateBrightness()
    {
        // Arrange
        ushort level = 35;
        mockState = LightSwitchState.ON.ToString();
        mockBrightness = 50.ToString();

        // Act
        await dimmerSwitch.SetLevel(level);

        // Assert
        Assert.Equal(LightSwitchState.ON, dimmerSwitch.State);
        Assert.Equal(level, dimmerSwitch.Brightness);
        homebridgeEventSenderMock.Verify(x => x.DimmerBrightness("TestDimmerSwitch", level), Times.Once);
        zwaveEventSenderMock.Verify(x => x.MultiLevelSwitchUpdate("TestDimmerSwitch", level), Times.Once);
    }

    [Fact]
    public async Task SetLevel_WhenOff_ShouldBeOn()
    {
        // Arrange
        ushort level = 35;
        mockState = LightSwitchState.OFF.ToString();
        mockBrightness = 0.ToString();

        // Act
        await dimmerSwitch.SetLevel(level);

        // Assert
        Assert.Equal(LightSwitchState.ON, dimmerSwitch.State);
        Assert.Equal(level, dimmerSwitch.Brightness);
        homebridgeEventSenderMock.Verify(x => x.DimmerBrightness("TestDimmerSwitch", level), Times.Once);
        zwaveEventSenderMock.Verify(x => x.MultiLevelSwitchUpdate("TestDimmerSwitch", level), Times.Once);
    }

    [Fact]
    public async Task SetLevelZero_WhenOn_ShouldBeOff()
    {
        // Arrange
        ushort level = 0;
        mockState = LightSwitchState.ON.ToString();
        mockBrightness = 50.ToString();

        // Act
        await dimmerSwitch.SetLevel(level);

        // Assert
        Assert.Equal(LightSwitchState.OFF, dimmerSwitch.State);
        Assert.Equal(level, dimmerSwitch.Brightness);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOff("TestDimmerSwitch"), Times.Once);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOff("TestDimmerSwitch"), Times.Once);
    }

    [Fact]
    public async Task SetLevelZero_WhenOff_ShouldIgnoreCommand()
    {
        // Arrange
        ushort level = 0;
        mockState = LightSwitchState.OFF.ToString();
        mockBrightness = level.ToString();

        // Act
        await dimmerSwitch.SetLevel(level);

        // Assert
        Assert.Equal(LightSwitchState.OFF, dimmerSwitch.State);
        homebridgeEventSenderMock.Verify(x => x.LightSwitchOff("TestDimmerSwitch"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOff("TestDimmerSwitch"), Times.Never);
    }
}
