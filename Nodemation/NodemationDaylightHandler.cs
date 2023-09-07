using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using SimplySmart.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimplySmart.Nodemation;

public interface INodemationDaylightHandler
{
    Task HandleEvent(MqttApplicationMessageReceivedEventArgs e);
}

internal class NodemationDaylightHandler : INodemationDaylightHandler
{
    private readonly ILogger<NodemationDaylightHandler> logger;
    private readonly IHouseManager houseManager;

    public NodemationDaylightHandler(ILogger<NodemationDaylightHandler> logger, IHouseManager houseManager)
    {
        this.logger = logger;
        this.houseManager = houseManager;
    }

    public async Task HandleEvent(MqttApplicationMessageReceivedEventArgs e)
    {
        var message = e.ApplicationMessage.ConvertPayloadToString();
        var daylightEvent = DeserialiseEvent(message);
        if (daylightEvent == null)
        {
            logger.LogError("message JSON was empty");
            return;
        }

        ToggleAutoLightSwitch(daylightEvent.isSunrise);
    }

    private void ToggleAutoLightSwitch(bool isSunrise)
    {
        if (isSunrise)
        {
            logger.LogInformation("Disabling auto light");
            houseManager.AutoLight.Trigger(AutoLightCommand.OFF);
        }
        else
        {
            logger.LogInformation("Enabling auto light");
            houseManager.AutoLight.Trigger(AutoLightCommand.ON);
        }
    }

    private Daylight? DeserialiseEvent(string message)
    {
        try
        {
            return JsonSerializer.Deserialize<Daylight>(message);
        }
        catch
        {
            logger.LogError("message not in JSON format.");
        }

        return null;
    }
}
