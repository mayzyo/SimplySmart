using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimplySmart.Core.Models;
using SimplySmart.HouseStates.Areas;
using SimplySmart.HouseStates.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.HouseStates.Services;

public interface IAccessControlService
{
    IAccessControl? this[string key] { get; }
}

internal class AccessControlService(IOptions<ApplicationConfig> options, ILogger<IAccessControlService> logger, IAccessControlFactory accessControlFactory) : IAccessControlService
{
    public IAccessControl? this[string key]
    {
        get
        {
            if (TryGetDoorWindowSensor(key, out DoorWindowSensor? doorWindowSensor) && doorWindowSensor != null)
            {
                return accessControlFactory.CreateAccessControl(doorWindowSensor);
            }

            logger.LogError($"Access Control with {key} does not exist");
            return null;
        }
    }

    bool TryGetDoorWindowSensor(string key, out DoorWindowSensor? doorWindowSensor)
    {
        if (options.Value.doorWindowSensor is null)
        {
            doorWindowSensor = null;
            return false;
        }

        doorWindowSensor = options.Value.doorWindowSensor.Where(e => e.name == key).FirstOrDefault();
        return true;
    }
}
