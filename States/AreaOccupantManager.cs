using Microsoft.Extensions.Logging;
using SimplySmart.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace SimplySmart.States;

public interface IAreaOccupantManager
{
    IAreaOccupant this[string key] { get; }

    bool Exists(string key);
}

internal class AreaOccupantManager: IAreaOccupantManager
{
    public IAreaOccupant this[string key]
    { 
        get
        {
            if (!states.ContainsKey(key))
            {
                throw new Exception($"Area Occupant with {key} does not exist");
            }

            return states[key];
        }
    }

    private readonly ILogger<AreaOccupantManager> logger;
    private readonly IDictionary<string, IAreaOccupant> states = new Dictionary<string, IAreaOccupant>();

    public AreaOccupantManager(ILogger<AreaOccupantManager> logger, IDeserializer deserializer, ILightSwitchManager lightSwitchManager)
    {
        this.logger = logger;
        var config = DeserialiseConfig(deserializer);

        if (config.cameras != null)
        {
            InitialiseCamera(config, lightSwitchManager);
        }

        if (config.multiSensors != null)
        {
            InitialiseMultiSensor(config, lightSwitchManager);
        }
    }

    private void InitialiseCamera(ApplicationConfig appConfig, ILightSwitchManager lightSwitchManager)
    {
        foreach (var config in appConfig.cameras)
        {
            var areaOccupant = new AreaOccupant();

            areaOccupant.Initialise(config.lightSwitch != default ? lightSwitchManager[config.lightSwitch] : default);
            states.Add(config.name, areaOccupant);
        }

        logger.LogInformation("Cameras loaded successfully in Area Occupant Manager");
    }

    private void InitialiseMultiSensor(ApplicationConfig appConfig, ILightSwitchManager lightSwitchManager)
    {
        foreach (var config in appConfig.multiSensors)
        {
            var areaOccupant = new AreaOccupant();

            areaOccupant.Initialise(config.lightSwitch != default ? lightSwitchManager[config.lightSwitch] : default);
            states.Add(config.name, areaOccupant);
        }

        logger.LogInformation("Multi Sensors loaded successfully in Area Occupant Manager");
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
