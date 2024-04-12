using Microsoft.Extensions.Logging;
using MQTTnet.Client;
using SimplySmart.Core.Abstractions;
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

internal class BinarySwitchEventHandler(ILogger<IBinarySwitchEventHandler> logger, IStateStore stateStorageService, IBinarySwitchService binarySwitchService) : IBinarySwitchEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("zwave/", "").Replace("/currentValue", "");
        if (!e.ApplicationMessage.DeserialiseMessage(out BinarySwitch? binarySwitch) || binarySwitch == default)
        {
            logger.LogError("message not in JSON format.");
            return;
        }

        var expiry = stateStorageService.GetState(name + "_binary");
        if(expiry != null && (expiry == "true") == binarySwitch.value)
        {
            return;
        }

        binarySwitchService[name]?.SetToOn(binarySwitch.value);
    }
}
