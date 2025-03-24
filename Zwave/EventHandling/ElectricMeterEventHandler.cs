using Microsoft.Extensions.Logging;
using MQTTnet.Client;
using SimplySmart.Core.Extensions;
using SimplySmart.Zwave.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimplySmart.Zwave.EventHandling;

public interface IElectricMeterEventHandler
{
    const string MQTT_TOPIC = "zwave/+/+/50/0/value/66049";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

internal class ElectricMeterEventHandler(ILogger<IElectricMeterEventHandler> logger) : IElectricMeterEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("zwave/", "");
        if (!e.ApplicationMessage.DeserialiseMessage(out ElectricConsumption? payload) || payload == default)
        {
            logger.LogError("message not in JSON format.");
            return;
        }
    }
}

