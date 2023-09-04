using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimplySmart.Frigate;
using SimplySmart.Homebridge;
using SimplySmart.Utils;
using SimplySmart.Zwave;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace SimplySmart.States;

public interface ILightSwitchManager
{
    ILightSwitch this[string key] { get; }

    void DisableAuto();

    void EnableAuto();

    bool Exists(string key);
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

            lightSwitch.stateMachine.Configure(LightSwitchState.ON)
                .OnEntryAsync(async (e) =>
                {
                    using var scope = serviceProvider.CreateScope();
                    IZwaveLightSwitchHandler zwaveHandler = scope.ServiceProvider.GetRequiredService<IZwaveLightSwitchHandler>();
                    IHomebridgeLightSwitchHandler homebridgeHandler = scope.ServiceProvider.GetRequiredService<IHomebridgeLightSwitchHandler>();
                    await zwaveHandler.HandleOn(lightSwitchConfig.name);
                    await homebridgeHandler.HandleOn(lightSwitchConfig.name);
                });

            lightSwitch.stateMachine.Configure(LightSwitchState.OFF)
                .OnEntryAsync(async () =>
                {
                    using var scope = serviceProvider.CreateScope();
                    IZwaveLightSwitchHandler zwaveHandler = scope.ServiceProvider.GetRequiredService<IZwaveLightSwitchHandler>();
                    IHomebridgeLightSwitchHandler homebridgeHandler = scope.ServiceProvider.GetRequiredService<IHomebridgeLightSwitchHandler>();
                    await zwaveHandler.HandleOff(lightSwitchConfig.name);
                    await homebridgeHandler.HandleOff(lightSwitchConfig.name);
                });

            states.Add(lightSwitchConfig.name, lightSwitch);
        }
    }

    public void DisableAuto()
    {
        logger.LogInformation("Auto light switch disabled");
        foreach (var state in states)
        {
            state.Value.Trigger(LightSwitchCommand.DISABLE_AUTO);
        }
    }

    public void EnableAuto()
    {
        logger.LogInformation("Auto light switch enabled");
        foreach (var state in states)
        {
            state.Value.Trigger(LightSwitchCommand.ENABLE_AUTO);
        }
    }

    public bool Exists(string key)
    {
        return states.ContainsKey(key);
    }
}