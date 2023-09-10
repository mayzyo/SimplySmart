using Microsoft.Extensions.Logging;
using MQTTnet.Client;
using SimplySmart.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SimplySmart.Zwave;

public interface IZwaveCentralSceneHandler
{
    const string MQTT_TOPIC = "zwave/+/91/0/scene/+";

    Task HandleEvent(MqttApplicationMessageReceivedEventArgs e);
}

internal class ZwaveCentralSceneHandler : IZwaveCentralSceneHandler
{
    private readonly ILogger<ZwaveCentralSceneHandler> logger;
    private readonly IFobManager fobManager;

    public ZwaveCentralSceneHandler(ILogger<ZwaveCentralSceneHandler> logger, IFobManager fobManager)
    {
        this.logger = logger;
        this.fobManager = fobManager;
    }

    public async Task HandleEvent(MqttApplicationMessageReceivedEventArgs e)
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
