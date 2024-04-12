using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimplySmart.Core.Models;
using SimplySmart.DeviceStates.Devices;
using SimplySmart.DeviceStates.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                if(lightSwitch.isDimmer == true)
                {
                    return lightSwitchFactory.CreateDimmerLightSwitch(lightSwitch, lightSwitch.stayOn);
                } else
                {
                    return lightSwitchFactory.CreateLightSwitch(lightSwitch, lightSwitch.stayOn);
                }
            }

            if (TryGetPowerSwitch(key, out Core.Models.PowerSwitch? powerSwitch) && powerSwitch != null)
            {
                return lightSwitchFactory.CreateLightSwitch(powerSwitch, null);
            }

            logger.LogError($"Light Switch with {key} does not exist");
            return null;
        }
    }

    public async Task PublishAll()
    {
        foreach (var lightSwitch in GetAllLightSwitch())
        {
            if (lightSwitch.isDimmer == true)
            {
                await lightSwitchFactory.CreateDimmerLightSwitch(lightSwitch, lightSwitch.stayOn).Publish();
            }
            else
            {
                await lightSwitchFactory.CreateLightSwitch(lightSwitch, lightSwitch.stayOn).Publish();
            }
        }

        foreach (var powerSwitch in GetAllPowerSwitch())
        {
            await lightSwitchFactory.CreateLightSwitch(powerSwitch, null).Publish();
        }
    }

    public void SetAllToAuto(bool command)
    {
        foreach (var lightSwitchConfig in GetAllLightSwitch())
        {
            ILightSwitch lightSwitch;

            if (lightSwitchConfig.isDimmer == true)
            {
                lightSwitch = lightSwitchFactory.CreateDimmerLightSwitch(lightSwitchConfig, lightSwitchConfig.stayOn);
            }
            else
            {
                lightSwitch = lightSwitchFactory.CreateLightSwitch(lightSwitchConfig, lightSwitchConfig.stayOn);
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
            var lightSwitch = lightSwitchFactory.CreateLightSwitch(powerSwitch, null);
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
        if (options.Value.lightSwitches is null)
        {
            lightSwitch = null;
            return false;
        }

        lightSwitch = options.Value.lightSwitches.Where(e => e.name == key).FirstOrDefault();
        return true;
    }

    bool TryGetPowerSwitch(string key, out Core.Models.PowerSwitch? powerSwitch)
    {
        if (options.Value.powerSwitches is null)
        {
            powerSwitch = null;
            return false;
        }

        powerSwitch = options.Value.powerSwitches.Where(e => e.name == key).FirstOrDefault();
        return true;
    }

    IEnumerable<Core.Models.LightSwitch> GetAllLightSwitch()
    {
        if (options.Value.lightSwitches == null)
        {
            return [];
        }

        return options.Value.lightSwitches;
    }

    IEnumerable<Core.Models.PowerSwitch> GetAllPowerSwitch()
    {
        if (options.Value.powerSwitches == null)
        {
            return [];
        }

        return options.Value.powerSwitches.Where(e => e.type == "light");
    }
}