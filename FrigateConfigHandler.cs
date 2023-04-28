using Microsoft.Extensions.Logging;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimpleFrigateSorter;

public interface IFrigateConfigHandler
{
    void HandleEvent(string message);
}

public class FrigateConfigHandler : IFrigateConfigHandler
{
    private readonly ILogger<FrigateConfigHandler> logger;
    private readonly IConfigurationService configurationService;
    public FrigateConfigHandler(ILogger<FrigateConfigHandler> logger, IConfigurationService configurationService)
    {
        this.logger = logger;
        this.configurationService = configurationService;
    }

    public void HandleEvent(string message)
    {
        try
        {
            var config = JsonSerializer.Deserialize<Configuration>(message);
            if (config == null)
            {
                logger.LogError("message JSON was empty");
                return;
            }

            logger.LogInformation("Is Home: {bool} - {DateTime}", config.IsHome, DateTime.Now);
            configurationService.Update(new Configuration { IsHome = config.IsHome });
        }
        catch
        {
            logger.LogError("message not in JSON format.");
        }
    }
}
