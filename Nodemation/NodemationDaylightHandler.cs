﻿using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using SimpleFrigateSorter.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimpleFrigateSorter.Nodemation;

public interface INodemationDaylightHandler
{
    Task HandleEvent(MqttApplicationMessageReceivedEventArgs e);
}

internal class NodemationDaylightHandler : INodemationDaylightHandler
{
    private readonly ILogger<NodemationDaylightHandler> logger;
    private readonly ILightSwitchManager lightSwitchManager;

    public NodemationDaylightHandler(ILogger<NodemationDaylightHandler> logger, ILightSwitchManager lightSwitchManager)
    {
        this.logger = logger;
        this.lightSwitchManager = lightSwitchManager;
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
            lightSwitchManager.DisableAuto();
        }
        else
        {
            lightSwitchManager.EnableAuto();
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