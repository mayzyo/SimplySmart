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

public interface IBinarySwitchEventHandler
{
    const string MQTT_TOPIC = "zwave/+/+/37/+/currentValue";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

internal class BinarySwitchEventHandler(ILogger<IBinarySwitchEventHandler> logger, IBinarySwitchService binarySwitchService) : IBinarySwitchEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("zwave/", "").Replace("/currentValue", "");
        if (!e.ApplicationMessage.DeserialiseMessage(out BinarySwitch? payload) || payload == default)
        {
            logger.LogError("message not in JSON format.");
            return;
        }

        await (binarySwitchService[name]?.SetToOn(payload.value) ?? Task.CompletedTask);
    }
}
