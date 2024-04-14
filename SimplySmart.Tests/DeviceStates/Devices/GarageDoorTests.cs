using Moq;
using SimplySmart.Homebridge.Services;
using SimplySmart.Zwave.Services;
using SimplySmart.DeviceStates.Devices;
using SimplySmart.Core.Abstractions;

namespace SimplySmart.Tests.DeviceStates.Devices;

public class GarageDoorTests
{
    private readonly Mock<IHomebridgeEventSender> homebridgeEventSenderMock;
    private readonly Mock<IZwaveEventSender> zwaveEventSenderMock;
    private readonly Mock<IStateStore> stateStorageServiceMock;
    private readonly GarageDoor garageDoor;

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
        garageDoor = new GarageDoor(stateStorageServiceMock.Object, "TestGarageDoor", homebridgeEventSenderMock.Object, zwaveEventSenderMock.Object);
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