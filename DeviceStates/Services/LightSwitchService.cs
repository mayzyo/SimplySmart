using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimplySmart.Core.Models;
using SimplySmart.DeviceStates.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.DeviceStates.Services;

public interface ILightSwitchService
{
    ILightSwitch this[string key] { get; }

    void All(LightSwitchCommand command);

    bool Exists(string key);
}

internal class LightSwitchService : ILightSwitchService
{
    public ILightSwitch this[string key]
    {
        get
        {
            if (!states.TryGetValue(key, out ILightSwitch? value))
            {
                throw new Exception($"Light Switch with {key} does not exist");
            }

            return value;
        }
    }

    private readonly ILogger<LightSwitchService> logger;
    private readonly Dictionary<string, ILightSwitch> states = [];

    public LightSwitchService(IOptions<ApplicationConfig> options, ILogger<LightSwitchService> logger, ILightSwitchFactory lightSwitchFactory)
    {
        this.logger = logger;

        if (options.Value.lightSwitches != null)
        {
            InitialiseLightSwitch(lightSwitchFactory, options.Value);
        }

        if (options.Value.powerSwitches != null)
        {
            InitialisePowerSwitch(lightSwitchFactory, options.Value);
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

    private void InitialiseLightSwitch(ILightSwitchFactory lightSwitchFactory, ApplicationConfig appConfig)
    {
        foreach (var config in appConfig.lightSwitches)
        {
            ILightSwitch lightSwitch;

            if (config.isDimmer == true)
            {
                lightSwitch = lightSwitchFactory.CreateDimmerLightSwitch(config.name, config.stayOn);
            }
            else
            {
                lightSwitch = lightSwitchFactory.CreateLightSwitch(config.name, config.stayOn);
            }

            states.Add(config.name, lightSwitch);
        }

        logger.LogInformation("Light Switches loaded successfully in Light Switch Service");
    }

    private void InitialisePowerSwitch(ILightSwitchFactory lightSwitchFactory, ApplicationConfig appConfig)
    {
        foreach (var config in appConfig.powerSwitches.Where(e => e.type == "light"))
        {
            var lightSwitch = lightSwitchFactory.CreateLightSwitch(config.name, null);
            states.Add(config.name, lightSwitch);

            logger.LogInformation("Power Switches loaded successfully in Light Switch Service");
        }
    }
}