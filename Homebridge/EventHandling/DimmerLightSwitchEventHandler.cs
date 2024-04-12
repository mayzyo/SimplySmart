using MQTTnet;
using MQTTnet.Client;
using SimplySmart.DeviceStates.Devices;
using SimplySmart.DeviceStates.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Homebridge.EventHandling;

public interface IDimmerLightSwitchEventHandler
{
    const string MQTT_TOPIC = "homebridge/light_switch/+/+/+/+/brightness";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

internal class DimmerLightSwitchEventHandler(ILightSwitchService lightSwitchService) : IDimmerLightSwitchEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("homebridge/light_switch/", "").Replace("/brightness", "");
        var message = e.ApplicationMessage.ConvertPayloadToString();
        var brightness = ushort.Parse(message);
        var dimmerSwitch = lightSwitchService[name];
        if(dimmerSwitch != null)
        {
            await ((IDimmerLightSwitch)dimmerSwitch).SetLevel(brightness);
        }
    }
}
