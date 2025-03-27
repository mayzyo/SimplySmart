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

public interface IGarageDoorService
{
    IGarageDoor? this[string key] { get; }
    Task PublishAll();
}

internal class GarageDoorService(IOptions<ApplicationConfig> options, ILogger<IGarageDoorService> logger, IGarageDoorFactory garageDoorFactory) : IGarageDoorService
{
    public IGarageDoor? this[string key]
    {
        get
        {
            if (TryGetSmartImplant(key, out SmartImplant? smartImplant) && smartImplant != null)
            {
                return garageDoorFactory.CreateGarageDoor(smartImplant);
            }

            logger.LogError($"Garage Door with {key} does not exist");
            return null;
        }
    }

    public async Task PublishAll()
    {
        foreach (var smartImplant in GetAllSmartImplant())
        {
            await garageDoorFactory.CreateGarageDoor(smartImplant).Publish();
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

    IEnumerable<SmartImplant> GetAllSmartImplant()
    {
        if (options.Value.SmartImplants == null)
        {
            return [];
        }

        return options.Value.SmartImplants.Where(e => e.Type == "garageDoor");
    }
}
