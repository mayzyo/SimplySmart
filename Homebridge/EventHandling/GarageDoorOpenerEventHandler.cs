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
    const string MQTT_TOPIC = "homebridge/garage_door_opener/+/+/+/+/setTargetDoorState";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

internal class GarageDoorOpenerEventHandler(IGarageDoorService garageDoorService) : IGarageDoorOpenerEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("homebridge/garage_door_opener/", "").Replace("/setTargetDoorState", "");
        var message = e.ApplicationMessage.ConvertPayloadToString();

        switch(message)
        {
            case "O":
                await (garageDoorService[name]?.Open() ?? Task.CompletedTask);
                return;
            case "C":
                await (garageDoorService[name]?.Close() ?? Task.CompletedTask);
                return;
        }
    }
}