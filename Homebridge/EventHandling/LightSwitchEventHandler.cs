using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using SimplySmart.Core.Models;
using SimplySmart.DeviceStates.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Homebridge.EventHandling;

public interface ILightSwitchEventHandler
{
    const string MQTT_TOPIC = "homebridge/light_switch/#";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

internal class LightSwitchEventHandler(ILogger<LightSwitchEventHandler> logger, ILightSwitchService lightSwitchService) : ILightSwitchEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("homebridge/light_switch/", "");
        var message = e.ApplicationMessage.ConvertPayloadToString();

        if (MqttTopicFilterComparer.Compare(e.ApplicationMessage.Topic, "homebridge/light_switch/+/+/37/#") == MqttTopicFilterCompareResult.IsMatch)
        {
            var isOn = bool.Parse(message);
            ChangeLightSwitchState(name, isOn);
        }
        else
        {
            if (e.ApplicationMessage.Topic.Contains("brightness"))
            {
                name = name.Replace("/brightness", "");
            }

            if (!lightSwitchService.Exists(name))
            {
                return;
            }

            var dimmer = (IDimmerLightSwitch)lightSwitchService[name];
            ushort brightness;
            bool isOn = true;
            if (e.ApplicationMessage.Topic.Contains("brightness"))
            {
                brightness = ushort.Parse(message);
            }
            else
            {
                isOn = bool.Parse(message);

                if (dimmer.IsInState(LightSwitchState.ON) && isOn)
                {
                    return;
                }
                else if (dimmer.IsInState(LightSwitchState.OFF) && isOn == false)
                {
                    return;
                }

                brightness = (ushort)(isOn ? dimmer.Brightness != 0 ? dimmer.Brightness : 100 : dimmer.Brightness);
            }

            if (!isOn)
            {
                dimmer.Trigger(LightSwitchCommand.MANUAL_OFF, brightness, BroadcastSource.HOMEBRIDGE);
            }
            else
            {
                dimmer.Trigger(LightSwitchCommand.MANUAL_ON, brightness, BroadcastSource.HOMEBRIDGE);
            }
        }
    }

    private void ChangeLightSwitchState(string name, bool isOn)
    {
        if (lightSwitchService.Exists(name))
        {
            if (isOn)
            {
                lightSwitchService[name].Trigger(LightSwitchCommand.MANUAL_ON, BroadcastSource.HOMEBRIDGE);
            }
            else
            {
                lightSwitchService[name].Trigger(LightSwitchCommand.MANUAL_OFF, BroadcastSource.HOMEBRIDGE);
            }
        }
    }
}
