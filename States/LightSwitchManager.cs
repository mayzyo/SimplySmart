using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleFrigateSorter.Frigate;
using SimpleFrigateSorter.Utils;
using SimpleFrigateSorter.Zwave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace SimpleFrigateSorter.States;

public interface ILightSwitchManager
{
    ILightSwitch this[string key] { get; }

    void DisableAuto();

    void EnableAuto();
}

internal class LightSwitchManager : ILightSwitchManager
{
    private readonly IServiceProvider serviceProvider;

    public ILightSwitch this[string key]
    {
        get
        {
            if (!states.ContainsKey(key))
            {
                throw new Exception($"Light Switch with {key} does not exist");
            }

            return states[key];
        }
    }

    private readonly ILogger<LightSwitchManager> logger;
    private readonly IDictionary<string, ILightSwitch> states = new Dictionary<string, ILightSwitch>();

    public LightSwitchManager(ILogger<LightSwitchManager> logger, IServiceProvider serviceProvider, IDeserializer deserializer)
    {
        this.logger = logger;
        this.serviceProvider = serviceProvider;
        var path = Environment.GetEnvironmentVariable("CONFIG_FILE_PATH") ?? throw new Exception("Config file missing!");
        using var sr = File.OpenText(path);
        var config = deserializer.Deserialize<ApplicationConfig>(sr);

        foreach (var lightSwitchConfig in config.lightSwitches)
        {
            var lightSwitch = new LightSwitch(lightSwitchConfig.stayOn);
            lightSwitch.stateMachine.OnTransitioned(async (transition) =>
            {
                using var scope = serviceProvider.CreateScope();
                IZwaveLightSwitchHandler handler = scope.ServiceProvider.GetRequiredService<IZwaveLightSwitchHandler>();

                if (transition.Destination == LightSwitchState.ON || transition.Destination == LightSwitchState.FORCED_ON)
                {
                    await handler.HandleOn(lightSwitchConfig.name);
                }
                else if ((transition.Destination == LightSwitchState.OFF || transition.Destination == LightSwitchState.FORCED_OFF) && transition.Source != LightSwitchState.FORCED_OFF)
                {
                    await handler.HandleOff(lightSwitchConfig.name);
                }
            });

            states.Add(lightSwitchConfig.name, lightSwitch);
        }
    }

    public void DisableAuto()
    {
        logger.LogInformation("Auto light switch disabled");
        foreach (var state in states)
        {
            state.Value.Trigger(LightSwitchCommand.FORCE_OFF);
        }
    }

    public void EnableAuto()
    {
        logger.LogInformation("Auto light switch enabled");
        foreach (var state in states)
        {
            state.Value.Trigger(LightSwitchCommand.SET_OFF);
        }
    }
}