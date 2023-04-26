using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SimpleFrigateSorter;

public interface IConfigurationService
{
    Configuration? Configuration { get; }
}

public class ConfigurationService : IConfigurationService
{
    public Configuration? Configuration { get; private set; } = null;
    private readonly ILogger<ConfigurationService> logger;

    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        this.logger = logger;
        Initialise();
    }

    private async void Initialise()
    {
        var path = Environment.GetEnvironmentVariable("CONFIG_FILE_PATH");
        Configuration = await ReadJsonFileAsync<Configuration>(path);

        logger.LogInformation("Config file loaded successfully");
        logger.LogDebug("Outdoor Cameras: {List}", string.Join(",", Configuration.OutdoorCameras));
    }

    private async Task<T> ReadJsonFileAsync<T>(string filePath)
    {
        using FileStream stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<T>(stream);
    }
}
