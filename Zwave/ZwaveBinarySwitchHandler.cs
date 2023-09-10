using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using SimplySmart.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SimplySmart.Zwave;

public interface IZwaveBinarySwitchHandler
{
    const string MQTT_TOPIC = "zwave/+/+/37/+/currentValue";

    Task HandleEvent(MqttApplicationMessageReceivedEventArgs e);
    Task HandleOn(string triggerUri);
    Task HandleOff(string triggerUri);
}

internal class ZwaveBinarySwitchHandler : IZwaveBinarySwitchHandler
{
    private readonly ILogger<ZwaveBinarySwitchHandler> logger;
    private readonly ILightSwitchManager lightSwitchManager;
    private readonly IManagedMqttClient mqttClient;

    public ZwaveBinarySwitchHandler(
        ILogger<ZwaveBinarySwitchHandler> logger,
        IManagedMqttClient mqttClient,
        ILightSwitchManager lightSwitchManager
        )
    {
        this.logger = logger;
        this.mqttClient = mqttClient;
        this.lightSwitchManager = lightSwitchManager;
    }

    public async Task HandleOff(string triggerUri)
    {
        var epoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var payload = JsonSerializer.Serialize(new BinarySwitch { value = false, time = epoch });
        await mqttClient.EnqueueAsync($"zwave/{triggerUri}/targetValue/set", payload);
    }

    public async Task HandleOn(string triggerUri)
    {
        var epoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var payload = JsonSerializer.Serialize(new BinarySwitch { value = true, time = epoch });
        await mqttClient.EnqueueAsync($"zwave/{triggerUri}/targetValue/set", payload);
    }

    public async Task HandleEvent(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("zwave/", "").Replace("/currentValue", "");

        if(lightSwitchManager.Exists(name))
        {
            var message = e.ApplicationMessage.ConvertPayloadToString();
            UpdateLightSwitch(name, message);
        }
    }

    private void UpdateLightSwitch(string name, string message)
    {
        var binarySwitch = DeserialiseMessage<BinarySwitch>(message);
        if (binarySwitch == default)
        {
            logger.LogError("message JSON was empty");
            return;
        }

        if (binarySwitch.value == true)
        {
            lightSwitchManager[name].Trigger(LightSwitchCommand.MANUAL_ON);
        }
        else
        {
            lightSwitchManager[name].Trigger(LightSwitchCommand.MANUAL_OFF);
        }
    }

    private T? DeserialiseMessage<T>(string message)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(message);
        }
        catch
        {
            logger.LogError("message not in JSON format.");
        }

        return default;
    }
}
