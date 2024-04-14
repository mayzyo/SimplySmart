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

public interface IMultiLevelSwitchService
{
    IMultiLevelSwitch? this[string key] { get; }
}

internal class MultiLevelSwitchService(
    IOptions<ApplicationConfig> options,
    ILogger<MultiLevelSwitchService> logger,
    ILightSwitchFactory lightSwitchFactory
) : IMultiLevelSwitchService
{
    public IMultiLevelSwitch? this[string key]
    {
        get
        {
            if (TryGetLightSwitch(key, out LightSwitch? lightSwitch) && lightSwitch != null)
            {
                return lightSwitchFactory.CreateDimmerLightSwitch(lightSwitch, true);
            }

            logger.LogError($"Multi Level Switch with {key} does not exist");
            return null;
        }
    }

    bool TryGetLightSwitch(string key, out LightSwitch? lightSwitch)
    {
        if (options.Value.lightSwitches is null)
        {
            lightSwitch = null;
            return false;
        }

        lightSwitch = options.Value.lightSwitches.Where(e => e.name == key && e.isDimmer == true).FirstOrDefault();
        return true;
    }
}
