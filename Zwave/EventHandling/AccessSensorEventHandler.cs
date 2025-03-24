using Microsoft.Extensions.Logging;
using MQTTnet.Client;
using SimplySmart.Core.Extensions;
using SimplySmart.Zwave.Models;
using SimplySmart.Zwave.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimplySmart.Zwave.EventHandling;

public interface IAccessSensorEventHandler
{
    const string MQTT_TOPIC = "zwave/+/+/113/0/Access_Control/Door_state";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

internal class AccessSensorEventHandler(ILogger<IAccessSensorEventHandler> logger, IAccessSensorService accessSensorService) : IAccessSensorEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("zwave/", "");
        if (!e.ApplicationMessage.DeserialiseMessage(out Payload? payload) || payload == default)
        {
            logger.LogError("message not in JSON format.");
            return;
        }
        // 22 the moment it can't detect the other side. 23 when it detects it.
        await (accessSensorService[name]?.HandleContactChange(payload.value == 23) ?? Task.CompletedTask);
    }
}
