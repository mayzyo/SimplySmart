using MQTTnet;
using MQTTnet.Client;
using SimplySmart.DeviceStates.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Homebridge.EventHandling;

public interface ILightSwitchEventHandler
{
    const string MQTT_TOPIC = "homebridge/light_switch/+/+/+/+";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

internal class LightSwitchEventHandler(ILightSwitchService lightSwitchService) : ILightSwitchEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("homebridge/light_switch/", "");
        var message = e.ApplicationMessage.ConvertPayloadToString();
        var isOn = bool.Parse(message);

        lightSwitchService[name]?.SetToOn(isOn);
    }
}
