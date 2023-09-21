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

public interface IHomebridgeFanHandler
{
    public const string MQTT_TOPIC = "homebridge/fan/+/+/+/+/on";

    Task HandleEvent(MqttApplicationMessageReceivedEventArgs e);

    Task HandleOn(string name);

    Task HandleOff(string name);
}

internal class HomebridgeFanHandler : IHomebridgeFanHandler
{
    private readonly ILogger<HomebridgeFanHandler> logger;
    private readonly IManagedMqttClient mqttClient;
    private readonly IApplianceManager applianceManager;

    public HomebridgeFanHandler(ILogger<HomebridgeFanHandler> logger, IManagedMqttClient mqttClient, IApplianceManager applianceManager)
    {
        this.logger = logger;
        this.mqttClient = mqttClient;
        this.applianceManager = applianceManager;
    }

    public async Task HandleOn(string name)
    {
        await mqttClient.EnqueueAsync($"homebridge/fan/{name}/on/set", "true");
    }

    public async Task HandleOff(string name)
    {
        await mqttClient.EnqueueAsync($"homebridge/fan/{name}/on/set", "false");
    }

    public async Task HandleEvent(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("homebridge/fan/", "").Replace("/on", "");
        var message = e.ApplicationMessage.ConvertPayloadToString();

        await UpdateState(name, message);
    }

    private async Task UpdateState(string name, string message)
    {
        var command = message == "true" ? ApplianceCommand.ON : ApplianceCommand.OFF;
        var fan = (IFan)applianceManager[name];
        await fan.Trigger(command);
    }
}
