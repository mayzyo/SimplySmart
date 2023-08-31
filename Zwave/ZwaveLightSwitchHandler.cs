using Microsoft.Extensions.Logging;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SimpleFrigateSorter.Zwave;

public interface IZwaveLightSwitchHandler
{
    Task HandleOn(string triggerUri);

    Task HandleOff(string triggerUri);
}

internal class ZwaveLightSwitchHandler : IZwaveLightSwitchHandler
{
    private readonly ILogger<ZwaveLightSwitchHandler> logger;
    private readonly IManagedMqttClient mqttClient;

    public ZwaveLightSwitchHandler(ILogger<ZwaveLightSwitchHandler> logger, IManagedMqttClient mqttClient)
    {
        this.logger = logger;
        this.mqttClient = mqttClient;
    }

    public async Task HandleOff(string triggerUri)
    {
        var epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds() * 1000;
        var payload = JsonSerializer.Serialize(new BinarySwitch { value = false, time = epoch });
        await mqttClient.EnqueueAsync($"zwave/{triggerUri}/targetValue/set", payload);
    }

    public async Task HandleOn(string triggerUri)
    {
        var epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds() * 1000;
        var payload = JsonSerializer.Serialize(new BinarySwitch { value = true, time = epoch });
        await mqttClient.EnqueueAsync($"zwave/{triggerUri}/targetValue/set", payload);
    }
}
