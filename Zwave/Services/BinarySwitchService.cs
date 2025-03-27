using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimplySmart.Core.Models;
using SimplySmart.DeviceStates.Factories;
using SimplySmart.Zwave.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Zwave.Services;

public interface IBinarySwitchService
{
    IBinarySwitch? this[string key] { get; }
}

internal class BinarySwitchService(
    IOptions<ApplicationConfig> options,
    ILogger<IBinarySwitchService> logger,
    ILightSwitchFactory lightSwitchFactory,
    IFanFactory fanFactory,
    IGarageDoorFactory garageDoorFactory
) : IBinarySwitchService
{
    public IBinarySwitch? this[string key]
    {
        get
        {
            if (TryGetLightSwitch(key, out LightSwitch? lightSwitch) && lightSwitch != null)
            {
                return lightSwitchFactory.CreateLightSwitch(lightSwitch);
            }

            if (TryGetPowerSwitch(key, out PowerSwitch? powerSwitch) && powerSwitch != null)
            {
                if (powerSwitch.Type == "fan")
                {
                    return fanFactory.CreateFan(powerSwitch);
                }
                return lightSwitchFactory.CreateLightSwitch(powerSwitch);
            }

            if (TryGetSmartImplant(key, out SmartImplant? smartImplant) && smartImplant != null)
            {
                if (smartImplant.Type == "garageDoor")
                {
                    return garageDoorFactory.CreateGarageDoor(smartImplant);
                }
            }

            logger.LogError($"Binary Switch with {key} does not exist");
            return null;
        }
    }

    bool TryGetLightSwitch(string key, out LightSwitch? lightSwitch)
    {
        if (options.Value.LightSwitches is null)
        {
            lightSwitch = null;
            return false;
        }

        lightSwitch = options.Value.LightSwitches.Where(e => e.Name == key && e.IsDimmer == null).FirstOrDefault();
        return true;
    }

    bool TryGetPowerSwitch(string key, out PowerSwitch? powerSwitch)
    {
        if (options.Value.PowerSwitches is null)
        {
            powerSwitch = null;
            return false;
        }

        powerSwitch = options.Value.PowerSwitches.Where(e => e.Name == key).FirstOrDefault();
        return true;
    }

    bool TryGetSmartImplant(string key, out SmartImplant? smartImplant)
    {
        if (options.Value.SmartImplants == null)
        {
            smartImplant = null;
            return false;
        }

        smartImplant = options.Value.SmartImplants.Where(e => e.Name == key).FirstOrDefault();
        return true;
    }
}
