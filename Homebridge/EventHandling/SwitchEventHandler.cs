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

internal class SwitchEventHandler(ILogger<SwitchEventHandler> logger, IHouseService houseService) : ISwitchEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("homebridge/switch/", "").Replace("/setOn", "");
        var message = e.ApplicationMessage.ConvertPayloadToString();

        if (name == "auto_light")
        {
            var isOn = bool.Parse(message);
            ChangeAutoLightState(isOn);
        }
    }

    private void ChangeAutoLightState(bool isOn)
    {
        if (isOn)
        {
            houseService.AutoLight.Trigger(AutoLightCommand.ON);
        }
        else
        {
            houseService.AutoLight.Trigger(AutoLightCommand.OFF);
        }
    }
}
