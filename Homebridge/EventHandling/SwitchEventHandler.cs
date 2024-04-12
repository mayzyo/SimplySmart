using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using SimplySmart.HouseStates.Features;
using SimplySmart.HouseStates.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Homebridge.EventHandling;

public interface ISwitchEventHandler
{
    const string MQTT_TOPIC = "homebridge/switch/+/setOn";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

internal class SwitchEventHandler(ILogger<ISwitchEventHandler> logger, IHouseService houseService) : ISwitchEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("homebridge/switch/", "").Replace("/setOn", "");
        var message = e.ApplicationMessage.ConvertPayloadToString();

        if (name == "auto_light")
        {
            logger.LogInformation("auto light triggered");
            var isOn = bool.Parse(message);
            await houseService.AutoLight.Trigger(isOn ? AutoLightCommand.ON : AutoLightCommand.OFF);
        }
    }
}
