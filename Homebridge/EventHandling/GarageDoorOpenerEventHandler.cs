using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using SimplySmart.DeviceStates.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Homebridge.EventHandling;

public interface IGarageDoorOpenerEventHandler
{
    const string MQTT_TOPIC = "homebridge/garage_door_opener/+/+/+/+/targetDoorState";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

internal class GarageDoorOpenerEventHandler(ILogger<IGarageDoorOpenerEventHandler> logger, IAccessPointService accessPointServices) : IGarageDoorOpenerEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("homebridge/garage_door_opener/", "").Replace("/targetDoorState", "");
        var message = e.ApplicationMessage.ConvertPayloadToString();

        await UpdateState(name, message);
    }

    private async Task UpdateState(string name, string message)
    {
        var command = ConvertMessage(message);
        var garageDoor = (IGarageDoor)accessPointServices[name];
        await garageDoor.Trigger(command);
    }

    private static GarageDoorCommand ConvertMessage(string message)
    {
        switch (message)
        {
            case "O": return GarageDoorCommand.OPEN;
            case "C": return GarageDoorCommand.CLOSE;
            default: throw new Exception("Undefined value received from homebridge garage door opener");
        }
    }
}
