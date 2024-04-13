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

public interface IMultiLevelSwitchEventHandler
{
    const string MQTT_TOPIC = "zwave/+/+/38/+/currentValue";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

// Unused. The currentValue fluctuates randomly after setting the targetValue too rapidly.
// This is just too much effort for the time being and our physical switch for the dimmer don't really work anyways.
internal class MultiLevelSwitchEventHandler(ILogger<MultiLevelSwitchEventHandler> logger, IMultiLevelSwitchService multiLevelSwitchService) : IMultiLevelSwitchEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("zwave/", "").Replace("/currentValue", "");
        if (!e.ApplicationMessage.DeserialiseMessage(out MultilevelSwitch? payload) || payload == default)
        {
            logger.LogError("message not in JSON format.");
            return;
        }
        payload.value = PadValueToHundred(payload.value);

        await (multiLevelSwitchService[name]?.SetLevel(payload.value) ?? Task.CompletedTask);
    }

    // Zwave Multilevel Switch only goes up to 99 (0 - 99), we want 100 for compatibility.
    private static ushort PadValueToHundred(ushort value)
    {
        return (ushort)(value == 99 ? 100 : value);
    }
}
