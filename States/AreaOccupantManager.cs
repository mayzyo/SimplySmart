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
        var path = Environment.GetEnvironmentVariable("CONFIG_FILE_PATH") ?? throw new Exception("Config file missing!");
        using var sr = File.OpenText(path);
        var config = deserializer.Deserialize<ApplicationConfig>(sr);

        if(config.areaOccupants == null)
        {
            return;
        }

        foreach (var areaOccupantConfig in config.areaOccupants)
        {
            var areaOccupant = new AreaOccupant();
            areaOccupant.stateMachine.Configure(AreaOccupantState.EMPTY)
                .OnEntry(() =>
                    lightSwitchManager[areaOccupantConfig.lightSwitch].Trigger(LightSwitchCommand.AUTO_OFF)
                );

            areaOccupant.stateMachine.Configure(AreaOccupantState.MOVING)
                .OnEntry(() =>
                    lightSwitchManager[areaOccupantConfig.lightSwitch].Trigger(LightSwitchCommand.AUTO_ON)
                );

            areaOccupant.stateMachine.Configure(AreaOccupantState.STATIONARY)
                .OnEntry(
                    () => lightSwitchManager[areaOccupantConfig.lightSwitch].Trigger(LightSwitchCommand.AUTO_ON)
                );

            states.Add(areaOccupantConfig.name, areaOccupant);
        }
    }

    public bool Exists(string key)
    {
        return states.ContainsKey(key);
    }
}
