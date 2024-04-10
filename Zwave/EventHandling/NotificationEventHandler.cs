using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using SimplySmart.Core.Extensions;
using SimplySmart.HouseStates.Areas;
using SimplySmart.HouseStates.Services;
using SimplySmart.Zwave.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SimplySmart.Zwave.EventHandling;

public interface INotificationEventHandler
{
    const string MQTT_TOPIC = "zwave/+/+/113/0/#";
    Task HandleEvent(MqttApplicationMessageReceivedEventArgs e);
}

internal class NotificationEventHandler(ILogger<NotificationEventHandler> logger, IAreaOccupantService areaOccupantService) : INotificationEventHandler
{
    public async Task HandleEvent(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("zwave/", "");
        if (areaOccupantService.Exists(name))
        {
            if (!e.ApplicationMessage.DeserialiseMessage(out Payload? payload) || payload == default)
            {
                logger.LogError("message not in JSON format.");
                return;
            }

            UpdateMultiSensor(name, payload);
        }
    }

    void UpdateMultiSensor(string name, Payload payload)
    {
        var multiSensor = areaOccupantService[name];
        var command = payload.value == 0 ? AreaOccupantCommand.SET_EMPTY : AreaOccupantCommand.SET_MOVING;
        multiSensor.Trigger(command);
    }
}
