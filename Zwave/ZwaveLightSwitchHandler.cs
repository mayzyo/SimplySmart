using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using SimplySmart.Frigate;
using SimplySmart.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SimplySmart.Zwave;

public interface IZwaveLightSwitchHandler
{
    Task HandleEvent(MqttApplicationMessageReceivedEventArgs e);
    Task HandleOn(string triggerUri);
    Task HandleOff(string triggerUri);
}

internal class ZwaveLightSwitchHandler : IZwaveLightSwitchHandler
{
    private readonly ILogger<ZwaveLightSwitchHandler> logger;
    private readonly ILightSwitchManager lightSwitchManager;
    private readonly IManagedMqttClient mqttClient;

    public ZwaveLightSwitchHandler(ILogger<ZwaveLightSwitchHandler> logger, IManagedMqttClient mqttClient, ILightSwitchManager lightSwitchManager)
    {
        this.logger = logger;
        this.lightSwitchManager = lightSwitchManager;
        this.mqttClient = mqttClient;
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
        if(!lightSwitchManager.Exists(name))
        {
            return;
        }

        var message = e.ApplicationMessage.ConvertPayloadToString();
        var binarySwitch = DeserialiseMessage(message);
        if (binarySwitch == null)
        {
            logger.LogError("message JSON was empty");
            return;
        }

        ChangeLightSwitchState(name, binarySwitch.value);
    }

    private void ChangeLightSwitchState(string name, bool isOn)
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

    private BinarySwitch? DeserialiseMessage(string message)
    {
        try
        {
            return JsonSerializer.Deserialize<BinarySwitch>(message);
        }
        catch
        {
            logger.LogError("message not in JSON format.");
        }

        return null;
    }
}
