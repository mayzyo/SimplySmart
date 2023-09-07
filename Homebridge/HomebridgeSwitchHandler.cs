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

public interface IHomebridgeSwitchHandler
{
    Task HandleEvent(MqttApplicationMessageReceivedEventArgs e);

    Task HandleOn(string name);

    Task HandleOff(string name);
}

internal class HomebridgeSwitchHandler : IHomebridgeSwitchHandler
{
    private readonly ILogger<HomebridgeSwitchHandler> logger;
    private readonly IManagedMqttClient mqttClient;
    private readonly IHouseManager houseManager;

    public HomebridgeSwitchHandler(ILogger<HomebridgeSwitchHandler> logger, IManagedMqttClient mqttClient, IHouseManager houseManager)
    {
        this.logger = logger;
        this.mqttClient = mqttClient;
        this.houseManager = houseManager;
    }

    public async Task HandleOn(string name)
    {
        await mqttClient.EnqueueAsync($"homebridge/switch/{name}/getOn", "true");
    }

    public async Task HandleOff(string name)
    {
        await mqttClient.EnqueueAsync($"homebridge/switch/{name}/getOn", "false");
    }

    public async Task HandleEvent(MqttApplicationMessageReceivedEventArgs e)
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
            houseManager.AutoLight.Trigger(AutoLightCommand.ON);
        }
        else
        {
            houseManager.AutoLight.Trigger(AutoLightCommand.OFF);
        }
    }
}
