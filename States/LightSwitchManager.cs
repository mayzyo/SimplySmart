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

    void All(LightSwitchCommand command);

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

    public LightSwitchManager(ILogger<LightSwitchManager> logger, IServiceProvider serviceProvider, IDeserializer deserializer)
    {
        this.logger = logger;

        var config = DeserialiseConfig(deserializer);

        if (config.lightSwitches != null)
        {
            InitialiseLightSwitch(serviceProvider, config);
        }

        if (config.powerSwitches != null)
        {
            InitialisePowerSwitch(serviceProvider, config);
        }
    }

    public void All(LightSwitchCommand command)
    {
        logger.LogInformation("All light switch triggered");

        foreach (var state in states)
        {
            state.Value.Trigger(command, BroadcastSource.EXTERNAL);
        }
    }

    public bool Exists(string key)
    {
        return states.ContainsKey(key);
    }

    private void InitialiseLightSwitch(IServiceProvider serviceProvider, ApplicationConfig appConfig)
    {
        foreach (var config in appConfig.lightSwitches)
        {
            LightSwitch lightSwitch;

            if(config.isDimmer == true)
            {
                var dimmerLightSwitch = new DimmerLightSwitch(config.stayOn);

                dimmerLightSwitch.stateMachine.Configure(LightSwitchState.MANUAL_ON)
                    .OnEntryAsync(async () =>
                    {
                        using var scope = serviceProvider.CreateScope();

                        if(dimmerLightSwitch.Source != BroadcastSource.ZWAVE)
                        {
                            IZwaveMultiLevelSwitchHandler zwaveHandler = scope.ServiceProvider.GetRequiredService<IZwaveMultiLevelSwitchHandler>();
                            await zwaveHandler.HandleOn(config.name, dimmerLightSwitch.Brightness);
                        }
                        
                        if(dimmerLightSwitch.Source != BroadcastSource.HOMEBRIDGE)
                        {
                            IHomebridgeLightSwitchHandler homebridgeHandler = scope.ServiceProvider.GetRequiredService<IHomebridgeLightSwitchHandler>();
                            await homebridgeHandler.HandleOn(config.name, dimmerLightSwitch.Brightness);

                        }
                    })
                    .PermitReentryIf(LightSwitchCommand.MANUAL_ON, dimmerLightSwitch.LevelChange);

                lightSwitch = dimmerLightSwitch;
            }
            else
            {
                lightSwitch = new LightSwitch(config.stayOn);

                lightSwitch.stateMachine.Configure(LightSwitchState.ON)
                    .OnEntryAsync(async () =>
                    {
                        using var scope = serviceProvider.CreateScope();

                        if (lightSwitch.Source != BroadcastSource.ZWAVE)
                        {
                            IZwaveBinarySwitchHandler zwaveHandler = scope.ServiceProvider.GetRequiredService<IZwaveBinarySwitchHandler>();
                            await zwaveHandler.HandleOn(config.name);

                        }
                        
                        if (lightSwitch.Source != BroadcastSource.HOMEBRIDGE)
                        {
                            IHomebridgeLightSwitchHandler homebridgeHandler = scope.ServiceProvider.GetRequiredService<IHomebridgeLightSwitchHandler>();
                            await homebridgeHandler.HandleOn(config.name);
                        }
                    });
            }

            lightSwitch.stateMachine.Configure(LightSwitchState.OFF)
                .OnEntryAsync(async () =>
                {
                    using var scope = serviceProvider.CreateScope();

                    if (lightSwitch.Source != BroadcastSource.ZWAVE)
                    {
                        IZwaveBinarySwitchHandler zwaveHandler = scope.ServiceProvider.GetRequiredService<IZwaveBinarySwitchHandler>();
                        await zwaveHandler.HandleOff(config.name);
                    }
                    
                    if (lightSwitch.Source != BroadcastSource.HOMEBRIDGE)
                    {
                        IHomebridgeLightSwitchHandler homebridgeHandler = scope.ServiceProvider.GetRequiredService<IHomebridgeLightSwitchHandler>();
                        await homebridgeHandler.HandleOff(config.name);
                    }
                });

            states.Add(config.name, lightSwitch);
        }

        logger.LogInformation("Light Switches loaded successfully in Light Switch Manager");
    }

    private void InitialisePowerSwitch(IServiceProvider serviceProvider, ApplicationConfig appConfig)
    {
        foreach (var config in appConfig.powerSwitches.Where(e => e.type == "light"))
        {
            var lightSwitch = new LightSwitch(null);

            lightSwitch.stateMachine.Configure(LightSwitchState.ON)
                .OnEntryAsync(async () =>
                {
                    using var scope = serviceProvider.CreateScope();
                    IZwaveBinarySwitchHandler zwaveHandler = scope.ServiceProvider.GetRequiredService<IZwaveBinarySwitchHandler>();
                    IHomebridgeLightSwitchHandler homebridgeHandler = scope.ServiceProvider.GetRequiredService<IHomebridgeLightSwitchHandler>();
                    await zwaveHandler.HandleOn(config.name);
                    await homebridgeHandler.HandleOn(config.name);
                });

            lightSwitch.stateMachine.Configure(LightSwitchState.OFF)
                .OnEntryAsync(async () =>
                {
                    using var scope = serviceProvider.CreateScope();
                    IZwaveBinarySwitchHandler zwaveHandler = scope.ServiceProvider.GetRequiredService<IZwaveBinarySwitchHandler>();
                    IHomebridgeLightSwitchHandler homebridgeHandler = scope.ServiceProvider.GetRequiredService<IHomebridgeLightSwitchHandler>();
                    await zwaveHandler.HandleOff(config.name);
                    await homebridgeHandler.HandleOff(config.name);
                });

            states.Add(config.name, lightSwitch);

            logger.LogInformation("Power Switches loaded successfully in Light Switch Manager");
        }
    }

    private static ApplicationConfig DeserialiseConfig(IDeserializer deserializer)
    {
        var path = Environment.GetEnvironmentVariable("CONFIG_FILE_PATH") ?? throw new Exception("Config file missing!");
        using var sr = File.OpenText(path);
        return deserializer.Deserialize<ApplicationConfig>(sr);
    }
}