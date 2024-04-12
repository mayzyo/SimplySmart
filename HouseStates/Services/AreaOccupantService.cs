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

public interface IAreaOccupantService
{
    IAreaOccupant? this[string key] { get; }
}

internal class AreaOccupantService(IOptions<ApplicationConfig> options, ILogger<IAreaOccupantService> logger, IAreaOccupantFactory areaOccupantFactory) : IAreaOccupantService
{
    public IAreaOccupant? this[string key]
    {
        get
        {
            if (TryGetCamera(key, out Camera? camera) && camera != null)
            {
                return areaOccupantFactory.CreateAreaOccupant(camera);
            }

            if (TryGetMultiSensor(key, out MultiSensor? multiSensor) && multiSensor != null)
            {
                return areaOccupantFactory.CreateAreaOccupant(multiSensor);
            }

            //logger.LogError($"Area Occupant with {key} does not exist");
            return null;
        }
    }

    bool TryGetCamera(string key, out Camera? camera)
    {
        if (options.Value.cameras is null)
        {
            camera = null;
            return false;
        }

        camera = options.Value.cameras.Where(e => e.name == key).FirstOrDefault();
        return true;
    }

    bool TryGetMultiSensor(string key, out MultiSensor? multiSensor)
    {
        if (options.Value.multiSensors is null)
        {
            multiSensor = null;
            return false;
        }

        multiSensor = options.Value.multiSensors.Where(e => e.name == key).FirstOrDefault();
        return true;
    }
}
