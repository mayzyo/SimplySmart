using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using SimplySmart.States;
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

    public async Task HandleEvent(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("homebridge/light_switch/", "");
        var message = e.ApplicationMessage.ConvertPayloadToString();
        var isOn = bool.Parse(message);

        ChangeLightSwitchState(name, isOn);
    }

    private void ChangeLightSwitchState(string name, bool isOn)
    {
        if (lightSwitchManager.Exists(name))
        {
            if (isOn)
            {
                lightSwitchManager[name].Trigger(LightSwitchCommand.MANUAL_ON);
            }
            else
            {
                lightSwitchManager[name].Trigger(LightSwitchCommand.MANUAL_OFF);
            }
        }
    }
}
