using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimplySmart.Homebridge;
using SimplySmart.Utils;
using SimplySmart.Zwave;
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
    private readonly IDictionary<string, IDimmerLightSwitch> dimmers = new Dictionary<string, IDimmerLightSwitch>();

    public LightSwitchManager(ILogger<LightSwitchManager> logger, IServiceProvider serviceProvider, IDeserializer deserializer)
    {
        this.logger = logger;
        Initialise(serviceProvider, deserializer);
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

    private void Initialise(IServiceProvider serviceProvider, IDeserializer deserializer)
    {
        var path = Environment.GetEnvironmentVariable("CONFIG_FILE_PATH") ?? throw new Exception("Config file missing!");
        using var sr = File.OpenText(path);
        var config = deserializer.Deserialize<ApplicationConfig>(sr);

        if (config.lightSwitches == null)
        {
            return;
        }

        foreach (var lightSwitchConfig in config.lightSwitches)
        {
            LightSwitch lightSwitch;

            if(lightSwitchConfig.isDimmer == true)
            {
                var dimmerLightSwitch = new DimmerLightSwitch(lightSwitchConfig.stayOn);

                dimmerLightSwitch.stateMachine.Configure(LightSwitchState.MANUAL_ON)
                    .OnEntryAsync(async () =>
                    {
                        using var scope = serviceProvider.CreateScope();

                        if(dimmerLightSwitch.Source == BroadcastSource.ZWAVE)
                        {
                            IHomebridgeLightSwitchHandler homebridgeHandler = scope.ServiceProvider.GetRequiredService<IHomebridgeLightSwitchHandler>();
                            await homebridgeHandler.HandleOn(lightSwitchConfig.name, dimmerLightSwitch.Brightness);
                        }
                        else if(dimmerLightSwitch.Source == BroadcastSource.HOMEBRIDGE)
                        {
                            IZwaveLightSwitchHandler zwaveHandler = scope.ServiceProvider.GetRequiredService<IZwaveLightSwitchHandler>();
                            await zwaveHandler.HandleOn(lightSwitchConfig.name, dimmerLightSwitch.Brightness);
                        }
                    })
                    .PermitReentryIf(LightSwitchCommand.MANUAL_ON, dimmerLightSwitch.LevelChange);

                lightSwitch = dimmerLightSwitch;
                dimmers.Add(lightSwitchConfig.name, dimmerLightSwitch);
            }
            else
            {
                lightSwitch = new LightSwitch(lightSwitchConfig.stayOn);

                lightSwitch.stateMachine.Configure(LightSwitchState.ON)
                    .OnEntryAsync(async () =>
                    {
                        using var scope = serviceProvider.CreateScope();
                        IZwaveLightSwitchHandler zwaveHandler = scope.ServiceProvider.GetRequiredService<IZwaveLightSwitchHandler>();
                        IHomebridgeLightSwitchHandler homebridgeHandler = scope.ServiceProvider.GetRequiredService<IHomebridgeLightSwitchHandler>();
                        await zwaveHandler.HandleOn(lightSwitchConfig.name);
                        await homebridgeHandler.HandleOn(lightSwitchConfig.name);
                    });
            }

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
}