using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using SimplySmart.HouseStates.Areas;
using SimplySmart.HouseStates.Services;
using System;
using System.Collections.Generic;

namespace SimplySmart.Frigate.EventHandling;

public interface IPersonEventHandler
{
    const string MQTT_TOPIC = "frigate/+/person";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

internal class PersonEventHandler(ILogger<PersonEventHandler> logger, IAreaOccupantService areaOccupantService) : IPersonEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var areaName = e.ApplicationMessage.Topic.Split("/")[1];
        var message = e.ApplicationMessage.ConvertPayloadToString();
        var count = int.Parse(message);

        ChangeOccupantState(areaName, count);
    }

    private void ChangeOccupantState(string areaName, int count)
    {
        if (areaOccupantService.Exists(areaName))
        {
            var command = count != 0 ? AreaOccupantCommand.SET_MOVING : AreaOccupantCommand.SET_EMPTY;
            areaOccupantService[areaName].Trigger(command);
        }
    }
}
