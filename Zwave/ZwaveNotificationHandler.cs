using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using SimplySmart.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SimplySmart.Zwave;

public interface IZwaveNotificationHandler
{
    const string MQTT_TOPIC = "zwave/+/+/113/0/#";

    Task HandleEvent(MqttApplicationMessageReceivedEventArgs e);
}

internal class ZwaveNotificationHandler : IZwaveNotificationHandler
{
    private readonly ILogger<ZwaveNotificationHandler> logger;
    private readonly IAreaOccupantManager areaOccupantManager;

    public ZwaveNotificationHandler(ILogger<ZwaveNotificationHandler> logger, IAreaOccupantManager areaOccupantManager)
    {
        this.logger = logger;
        this.areaOccupantManager = areaOccupantManager;
    }

    public async Task HandleEvent(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("zwave/", "");
        if(areaOccupantManager.Exists(name))
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
