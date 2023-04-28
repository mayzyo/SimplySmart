using Microsoft.Extensions.Logging;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SimpleFrigateSorter;

public interface IFrigateEventHandler
{
    void HandleEvent(string message);
}

public class FrigateEventHandler : IFrigateEventHandler
{
    private readonly ILogger<FrigateEventHandler> logger;
    private readonly IManagedMqttClient managedMqttClient;
    private readonly IConfigurationService configurationService;
    public FrigateEventHandler(ILogger<FrigateEventHandler> logger, IManagedMqttClient managedMqttClient, IConfigurationService configurationService)
    {
        this.logger = logger;
        this.managedMqttClient = managedMqttClient;
        this.configurationService = configurationService;
    }

    public async void HandleEvent(string message)
    {
        try
        {
            var frigateEvent = JsonSerializer.Deserialize<FrigateEvent>(message);
            if(frigateEvent == null)
            {
                logger.LogError("message JSON was empty");
                return;
            }

            logger.LogInformation("{Type} - {Camera} - {DateTime}", frigateEvent.type, frigateEvent.before?.camera, DateTime.Now);

            if (frigateEvent.type == "new")
            {
                //await managedMqttClient.EnqueueAsync("frigate/events/new", message);

                if(frigateEvent.before?.label == "car")
                {
                    await managedMqttClient.EnqueueAsync("frigate/events/car", message);
                }
            }
            else if (frigateEvent.type == "end")
            {
                //await managedMqttClient.EnqueueAsync("frigate/events/end", message);
                if(configurationService.Configuration?.IsHome != true)
                {
                    await managedMqttClient.EnqueueAsync("frigate/events/alarm", message);
                } else if (configurationService.Configuration?.OutdoorCameras != null && configurationService.Configuration.OutdoorCameras.Contains(frigateEvent.before?.camera))
                {
                    await managedMqttClient.EnqueueAsync("frigate/events/outdoor", message);
                }
            }
        }
        catch
        {
            logger.LogError("message not in JSON format.");
        }
    }


}
