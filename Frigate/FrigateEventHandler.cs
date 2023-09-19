using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using SimplySmart.States;
using SimplySmart.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using YamlDotNet.Serialization;

namespace SimplySmart.Frigate;

public interface IFrigateEventHandler
{
    Task HandleEvent(MqttApplicationMessageReceivedEventArgs e);
}

public class FrigateEventHandler : IFrigateEventHandler
{
    private readonly ILogger<FrigateEventHandler> logger;
    private readonly IManagedMqttClient mqttClient;
    private readonly IHouseManager houseManager;
    private static ApplicationConfig? applicationConfig;

    public FrigateEventHandler(ILogger<FrigateEventHandler> logger, IDeserializer deserializer, IManagedMqttClient mqttClient, IHouseManager houseManager)
    {
        this.logger = logger;
        this.mqttClient = mqttClient;
        this.houseManager = houseManager;

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

        if (applicationConfig?.cameras == null)
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

        await PassthroughEvents(frigateEvent, message);
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

    private async Task PassthroughEvents(FrigateEvent frigateEvent, string message)
    {
        if(houseManager.Security.State == HouseSecurityState.OFF)
        {
            return;
        }

        if (frigateEvent.type == "new")
        {
            if (frigateEvent.before?.label == "car")
            {
                await mqttClient.EnqueueAsync("simply_smart/house_security/car", message);
            }
        }
        else if (frigateEvent.type == "end")
        {
            if (houseManager.Security.State == HouseSecurityState.AWAY)
            {
                await mqttClient.EnqueueAsync("simply_smart/house_security/alert", message);
            }
            else if(applicationConfig?.cameras.Where(e => e.isSurveillance).Any(e => e.name == frigateEvent.before?.camera) == true)
            {
                if(houseManager.Security.State == HouseSecurityState.NIGHT)
                {
                    await mqttClient.EnqueueAsync("simply_smart/house_security/alert", message);
                }
                else
                {
                    frigateEvent.type = "outdoor";
                    await mqttClient.EnqueueAsync("simply_smart/house_security/alert", JsonSerializer.Serialize(frigateEvent));

                }
            }
        }
    }
}
