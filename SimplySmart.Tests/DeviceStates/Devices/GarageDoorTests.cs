using Moq;
using SimplySmart.Homebridge.Services;
using SimplySmart.Zwave.Services;
using SimplySmart.DeviceStates.Devices;
using SimplySmart.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Quartz;
using SimplySmart.Frigate.Services;

namespace SimplySmart.Tests.DeviceStates.Devices;

public class GarageDoorTests
{
    readonly Mock<ILogger<IGarageDoor>> loggerMock;
    readonly Mock<IHomebridgeEventSender> homebridgeEventSenderMock;
    readonly Mock<IZwaveEventSender> zwaveEventSenderMock;
    readonly Mock<IFrigateWebhookSender> frigateWebhookSenderMock;
    readonly Mock<IStateStore> stateStorageServiceMock;
    readonly Mock<ISchedulerFactory> schedulerFactoryMock;
    readonly GarageDoor garageDoor;

    string mockState = GarageDoorState.CLOSED.ToString();

    public GarageDoorTests()
    {
        stateStorageServiceMock = new Mock<IStateStore>();
        stateStorageServiceMock.Setup(s => s.GetState("TestGarageDoor"))
            .Returns(() => mockState.ToString());
        stateStorageServiceMock.Setup(s => s.UpdateState(It.IsAny<string>(), It.IsAny<string>()))
            .Callback((string a, string b) => mockState = b);

        homebridgeEventSenderMock = new Mock<IHomebridgeEventSender>();
        zwaveEventSenderMock = new Mock<IZwaveEventSender>();
        frigateWebhookSenderMock = new Mock<IFrigateWebhookSender>();
        schedulerFactoryMock = new Mock<ISchedulerFactory>();
        loggerMock = new Mock<ILogger<IGarageDoor>>();
        garageDoor = new GarageDoor(
            loggerMock.Object,
            stateStorageServiceMock.Object,
            schedulerFactoryMock.Object,
            homebridgeEventSenderMock.Object,
            zwaveEventSenderMock.Object,
            frigateWebhookSenderMock.Object,
            "TestGarageDoor"
        );
        garageDoor.Connect();
    }

    [Fact]
    public async Task Publish_ShouldActivateStateMachine()
    {
        // Arrange
        mockState = GarageDoorState.OPENED.ToString();

        // Act
        await garageDoor.Publish();

        // Assert
        Assert.Equal(GarageDoorState.OPENED, garageDoor.State);
        homebridgeEventSenderMock.Verify(x => x.GarageDoorOpenerStopped("TestGarageDoor"), Times.Once);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOff("TestGarageDoor"), Times.Once);
        stateStorageServiceMock.Verify(x => x.UpdateState("TestGarageDoor", mockState), Times.Never);
    }

    [Fact]
    public async Task SetToOn_ShouldSendEvent()
    {
        // Arrange
        mockState = GarageDoorState.CLOSED.ToString();

        // Act
        await garageDoor.SetCurrentValue(true);

        // Assert
        Assert.Equal(GarageDoorState.OPENING, garageDoor.State);
        homebridgeEventSenderMock.Verify(x => x.GarageDoorOpenerOn("TestGarageDoor"), Times.Once);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestGarageDoor"), Times.Once);
    }

    [Fact]
    public async Task SetToOn_WhenAlreadyOpening_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = GarageDoorState.OPENING.ToString();

        // Act
        await garageDoor.SetCurrentValue(true);

        // Assert
        Assert.Equal(GarageDoorState.OPENING, garageDoor.State);
        homebridgeEventSenderMock.Verify(x => x.GarageDoorOpenerOn("TestGarageDoor"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestGarageDoor"), Times.Never);
    }

    [Fact]
    public async Task SetToOff_WhenAlreadyClosing_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = GarageDoorState.CLOSING.ToString();

        // Act
        await garageDoor.SetCurrentValue(false);

        // Assert
        Assert.Equal(GarageDoorState.CLOSING, garageDoor.State);
        homebridgeEventSenderMock.Verify(x => x.GarageDoorOpenerOff("TestGarageDoor"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOff("TestGarageDoor"), Times.Never);
    }

    [Fact]
    public async Task SetToOn_WhenAlreadyOpen_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = GarageDoorState.OPENED.ToString();

        // Act
        await garageDoor.SetCurrentValue(true);

        // Assert
        Assert.Equal(GarageDoorState.OPENED, garageDoor.State);
        homebridgeEventSenderMock.Verify(x => x.GarageDoorOpenerOn("TestGarageDoor"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOn("TestGarageDoor"), Times.Never);
    }

    [Fact]
    public async Task SetToOff_WhenAlreadyClosed_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = GarageDoorState.CLOSED.ToString();

        // Act
        await garageDoor.SetCurrentValue(false);

        // Assert
        Assert.Equal(GarageDoorState.CLOSED, garageDoor.State);
        homebridgeEventSenderMock.Verify(x => x.GarageDoorOpenerOff("TestGarageDoor"), Times.Never);
        zwaveEventSenderMock.Verify(x => x.BinarySwitchOff("TestGarageDoor"), Times.Never);
    }
}