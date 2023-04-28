using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MQTTnet.Client;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SimpleFrigateSorter;

public interface IConfigurationService
{
    Configuration? Configuration { get; }

    void Update(Configuration configuration);
}

public class ConfigurationService : IConfigurationService
{
    public Configuration Configuration { get; private set; } = new Configuration();
    private readonly ILogger<ConfigurationService> logger;

    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        this.logger = logger;
        Initialise();
    }

    public void Update(Configuration configuration)
    {
        Configuration.IsHome = configuration.IsHome;
    }

    private async void Initialise()
    {
        var path = Environment.GetEnvironmentVariable("CONFIG_FILE_PATH");
        Configuration = await ReadJsonFileAsync<Configuration>(path);

        logger.LogInformation("Config file loaded successfully");
        logger.LogDebug("Outdoor Cameras: {list}", string.Join(",", Configuration.OutdoorCameras ?? new List<string>()));

        var isHome = await ReadRedisAsync("isHome");
        Configuration.IsHome = isHome == "true";
        logger.LogDebug("Is Home: {data}", Configuration.IsHome);
    }

    private static async Task<T> ReadJsonFileAsync<T>(string filePath)
    {
        using FileStream stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<T>(stream);
    }

    private static async Task<RedisValue> ReadRedisAsync(string key)
    {
        _ = int.TryParse(Environment.GetEnvironmentVariable("REDIS_DATABASE") ?? "0", out var database);

        var options = new ConfigurationOptions
        {
            EndPoints = { Environment.GetEnvironmentVariable("REDIS_URL") ?? "" },
            DefaultDatabase = database,
            Password = GetCredentials()
        };
        var redis = ConnectionMultiplexer.Connect(options);

        var db = redis.GetDatabase();
        var isHome = await db.HashGetAsync("home-security:config", key);
        await redis.CloseAsync();

        return isHome;
    }

    private static string GetCredentials()
    {
        var password = Environment.GetEnvironmentVariable("REDIS_PASSWORD");

        if (password == null)
        {
            var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

            var secretProvider = config.Providers.First();
            secretProvider.TryGet("REDIS_PASSWORD", out password);
        }

        return password;
    }
}
