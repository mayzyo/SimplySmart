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

public interface IMultiLevelSwitchEventHandler
{
    const string MQTT_TOPIC = "zwave/+/+/38/+/currentValue";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

internal class MultiLevelSwitchEventHandler(ILogger<MultiLevelSwitchEventHandler> logger, ILightSwitchService lightSwitchService) : IMultiLevelSwitchEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("zwave/", "").Replace("/currentValue", "");
        if (lightSwitchService.Exists(name))
        {
            var message = e.ApplicationMessage.ConvertPayloadToString();
            UpdateDimmerLightSwitch(name, message);
        }
    }

    private void UpdateDimmerLightSwitch(string name, string message)
    {
        var dimmer = (IDimmerLightSwitch)lightSwitchService[name];
        var dimmerSwitch = DeserialiseMessage<MultilevelSwitch>(message);
        if (dimmerSwitch == default)
        {
            logger.LogError("message JSON was empty");
            return;
        }

        dimmerSwitch.value = (ushort)(dimmerSwitch.value == 99 ? 100 : dimmerSwitch.value);

        if (dimmerSwitch.value == 0)
        {
            dimmer.Trigger(LightSwitchCommand.MANUAL_OFF, BroadcastSource.ZWAVE);
        }
        else if (dimmer.IsInState(LightSwitchState.OFF) || dimmerSwitch.value != dimmer.Brightness)
        {
            dimmer.Trigger(LightSwitchCommand.MANUAL_ON, dimmerSwitch.value, BroadcastSource.ZWAVE);
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
