using MQTTnet;
using MQTTnet.Client;
using SimplySmart.DeviceStates.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Nodemation.EventHandling;

public interface IGarageDoorEventHandler
{
    const string MQTT_TOPIC = "nodemation/garageDoor/closed/+";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

internal class GarageDoorEventHandler(IGarageDoorService garageDoorService) : IGarageDoorEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("nodemation/garageDoor/closed/", "");
        var message = e.ApplicationMessage.ConvertPayloadToString();
        UpdateState(name, message);
    }

    void UpdateState(string name, string message)
    {
        var garageDoor = garageDoorService[name];
        if(message == "true")
        {
            garageDoor.CloseVerified();
        }
        else
        {
            garageDoor.OpenVerified();
        }
    }
}
