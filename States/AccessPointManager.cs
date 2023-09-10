using Microsoft.Extensions.Logging;
using SimplySmart.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace SimplySmart.States;

public interface IAccessPointManager
{
    IAccessPoint this[string key] { get; }

    bool Exists(string key);
}

internal class AccessPointManager : IAccessPointManager
{
    public IAccessPoint this[string key]
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

    private readonly ILogger<AccessPointManager> logger;
    private readonly IDictionary<string, IAccessPoint> states = new Dictionary<string, IAccessPoint>();

    public AccessPointManager(ILogger<AccessPointManager> logger, IDeserializer deserializer, IServiceProvider serviceProvider)
    {
        this.logger = logger;
        var config = DeserialiseConfig(deserializer);

        if (config.smartImplants != null)
        {
            Initialise(config, serviceProvider);
        }
    }

    private void Initialise(ApplicationConfig config, IServiceProvider serviceProvider)
    {
        foreach (var smartImplant in config.smartImplants)
        {
            if(smartImplant.type == "garageDoor")
            {
                var garageDoor = new GarageDoor(smartImplant.name);
                garageDoor.Initialise(serviceProvider);
                states.Add(smartImplant.name, garageDoor);
            }
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
