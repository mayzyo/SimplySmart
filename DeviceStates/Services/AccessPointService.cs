using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimplySmart.Core.Models;
using SimplySmart.DeviceStates.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.DeviceStates.Services;

public interface IAccessPointService
{
    IAccessPoint this[string key] { get; }

    bool Exists(string key);
}

internal class AccessPointService : IAccessPointService
{
    public IAccessPoint this[string key]
    {
        get
        {
            if (!states.TryGetValue(key, out IAccessPoint? value))
            {
                throw new Exception($"Garage door with {key} does not exist");
            }

            return value;
        }
    }

    private readonly ILogger<AccessPointService> logger;
    private readonly Dictionary<string, IAccessPoint> states = [];

    public AccessPointService(IOptions<ApplicationConfig> options, ILogger<AccessPointService> logger, IAccessPointFactory accessPointFactory)
    {
        this.logger = logger;

        if (options.Value.smartImplants != null)
        {
            InitialiseSmartImplant(options.Value, accessPointFactory);
        }
    }

    private void InitialiseSmartImplant(ApplicationConfig appConfig, IAccessPointFactory accessPointFactory)
    {
        foreach (var config in appConfig.smartImplants.Where(e => e.type == "garageDoor"))
        {
            var garageDoor = accessPointFactory.CreateGarageDoor(config);
            states.Add(config.name, garageDoor);
        }

        logger.LogInformation("Smart Implants loaded successfully in Access Point Service");
    }

    public bool Exists(string key)
    {
        return states.ContainsKey(key);
    }
}
