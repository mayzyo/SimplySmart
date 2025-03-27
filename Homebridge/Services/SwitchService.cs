using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimplySmart.Core.Models;
using SimplySmart.DeviceStates.Factories;
using SimplySmart.Homebridge.Abstractions;
using SimplySmart.HouseStates.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Homebridge.Services;

public interface ISwitchService
{
    ISwitch? this[string key] { get; }
}

internal class SwitchService(
    IOptions<ApplicationConfig> options,
    ILogger<ISwitchService> logger,
    IGarageDoorFactory garageDoorFactory,
    IAutoLightFactory autoLightFactory
) : ISwitchService
{
    public ISwitch? this[string key]
    {
        get
        {
            if(key == "auto_light/Office/0/0")
            {
                return autoLightFactory.CreateAutoLight();
            }

            if (TryGetSmartImplant(key, out SmartImplant? smartImplant) && smartImplant != null)
            {
                if (smartImplant.Type == "garageDoor")
                {
                    return garageDoorFactory.CreateGarageDoor(smartImplant);
                }
            }

            logger.LogError($"Switch with {key} does not exist");
            return null;
        }
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
