using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using SimplySmart.Frigate.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using SimplySmart.HouseStates.Services;
using Microsoft.Extensions.Options;
using SimplySmart.Core.Services;
using SimplySmart.Core.Models;

namespace SimplySmart.Frigate.EventHandling;

public interface IFrigateEventHandler
{
    const string MQTT_TOPIC = "frigate/events";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

public class FrigateEventHandler(ILogger<FrigateEventHandler> logger, IOptions<ApplicationConfig> options, IHouseService houseService, IPassthroughEventSender passthroughEventSender) : IFrigateEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        if (topic != "frigate/events")
        {
            return;
        }

        if (options.Value.cameras == null)
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
        if (houseService.Security.State == HouseSecurityState.OFF)
        {
            return;
        }

        if (frigateEvent.type == "new")
        {
            if (frigateEvent.before?.label == "car")
            {
                await passthroughEventSender.CarAlertEvent(message);
            }
        }
        else if (frigateEvent.type == "end")
        {
            if (houseService.Security.State == HouseSecurityState.AWAY)
            {
                await passthroughEventSender.AlertEvent(message);
            }
            else if (options.Value.cameras.Where(e => e.isSurveillance).Any(e => e.name == frigateEvent.before?.camera) == true)
            {
                if (houseService.Security.State == HouseSecurityState.NIGHT)
                {
                    await passthroughEventSender.AlertEvent(message);
                }
                else
                {
                    frigateEvent.type = "outdoor";
                    await passthroughEventSender.AlertEvent(JsonSerializer.Serialize(frigateEvent));
                }
            }
        }
    }
}
