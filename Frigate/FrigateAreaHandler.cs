using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using SimpleFrigateSorter.States;
using System;
using System.Collections.Generic;

namespace SimpleFrigateSorter.Frigate;

public interface IFrigateAreaHandler
{
    Task HandleEvent(MqttApplicationMessageReceivedEventArgs e);
}

internal class FrigateAreaHandler : IFrigateAreaHandler
{
    private readonly ILogger<FrigateAreaHandler> logger;
    private readonly IAreaOccupantManager areaOccupantManager;

    public FrigateAreaHandler(ILogger<FrigateAreaHandler> logger, IAreaOccupantManager areaOccupantManager)
    {
        this.logger = logger;
        this.areaOccupantManager = areaOccupantManager;
    }

    public async Task HandleEvent(MqttApplicationMessageReceivedEventArgs e)
    {
        var areaName = e.ApplicationMessage.Topic.Split("/")[1];
        var message = e.ApplicationMessage.ConvertPayloadToString();
        var count = int.Parse(message);

        ChangeOccupantState(areaName, count);
    }

    private void ChangeOccupantState(string areaName, int count)
    {
        if (areaOccupantManager.Exists(areaName))
        {
            if (count != 0)
            {
                areaOccupantManager[areaName].Trigger(AreaOccupantCommand.SET_MOVING);
            }
            else
            {
                areaOccupantManager[areaName].Trigger(AreaOccupantCommand.SET_EMPTY);
            }
        }
    }
}
