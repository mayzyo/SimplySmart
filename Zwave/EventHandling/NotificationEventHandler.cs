using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
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

internal class NotificationEventHandler : INotificationEventHandler
{
    private readonly ILogger<NotificationEventHandler> logger;
    private readonly IAreaOccupantService areaOccupantManager;

    public NotificationEventHandler(ILogger<NotificationEventHandler> logger, IAreaOccupantService areaOccupantManager)
    {
        this.logger = logger;
        this.areaOccupantManager = areaOccupantManager;
    }

    public async Task HandleEvent(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("zwave/", "");
        if (areaOccupantManager.Exists(name))
        {
            var message = e.ApplicationMessage.ConvertPayloadToString();
            UpdateMultiSensor(name, message);
        }
    }

    private void UpdateMultiSensor(string name, string message)
    {
        var multiSensor = areaOccupantManager[name];
        var payload = DeserialiseMessage<Payload>(message);
        if (payload == default)
        {
            logger.LogError("message JSON was empty");
            return;
        }

        if (payload.value == 0)
        {
            multiSensor.Trigger(AreaOccupantCommand.SET_EMPTY);
        }
        else
        {
            multiSensor.Trigger(AreaOccupantCommand.SET_MOVING);
        }
    }

    private T? DeserialiseMessage<T>(string message)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(message);
        }
        catch
        {
            logger.LogError("message not in JSON format.");
        }

        return default;
    }
}
