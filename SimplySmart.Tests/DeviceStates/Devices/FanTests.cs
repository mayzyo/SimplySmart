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

public class FanTests
{
    private readonly Mock<IHomebridgeEventSender> homebridgeEventSenderMock;
    private readonly Mock<IZwaveEventSender> zwaveEventSenderMock;
    private readonly Mock<IStateStorageService> stateStorageMock;
    private readonly Fan fan;

    string mockState = ApplianceState.OFF.ToString();

    public FanTests()
    {
        stateStorageMock = new Mock<IStateStorageService>();
        stateStorageMock.Setup(s => s.GetState("TestFan"))
            .Returns(() => mockState.ToString());
        stateStorageMock.Setup(s => s.UpdateState(It.IsAny<string>(), It.IsAny<string>()))
            .Callback((string a, string b) => mockState = b);

        homebridgeEventSenderMock = new Mock<IHomebridgeEventSender>();
        zwaveEventSenderMock = new Mock<IZwaveEventSender>();
        fan = new Fan(stateStorageMock.Object, "TestFan", homebridgeEventSenderMock.Object, zwaveEventSenderMock.Object);
        fan.Connect();
    }

    [Fact]
    public async Task SetToOn_ShouldSendEvents()
    {
        // Arrange
        mockState = ApplianceState.OFF.ToString();

        // Act
        await fan.SetToOn(true);

        // Assert
        Assert.Equal(ApplianceState.ON, fan.State);
        homebridgeEventSenderMock.Verify(x => x.FanOn("TestFan"), Times.Once);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestFan"), Times.Once);
    }

    [Fact]
    public async Task SetToOn_WhenAlreadyOn_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = ApplianceState.ON.ToString();

        // Act
        await fan.SetToOn(true);

        // Assert
        Assert.Equal(ApplianceState.ON, fan.State);
        homebridgeEventSenderMock.Verify(x => x.FanOn("TestFan"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestFan"), Times.Never);
    }

    [Fact]
    public async Task SetToOff_ShouldSendEvents()
    {
        // Arrange
        mockState = ApplianceState.ON.ToString();

        // Act
        await fan.SetToOn(false);

        // Assert
        Assert.Equal(ApplianceState.OFF, fan.State);
        homebridgeEventSenderMock.Verify(x => x.FanOff("TestFan"), Times.Once);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOff("TestFan"), Times.Once);
    }

    [Fact]
    public async Task SetToOff_WhenAlreadyOff_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = ApplianceState.OFF.ToString();

        // Act
        await fan.SetToOn(false);

        // Assert
        Assert.Equal(ApplianceState.OFF, fan.State);
        homebridgeEventSenderMock.Verify(x => x.FanOff("TestGarageDoor"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOff("TestGarageDoor"), Times.Never);
    }
}
