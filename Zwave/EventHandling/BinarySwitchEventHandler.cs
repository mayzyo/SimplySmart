using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using SimplySmart.Core.Models;
using SimplySmart.DeviceStates.Services;
using SimplySmart.Zwave.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SimplySmart.Zwave.EventHandling;

public interface IBinarySwitchEventHandler
{
    const string MQTT_TOPIC = "zwave/+/+/37/+/currentValue";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

internal class BinarySwitchEventHandler(ILogger<BinarySwitchEventHandler> logger, ILightSwitchService lightSwitchManager) : IBinarySwitchEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("zwave/", "").Replace("/currentValue", "");

        if (lightSwitchManager.Exists(name))
        {
            var message = e.ApplicationMessage.ConvertPayloadToString();
            UpdateLightSwitch(name, message);
        }
    }

    private void UpdateLightSwitch(string name, string message)
    {
        var binarySwitch = DeserialiseMessage<BinarySwitch>(message);
        if (binarySwitch == default)
        {
            logger.LogError("message JSON was empty");
            return;
        }

        if (binarySwitch.value == true)
        {
            lightSwitchManager[name].Trigger(LightSwitchCommand.MANUAL_ON, BroadcastSource.ZWAVE);
        }
        else
        {
            lightSwitchManager[name].Trigger(LightSwitchCommand.MANUAL_OFF, BroadcastSource.ZWAVE);
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
