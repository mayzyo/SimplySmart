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

public interface IFanService
{
    IFan? this[string key] { get; }
    Task PublishAll();
}

internal class FanService(IOptions<ApplicationConfig> options, ILogger<IFanService> logger, IFanFactory applianceFactory) : IFanService
{
    public IFan? this[string key]
    {
        get
        {
            if (TryGetPowerSwitch(key, out PowerSwitch? powerSwitch) && powerSwitch != null)
            {
                return applianceFactory.CreateFan(powerSwitch);
            }

            logger.LogError($"Fan with {key} does not exist");
            return null;
        }
    }

    public async Task PublishAll()
    {
        foreach (var powerSwitch in GetAllPowerSwitch())
        {
            await applianceFactory.CreateFan(powerSwitch).Publish();
        }
    }

    bool TryGetPowerSwitch(string key, out PowerSwitch? powerSwitch)
    {
        if(options.Value.powerSwitches == null)
        {
            powerSwitch = null;
            return false;
        }

        powerSwitch = options.Value.powerSwitches.Where(e => e.name == key).FirstOrDefault();
        return true;
    }

    IEnumerable<PowerSwitch> GetAllPowerSwitch()
    {
        if (options.Value.powerSwitches == null)
        {
            return [];
        }

        return options.Value.powerSwitches.Where(e => e.type == "fan");
    }
}
