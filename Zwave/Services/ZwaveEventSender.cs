using MQTTnet.Extensions.ManagedClient;
using SimplySmart.Zwave.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimplySmart.Zwave.Services;

public interface IZwaveEventSender
{
    Task BinarySwitchOn(string triggerUri);
    Task BinarySwitchOff(string triggerUri);
    Task MultiLevelSwitchUpdate(string triggerUri, ushort brightness);
}

internal class ZwaveEventSender(IManagedMqttClient mqttClient) : IZwaveEventSender
{
    public async Task BinarySwitchOff(string triggerUri)
    {
        var epoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var payload = JsonSerializer.Serialize(new BinarySwitch { Value = false, Time = epoch });
        await mqttClient.EnqueueAsync($"zwave/{triggerUri}/targetValue/set", payload);
    }

    public async Task BinarySwitchOn(string triggerUri)
    {
        var epoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var payload = JsonSerializer.Serialize(new BinarySwitch { Value = true, Time = epoch });
        await mqttClient.EnqueueAsync($"zwave/{triggerUri}/targetValue/set", payload);
    }

    public async Task MultiLevelSwitchUpdate(string triggerUri, ushort brightness)
    {
        var epoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var payload = JsonSerializer.Serialize(new MultilevelSwitch { Value = (ushort)(brightness == 100 ? 99 : brightness), Time = epoch });
        await mqttClient.EnqueueAsync($"zwave/{triggerUri}/targetValue/set", payload);
    }
}
