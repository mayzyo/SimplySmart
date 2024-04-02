using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimplySmart.Core.Models;
using SimplySmart.HouseStates.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.HouseStates.Services;

public interface IAreaOccupantService
{
    IAreaOccupant this[string key] { get; }
    bool Exists(string key);
}

internal class AreaOccupantService : IAreaOccupantService
{
    public IAreaOccupant this[string key]
    {
        get
        {
            if (!states.TryGetValue(key, out IAreaOccupant? value))
            {
                throw new Exception($"Area Occupant with {key} does not exist");
            }

            return value;
        }
    }

    private readonly ILogger<AreaOccupantService> logger;
    private readonly Dictionary<string, IAreaOccupant> states = [];

    public AreaOccupantService(IOptions<ApplicationConfig> options, ILogger<AreaOccupantService> logger, IAreaOccupantFactory areaOccupantFactory)
    {
        this.logger = logger;

        if (options.Value.cameras != null)
        {
            InitialiseCamera(options.Value, areaOccupantFactory);
        }

        if (options.Value.multiSensors != null)
        {
            InitialiseMultiSensor(options.Value, areaOccupantFactory);
        }
    }

    private void InitialiseCamera(ApplicationConfig appConfig, IAreaOccupantFactory areaOccupantFactory)
    {
        foreach (var config in appConfig.cameras)
        {
            var areaOccupant = areaOccupantFactory.CreateAreaOccupant(config.lightSwitch);
            states.Add(config.name, areaOccupant);
        }

        logger.LogInformation("Cameras loaded successfully in Area Occupant Service");
    }

    private void InitialiseMultiSensor(ApplicationConfig appConfig, IAreaOccupantFactory areaOccupantFactory)
    {
        foreach (var config in appConfig.multiSensors)
        {
            var areaOccupant = areaOccupantFactory.CreateAreaOccupant(config.lightSwitch);
            states.Add(config.name, areaOccupant);
        }

        logger.LogInformation("Multi Sensors loaded successfully in Area Occupant Service");
    }

    public bool Exists(string key)
    {
        return states.ContainsKey(key);
    }
}
