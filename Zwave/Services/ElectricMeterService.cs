using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimplySmart.Core.Models;
using SimplySmart.DeviceStates.Services;
using SimplySmart.Zwave.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimplySmart.Zwave.Services;

public interface IElectricMeterService
{
    IElectricMeter? this[string name] { get; }
}

internal class ElectricMeterService(
    IOptions<ApplicationConfig> options,
    ILogger<IElectricMeterService> logger,
    ILightSwitchService lightSwitchService
) : IElectricMeterService
{
    public IElectricMeter? this[string key]
    {
        get
        {
            //if (TryGetPowerSwitch(key, out PowerSwitch? powerSwitch) && powerSwitch != null)
            //{
            //    if (powerSwitch.threshold?.lightSwitch != null)
            //    {
            //        return lightSwitchService[powerSwitch.threshold?.lightSwitch];
            //    }
            //}

            logger.LogError($"Electric Meter with {key} does not exist");
            return null;
        }
    }

    bool TryGetPowerSwitch(string key, out PowerSwitch? powerSwitch)
    {
        if (options.Value.powerSwitches is null)
        {
            powerSwitch = null;
            return false;
        }

        powerSwitch = options.Value.powerSwitches
            .FirstOrDefault(e => e.name == key && e.type == "sensor");
        return true;
    }
}
