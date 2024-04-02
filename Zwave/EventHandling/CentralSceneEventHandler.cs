using Microsoft.Extensions.Logging;
using MQTTnet.Client;
using SimplySmart.DeviceStates.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SimplySmart.Zwave.EventHandling;

public interface ICentralSceneEventHandler
{
    const string MQTT_TOPIC = "zwave/+/91/0/scene/+";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}
// Just the FOB at the moment.
internal class CentralSceneEventHandler(ILogger<CentralSceneEventHandler> logger, IFobService fobManager) : ICentralSceneEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = Regex.Replace(e.ApplicationMessage.Topic.Replace("zwave/", ""), "\\/scene\\/\\d+", "");
        var command = e.ApplicationMessage.Topic.Split("/")[^1];

        if (fobManager.Exists(name))
        {
            UpdateFob(name, command);
        }
    }

    private void UpdateFob(string name, string command)
    {
        logger.LogInformation("Fob command received");
        fobManager[name].Trigger(command);
    }
}
