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

internal class GarageDoorOpenerEventHandler(IGarageDoorService garageDoorService) : IGarageDoorOpenerEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("homebridge/garage_door_opener/", "").Replace("/targetDoorState", "");
        var message = e.ApplicationMessage.ConvertPayloadToString();
        var command = ConvertMessage(message);
        garageDoorService[name]?.SetToOn(command);
    }

    public static bool ConvertMessage(string message)
    {
        return message switch
        {
            "O" => true,
            "C" => false,
            _ => throw new Exception("Undefined value received from homebridge garage door opener"),
        };
    }
}