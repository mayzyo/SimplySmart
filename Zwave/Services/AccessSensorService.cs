using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimplySmart.Core.Models;
using SimplySmart.DeviceStates.Services;
using SimplySmart.Zwave.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Zwave.Services;

public interface IAccessSensorService
{
    IAccessControl? this[string name] { get; }
}

internal class AccessSensorService(
    IOptions<ApplicationConfig> options,
    ILogger<IAccessSensorService> logger,
    IGarageDoorService garageDoorService
) : IAccessSensorService
{
    public IAccessControl? this[string key]
    {
        get
        {
            if (TryGetDoorWindowSensor(key, out DoorWindowSensor? doorWindowSensor) && doorWindowSensor != null)
            {
                if(doorWindowSensor.garageDoor != null)
                {
                    return garageDoorService[doorWindowSensor.garageDoor];
                }
            }

            logger.LogError($"Access Control with {key} does not exist");
            return null;
        }
    }

    bool TryGetDoorWindowSensor(string key, out DoorWindowSensor? doorWindowSensor)
    {
        if (options.Value.doorWindowSensors is null)
        {
            doorWindowSensor = null;
            return false;
        }

        doorWindowSensor = options.Value.doorWindowSensors
            .FirstOrDefault(e => e.name == key && e.garageDoor != null);
        return true;
    }
}
