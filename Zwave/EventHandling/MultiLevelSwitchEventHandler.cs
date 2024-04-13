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

public interface IMultiLevelSwitchEventHandler
{
    const string MQTT_TOPIC = "zwave/+/+/38/+/currentValue";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

internal class MultiLevelSwitchEventHandler(ILogger<MultiLevelSwitchEventHandler> logger, IStateStore stateStorageService, IMultiLevelSwitchService multiLevelSwitchService) : IMultiLevelSwitchEventHandler
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

        //var expiryString = stateStorageService.GetState(name + "_multilevel");
        //if (ushort.TryParse(expiryString, out ushort expiry) && expiry == multiLevelSwitch.value)
        //{
        //    return;
        //}

        //var cooloff = stateStorageService.GetState(name + "_multilevel_cooloff");
        //if(cooloff != null)
        //{
        //    return;
        //}

        //stateStorageService.SetExpiringState(name + "_multilevel_cooloff", "", TimeSpan.FromSeconds(5));

        await (multiLevelSwitchService[name]?.SetLevel(payload.value) ?? Task.CompletedTask);
    }

    // Zwave Multilevel Switch only goes up to 99 (0 - 99), we want 100 for compatibility.
    private static ushort PadValueToHundred(ushort value)
    {
        return (ushort)(value == 99 ? 100 : value);
    }
}
