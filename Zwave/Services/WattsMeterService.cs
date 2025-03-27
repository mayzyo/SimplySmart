using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimplySmart.Core.Models;
using SimplySmart.DeviceStates.Services;
using SimplySmart.Zwave.Abstractions;
using SimplySmart.Zwave.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimplySmart.Zwave.Services;

public interface IWattsMeterService
{
    IWattsMeter? this[string name] { get; }
}

internal class WattsMeterService(
    IOptions<ApplicationConfig> options,
    ILogger<IWattsMeterService> logger,
    ILightSwitchService lightSwitchService
) : IWattsMeterService
{
    public IWattsMeter? this[string key]
    {
        get
        {
            if (TryGetPowerSwitch(key, out WattsSensor? voltageSensor) && voltageSensor != null)
            {
                if (voltageSensor.LightSwitch != null)
                {
                    var binarySwitch = lightSwitchService[voltageSensor.LightSwitch];
                    if (binarySwitch != null)
                    {
                        if (voltageSensor.Type == "over")
                        {
                            return new OverWattageSwitch
                            {
                                BinarySwitch = binarySwitch,
                                Threshold = voltageSensor.Threshold
                            };
                        }
                        else
                        {
                            return new UnderWattageSwitch
                            {
                                BinarySwitch = binarySwitch,
                                Threshold = voltageSensor.Threshold
                            };
                        }
                    }
                }
            }

            logger.LogError($"Watts Sensor with {key} does not exist");
            return null;
        }
    }

    bool TryGetPowerSwitch(string key, out WattsSensor? voltageSensor)
    {
        if (options.Value.PowerSwitches is null)
        {
            voltageSensor = null;
            return false;
        }

        voltageSensor = options.Value.PowerSwitches
            .FirstOrDefault(e => e.Name == key && e.WattsSensor != null)?.WattsSensor;
        return true;
    }
}
