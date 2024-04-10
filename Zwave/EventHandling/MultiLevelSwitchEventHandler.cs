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

internal class MultiLevelSwitchEventHandler(ILogger<MultiLevelSwitchEventHandler> logger, IStateStorageService stateStorageService, IMultiLevelSwitchService multiLevelSwitchService) : IMultiLevelSwitchEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("zwave/", "").Replace("/currentValue", "");
        if (!e.ApplicationMessage.DeserialiseMessage(out MultilevelSwitch? multiLevelSwitch) || multiLevelSwitch == default)
        {
            logger.LogError("message not in JSON format.");
            return;
        }

        var expiryString = stateStorageService.GetState(name + "_zwave");
        if (ushort.TryParse(expiryString, out ushort expiry) && expiry == multiLevelSwitch.value)
        {
            return;
        }

        multiLevelSwitchService[name]?.SetLevel(multiLevelSwitch.value);
    }
}
