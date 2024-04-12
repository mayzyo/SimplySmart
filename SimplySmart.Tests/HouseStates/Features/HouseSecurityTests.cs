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

public class HouseSecurityTests
{
    private readonly Mock<IStateStore> stateStorageServiceMock;
    private readonly Mock<IHomebridgeEventSender> homebridgeEventSenderMock;
    private readonly HouseSecurity houseSecurity;

    string mockState = HouseSecurityState.OFF.ToString();

    public HouseSecurityTests()
    {
        stateStorageServiceMock = new Mock<IStateStore>();
        stateStorageServiceMock.Setup(s => s.GetState("house_security"))
            .Returns(() => mockState);
        stateStorageServiceMock.Setup(s => s.UpdateState("house_security", It.IsAny<string>()))
            .Callback((string a, string b) => mockState = b);
        homebridgeEventSenderMock = new Mock<IHomebridgeEventSender>();
        houseSecurity = new HouseSecurity(stateStorageServiceMock.Object, homebridgeEventSenderMock.Object);
        houseSecurity.Connect();
    }

    [Fact]
    public async Task Publish_ShouldSendUpdateEvent()
    {
        // Arrange
        mockState = HouseSecurityState.AWAY.ToString();

        // Act
        await houseSecurity.Publish();

        // Assert
        Assert.Equal(HouseSecurityState.AWAY, houseSecurity.State);
        homebridgeEventSenderMock.Verify(x => x.HouseSecurityUpdate(HouseSecurityState.AWAY), Times.Once);
        stateStorageServiceMock.Verify(x => x.UpdateState("house_security", mockState), Times.Never);
    }

    [Fact]
    public async Task Trigger_HOME_ShouldSendEvents()
    {
        // Arrange
        mockState = HouseSecurityState.OFF.ToString();

        // Act
        await houseSecurity.Trigger(HouseSecurityCommand.SET_HOME);

        // Assert
        Assert.Equal(HouseSecurityState.HOME, houseSecurity.State);
        homebridgeEventSenderMock.Verify(x => x.HouseSecurityUpdate(HouseSecurityState.HOME), Times.Once);
    }

    [Fact]
    public async Task Trigger_HOME_WhenAlreadyHome_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = HouseSecurityState.HOME.ToString();

        // Act
        await houseSecurity.Trigger(HouseSecurityCommand.SET_HOME);

        // Assert
        Assert.Equal(HouseSecurityState.HOME, houseSecurity.State);
        homebridgeEventSenderMock.Verify(x => x.HouseSecurityUpdate(HouseSecurityState.HOME), Times.Never);
    }

    [Fact]
    public async Task Trigger_AWAY_ShouldSendEvents()
    {
        // Arrange
        mockState = HouseSecurityState.HOME.ToString();

        // Act
        await houseSecurity.Trigger(HouseSecurityCommand.SET_AWAY);

        // Assert
        Assert.Equal(HouseSecurityState.AWAY, houseSecurity.State);
        homebridgeEventSenderMock.Verify(x => x.HouseSecurityUpdate(HouseSecurityState.AWAY), Times.Once);
    }

    [Fact]
    public async Task Trigger_AWAY_WhenAlreadyAway_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = HouseSecurityState.AWAY.ToString();

        // Act
        await houseSecurity.Trigger(HouseSecurityCommand.SET_AWAY);

        // Assert
        Assert.Equal(HouseSecurityState.AWAY, houseSecurity.State);
        homebridgeEventSenderMock.Verify(x => x.HouseSecurityUpdate(HouseSecurityState.AWAY), Times.Never);
    }

    [Fact]
    public async Task Trigger_NIGHT_ShouldSendEvents()
    {
        // Arrange
        mockState = HouseSecurityState.HOME.ToString();

        // Act
        await houseSecurity.Trigger(HouseSecurityCommand.SET_NIGHT);

        // Assert
        Assert.Equal(HouseSecurityState.NIGHT, houseSecurity.State);
        homebridgeEventSenderMock.Verify(x => x.HouseSecurityUpdate(HouseSecurityState.NIGHT), Times.Once);
    }

    [Fact]
    public async Task Trigger_NIGHT_WhenAlreadyNight_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = HouseSecurityState.NIGHT.ToString();

        // Act
        await houseSecurity.Trigger(HouseSecurityCommand.SET_NIGHT);

        // Assert
        Assert.Equal(HouseSecurityState.NIGHT, houseSecurity.State);
        homebridgeEventSenderMock.Verify(x => x.HouseSecurityUpdate(HouseSecurityState.NIGHT), Times.Never);
    }

    [Fact]
    public async Task Trigger_OFF_ShouldSendEvents()
    {
        // Arrange
        mockState = HouseSecurityState.HOME.ToString();

        // Act
        await houseSecurity.Trigger(HouseSecurityCommand.SET_OFF);

        // Assert
        Assert.Equal(HouseSecurityState.OFF, houseSecurity.State);
        homebridgeEventSenderMock.Verify(x => x.HouseSecurityUpdate(HouseSecurityState.OFF), Times.Once);
    }

    [Fact]
    public async Task Trigger_OFF_WhenAlreadyOff_ShouldIgnoreCommand()
    {
        // Arrange
        mockState = HouseSecurityState.OFF.ToString();

        // Act
        await houseSecurity.Trigger(HouseSecurityCommand.SET_OFF);

        // Assert
        Assert.Equal(HouseSecurityState.OFF, houseSecurity.State);
        homebridgeEventSenderMock.Verify(x => x.HouseSecurityUpdate(HouseSecurityState.OFF), Times.Never);
    }
}
