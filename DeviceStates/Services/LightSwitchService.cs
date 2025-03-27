using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimplySmart.Core.Models;
using SimplySmart.DeviceStates.Devices;
using SimplySmart.DeviceStates.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimplySmart.DeviceStates.Services;

public interface ILightSwitchService
{
    ILightSwitch? this[string key] { get; }
    Task PublishAll();
    void SetAllToAuto(bool command);
}

internal class LightSwitchService(IOptions<ApplicationConfig> options, ILogger<ILightSwitchService> logger, ILightSwitchFactory lightSwitchFactory) : ILightSwitchService
{
    public ILightSwitch? this[string key]
    {
        get
        {
            if (TryGetLightSwitch(key, out Core.Models.LightSwitch? lightSwitch) && lightSwitch != null)
            {
                if(lightSwitch.IsDimmer == true)
                {
                    return lightSwitchFactory.CreateDimmerLightSwitch(lightSwitch);
                } else
                {
                    return lightSwitchFactory.CreateLightSwitch(lightSwitch);
                }
            }

            if (TryGetPowerSwitch(key, out Core.Models.PowerSwitch? powerSwitch) && powerSwitch != null)
            {
                return lightSwitchFactory.CreateLightSwitch(powerSwitch);
            }

            logger.LogError($"Light Switch with {key} does not exist");
            return null;
        }
    }

    public async Task PublishAll()
    {
        foreach (var lightSwitch in GetAllLightSwitch())
        {
            if (lightSwitch.IsDimmer == true)
            {
                await lightSwitchFactory.CreateDimmerLightSwitch(lightSwitch).Publish();
            }
            else
            {
                await lightSwitchFactory.CreateLightSwitch(lightSwitch).Publish();
            }
        }

        foreach (var powerSwitch in GetAllPowerSwitch())
        {
            await lightSwitchFactory.CreateLightSwitch(powerSwitch).Publish();
        }
    }

    public void SetAllToAuto(bool command)
    {
        foreach (var lightSwitchConfig in GetAllLightSwitch())
        {
            ILightSwitch lightSwitch;

            if (lightSwitchConfig.IsDimmer == true)
            {
                lightSwitch = lightSwitchFactory.CreateDimmerLightSwitch(lightSwitchConfig);
            }
            else
            {
                lightSwitch = lightSwitchFactory.CreateLightSwitch(lightSwitchConfig);
            }

            if(command)
            {
                lightSwitch.EnableAuto();
            }
            else
            {
                lightSwitch.DisableAuto();
            }
        }

        foreach (var powerSwitch in GetAllPowerSwitch())
        {
            var lightSwitch = lightSwitchFactory.CreateLightSwitch(powerSwitch);
            if (command)
            {
                lightSwitch.EnableAuto();
            }
            else
            {
                lightSwitch.DisableAuto();
            }
        }

        logger.LogInformation("Auto is enabled on all lights");
    }

    bool TryGetLightSwitch(string key, out Core.Models.LightSwitch? lightSwitch)
    {
        if (options.Value.LightSwitches is null)
        {
            lightSwitch = null;
            return false;
        }

        lightSwitch = options.Value.LightSwitches.Where(e => e.Name == key).FirstOrDefault();
        return true;
    }

    bool TryGetPowerSwitch(string key, out Core.Models.PowerSwitch? powerSwitch)
    {
        if (options.Value.PowerSwitches is null)
        {
            powerSwitch = null;
            return false;
        }

        powerSwitch = options.Value.PowerSwitches.Where(e => e.Name == key).FirstOrDefault();
        return true;
    }

    IEnumerable<Core.Models.LightSwitch> GetAllLightSwitch()
    {
        if (options.Value.LightSwitches == null)
        {
            return [];
        }

        return options.Value.LightSwitches;
    }

    IEnumerable<Core.Models.PowerSwitch> GetAllPowerSwitch()
    {
        if (options.Value.PowerSwitches == null)
        {
            return [];
        }

        return options.Value.PowerSwitches.Where(e => e.Type == "light");
    }
}