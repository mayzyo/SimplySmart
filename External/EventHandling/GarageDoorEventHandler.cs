using Microsoft.Extensions.Logging;
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
    const string MQTT_TOPIC = "simply_smart/garageDoor/closed/#";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

 // Unused. Waiting for classifier model.
internal class GarageDoorEventHandler(ILogger<IGarageDoorEventHandler> logger, IGarageDoorService garageDoorService) : IGarageDoorEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("simply_smart/garageDoor/closed/", "");
        var message = e.ApplicationMessage.ConvertPayloadToString();
        if(TryMessageCleanup(message, out bool isClosed))
        {
            await (garageDoorService[name]?.StateVerified(isClosed) ?? Task.CompletedTask);
        }
    }

    bool TryMessageCleanup(string message, out bool isClosed)
    {
        if(message.Contains("true", StringComparison.CurrentCultureIgnoreCase))
        {
            isClosed = true;
            return true;
        }
        else if(message.Contains("false", StringComparison.CurrentCultureIgnoreCase))
        {
            isClosed = false;
            return true;
        }

        logger.LogError($"Message from Open AI didn't contain boolean. {message}");
        isClosed = default;
        return false;
    }
}
