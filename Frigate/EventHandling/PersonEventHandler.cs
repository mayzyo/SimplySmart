using MQTTnet;
using MQTTnet.Client;
using SimplySmart.HouseStates.Services;
using System;
using System.Collections.Generic;

namespace SimplySmart.Frigate.EventHandling;

public interface IPersonEventHandler
{
    const string MQTT_TOPIC = "frigate/+/person";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

internal class PersonEventHandler(IAreaOccupantService areaOccupantService) : IPersonEventHandler
{
    public Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var areaName = e.ApplicationMessage.Topic.Split("/")[1];
        var message = e.ApplicationMessage.ConvertPayloadToString();
        var count = int.Parse(message);

        areaOccupantService[areaName]?.SetMoving(count != 0);
        return Task.CompletedTask;
    }
}
