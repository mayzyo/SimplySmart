﻿using Moq;
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

public class GarageDoorOpenerEventHandlerTests
{
    readonly Mock<IGarageDoorService> garageDoorServiceMock;
    readonly GarageDoorOpenerEventHandler eventHandler;

    public GarageDoorOpenerEventHandlerTests()
    {
        garageDoorServiceMock = new Mock<IGarageDoorService>();
        eventHandler = new GarageDoorOpenerEventHandler(garageDoorServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidOpenMessage_CallsSetToOnWithTrue()
    {
        // Arrange
        var eventArgs = MockBuilder.CreateMockEventArgs("homebridge/garage_door_opener/door1/targetDoorState", "O");
        var garageDoorMock = new Mock<IGarageDoor>();
        garageDoorServiceMock.Setup(x => x["door1"]).Returns(garageDoorMock.Object);


        // Act
        await eventHandler.Handle(eventArgs);

        // Assert
        garageDoorMock.Verify(x => x.SetCurrentValue(true), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCloseMessage_CallsSetToOnWithFalse()
    {
        // Arrange
        var eventArgs = MockBuilder.CreateMockEventArgs("homebridge/garage_door_opener/door2/targetDoorState", "C");
        var garageDoorMock = new Mock<IGarageDoor>();
        garageDoorServiceMock.Setup(x => x["door2"]).Returns(garageDoorMock.Object);

        // Act
        await eventHandler.Handle(eventArgs);

        // Assert
        garageDoorMock.Verify(x => x.SetCurrentValue(false), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidMessage_ThrowsException()
    {
        // Arrange
        var eventArgs = MockBuilder.CreateMockEventArgs("homebridge/garage_door_opener/door3/targetDoorState", "X");

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => eventHandler.Handle(eventArgs));
    }
}
