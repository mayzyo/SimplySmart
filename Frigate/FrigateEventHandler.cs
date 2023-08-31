using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using SimpleFrigateSorter.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using YamlDotNet.Serialization;

namespace SimpleFrigateSorter.Frigate;

public interface IFrigateEventHandler
{
    Task HandleEvent(MqttApplicationMessageReceivedEventArgs e);
}

public class FrigateEventHandler : IFrigateEventHandler
{
    private readonly ILogger<FrigateEventHandler> logger;
    private readonly IManagedMqttClient mqttClient;
    private static ApplicationConfig? applicationConfig;

    public FrigateEventHandler(ILogger<FrigateEventHandler> logger, IDeserializer deserializer, IManagedMqttClient mqttClient)
    {
        this.logger = logger;
        this.mqttClient = mqttClient;

        if (applicationConfig == null)
        {
            var path = Environment.GetEnvironmentVariable("CONFIG_FILE_PATH") ?? throw new Exception("Config file missing!");
            using var sr = File.OpenText(path);
            applicationConfig = deserializer.Deserialize<ApplicationConfig>(sr);
        }
    }

    public async Task HandleEvent(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        if (topic != "frigate/events")
        {
            return;
        }

        var message = e.ApplicationMessage.ConvertPayloadToString();
        var frigateEvent = DeserialiseEvent(message);
        if (frigateEvent == null)
        {
            logger.LogError("message JSON was empty");
            return;
        }

        await TriggerOutdoor(frigateEvent, message);
    }

    private FrigateEvent? DeserialiseEvent(string message)
    {
        try
        {
            return JsonSerializer.Deserialize<FrigateEvent>(message);
        }
        catch
        {
            logger.LogError("message not in JSON format.");
        }

        return null;
    }

    private async Task TriggerOutdoor(FrigateEvent frigateEvent, string message)
    {
        if (frigateEvent.type == "new")
        {
            if (frigateEvent.before?.label == "car")
            {
                await mqttClient.EnqueueAsync("simply_smart/car", message);
            }
        }
        else if (frigateEvent.type == "end")
        {
            if (applicationConfig?.surveillances.Any(e => e.name == frigateEvent.before?.camera) == true)
            {
                await mqttClient.EnqueueAsync("simply_smart/outdoor", message);
            }
        }
    }
}
