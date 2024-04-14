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
    const string MQTT_TOPIC = "homebridge/fan/+/+/+/+/setOn";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

internal class FanEventHandler(IFanService fanService) : IFanEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("homebridge/fan/", "").Replace("/setOn", "");
        var message = e.ApplicationMessage.ConvertPayloadToString();

        await (fanService[name]?.SetToOn(message == "true") ?? Task.CompletedTask);
    }
}
