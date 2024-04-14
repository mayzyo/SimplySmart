using MQTTnet;
using MQTTnet.Client;
using SimplySmart.Homebridge.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Homebridge.EventHandling;

public interface ISwitchEventHandler
{
    const string MQTT_TOPIC = "homebridge/switch/+/+/+/+/setOn";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

internal class SwitchEventHandler(ISwitchService switchService) : ISwitchEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("homebridge/switch/", "").Replace("/setOn", "");
        var message = e.ApplicationMessage.ConvertPayloadToString();

        await (switchService[name]?.SetToOn(message == "true") ?? Task.CompletedTask);
    }
}
