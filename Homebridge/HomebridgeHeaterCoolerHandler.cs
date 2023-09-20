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

public interface IHomebridgeHeaterCoolerHandler
{
    public const string MQTT_TOPIC = "homebridge/heater_cooler/+/+/+/+/active";

    Task HandleEvent(MqttApplicationMessageReceivedEventArgs e);

    Task HandleOn(string name);

    Task HandleOff(string name);
}

internal class HomebridgeHeaterCoolerHandler : IHomebridgeHeaterCoolerHandler
{
    private readonly ILogger<HomebridgeHeaterCoolerHandler> logger;
    private readonly IManagedMqttClient mqttClient;
    private readonly IApplianceManager applianceManager;

    public HomebridgeHeaterCoolerHandler(ILogger<HomebridgeHeaterCoolerHandler> logger, IManagedMqttClient mqttClient, IApplianceManager applianceManager)
    {
        this.logger = logger;
        this.mqttClient = mqttClient;
        this.applianceManager = applianceManager;
    }

    public async Task HandleOn(string name)
    {
        await mqttClient.EnqueueAsync($"homebridge/heater_cooler/{name}/active/set", "true");
    }

    public async Task HandleOff(string name)
    {
        await mqttClient.EnqueueAsync($"homebridge/heater_cooler/{name}/active/set", "false");
    }

    public async Task HandleEvent(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("homebridge/heater_cooler/", "").Replace("/active", "");
        var message = e.ApplicationMessage.ConvertPayloadToString();

        await UpdateState(name, message);
    }

    private async Task UpdateState(string name, string message)
    {
        var command = message == "true" ? ApplianceCommand.ON : ApplianceCommand.OFF;
        var electricBlanket = (IElectricBlanket)applianceManager[name];
        await electricBlanket.Trigger(command);
    }
}
