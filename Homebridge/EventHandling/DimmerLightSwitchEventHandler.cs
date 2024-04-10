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
    const string MQTT_TOPIC = "homebridge/light_switch/+/+/38/#";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

internal class DimmerLightSwitchEventHandler(ILightSwitchService lightSwitchService) : IDimmerLightSwitchEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("homebridge/light_switch/", "");
        var message = e.ApplicationMessage.ConvertPayloadToString();
        var hasBrightness = e.ApplicationMessage.Topic.Contains("brightness");
        ChangeDimmerLightSwitchState(name, message, hasBrightness);
    }

    void ChangeDimmerLightSwitchState(string name, string message, bool hasBrightness)
    {
        if (hasBrightness)
        {
            name = name.Replace("/brightness", "");
            var brightness = ushort.Parse(message);
            var dimmerSwitch = (IDimmerLightSwitch)lightSwitchService[name];
            dimmerSwitch?.SetLevel(brightness);
        }
        else
        {
            var isOn = bool.Parse(message);
            var dimmerSwitch = (IDimmerLightSwitch)lightSwitchService[name];
            dimmerSwitch?.SetToOn(isOn);
        }
    }
}
