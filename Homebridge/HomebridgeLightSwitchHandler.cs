using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using SimplySmart.States;
using SimplySmart.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Homebridge;

public interface IHomebridgeLightSwitchHandler
{
    Task HandleEvent(MqttApplicationMessageReceivedEventArgs e);
    Task HandleOn(string triggerUri);
    Task HandleOn(string triggerUri, ushort brightness);
    Task HandleOff(string triggerUri);
}

internal class HomebridgeLightSwitchHandler : IHomebridgeLightSwitchHandler
{
    private readonly ILogger<HomebridgeLightSwitchHandler> logger;
    private readonly ILightSwitchManager lightSwitchManager;
    private readonly IManagedMqttClient mqttClient;

    public HomebridgeLightSwitchHandler(ILogger<HomebridgeLightSwitchHandler> logger, IManagedMqttClient mqttClient, ILightSwitchManager lightSwitchManager)
    {
        this.logger = logger;
        this.lightSwitchManager = lightSwitchManager;
        this.mqttClient = mqttClient;
    }

    public async Task HandleOff(string triggerUri)
    {
        await mqttClient.EnqueueAsync($"homebridge/light_switch/{triggerUri}/set", "false");
    }

    public async Task HandleOn(string triggerUri)
    {
        await mqttClient.EnqueueAsync($"homebridge/light_switch/{triggerUri}/set", "true");
    }

    public async Task HandleOn(string triggerUri, ushort brightness)
    {
        await mqttClient.EnqueueAsync($"homebridge/light_switch/{triggerUri}/brightness/set", brightness.ToString());
    }

    public async Task HandleEvent(MqttApplicationMessageReceivedEventArgs e)
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

            if (!lightSwitchManager.Exists(name))
            {
                return;
            }

            var dimmer = (IDimmerLightSwitch)lightSwitchManager[name];
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
        if (lightSwitchManager.Exists(name))
        {
            if (isOn)
            {
                lightSwitchManager[name].Trigger(LightSwitchCommand.MANUAL_ON, BroadcastSource.HOMEBRIDGE);
            }
            else
            {
                lightSwitchManager[name].Trigger(LightSwitchCommand.MANUAL_OFF, BroadcastSource.HOMEBRIDGE);
            }
        }
    }
}
