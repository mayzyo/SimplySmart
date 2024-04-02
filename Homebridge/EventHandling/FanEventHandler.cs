using MQTTnet;
using MQTTnet.Client;
using SimplySmart.DeviceStates.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Homebridge.EventHandling;

public interface IFanEventHandler
{
    const string MQTT_TOPIC = "homebridge/fan/+/+/+/+/on";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

internal class FanEventHandler(IApplianceService applianceService) : IFanEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("homebridge/fan/", "").Replace("/on", "");
        var message = e.ApplicationMessage.ConvertPayloadToString();

        await UpdateState(name, message);
    }

    private async Task UpdateState(string name, string message)
    {
        var command = message == "true" ? ApplianceCommand.ON : ApplianceCommand.OFF;
        var fan = (IFan)applianceService[name];
        await fan.Trigger(command);
    }
}
