using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Core.Services;

public interface IPassthroughEventSender
{
    Task CarAlertEvent(string message);
    Task AlertEvent(string message);
}

internal class PassthroughEventSender(IManagedMqttClient mqttClient) : IPassthroughEventSender
{
    public async Task CarAlertEvent(string message)
    {
        await mqttClient.EnqueueAsync("simply_smart/house_security/car", message);
    }

    public async Task AlertEvent(string message)
    {
        await mqttClient.EnqueueAsync("simply_smart/house_security/alert", message);
    }
}
