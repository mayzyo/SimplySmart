using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Nodemation.Services;

public interface INodemationEventSender
{
    Task QueryGarageDoor(string triggerUri);
}

internal class NodemationEventSender(IManagedMqttClient mqttClient) : INodemationEventSender
{
    public async Task QueryGarageDoor(string triggerUri)
    {
        await mqttClient.EnqueueAsync($"nodemation/garageDoor/{triggerUri}");
    }
}
