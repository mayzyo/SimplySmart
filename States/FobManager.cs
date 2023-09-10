using Microsoft.Extensions.Logging;
using SimplySmart.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace SimplySmart.States;

public interface IFobManager
{
    IFob this[string key] { get; }

    bool Exists(string key);
}

internal class FobManager : IFobManager
{
    public IFob this[string key]
    { 
        get
        {
            if (!states.ContainsKey(key))
            {
                throw new Exception($"Garage door with {key} does not exist");
            }

            return states[key];
        }
    }

    private readonly ILogger<FobManager> logger;
    private readonly IDictionary<string, IFob> states = new Dictionary<string, IFob>();

    public FobManager(ILogger<FobManager> logger, IDeserializer deserializer, IServiceProvider serviceProvider)
    {
        this.logger = logger;
        var config = DeserialiseConfig(deserializer);

        if (config.fobs != null)
        {
            Initialise(config, serviceProvider);
        }
    }

    private void Initialise(ApplicationConfig config, IServiceProvider serviceProvider)
    {
        foreach (var fobConfig in config.fobs)
        {
            var fob = new Fob();
            fob.Initialise(serviceProvider, fobConfig.fobButtons);
            states.Add(fobConfig.name, fob);
        }
    }

    private static ApplicationConfig DeserialiseConfig(IDeserializer deserializer)
    {
        var path = Environment.GetEnvironmentVariable("CONFIG_FILE_PATH") ?? throw new Exception("Config file missing!");
        using var sr = File.OpenText(path);
        return deserializer.Deserialize<ApplicationConfig>(sr);
    }

    public bool Exists(string key)
    {
        return states.ContainsKey(key);
    }
}
