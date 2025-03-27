using SimplySmart.Core.Abstractions;
using SimplySmart.Core.Models;
using SimplySmart.DeviceStates.Services;
using SimplySmart.HouseStates.Areas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.HouseStates.Factories;
public interface IAreaOccupantFactory
{
    IAreaOccupant CreateAreaOccupant(Camera camera);
    IAreaOccupant CreateAreaOccupant(MultiSensor multisensor);
}

internal class AreaOccupantFactory(IStateStore stateStorageService, ILightSwitchService lightSwitchService) : IAreaOccupantFactory
{
    public IAreaOccupant CreateAreaOccupant(Camera camera)
    {
        if(camera.LightSwitch != null)
        {
            var lightSwitch = lightSwitchService[camera.LightSwitch];

            if(lightSwitch != null)
            {
                return new AreaOccupant(stateStorageService, camera.Name).Connect(lightSwitch);
            }
        }

        return new AreaOccupant(stateStorageService, camera.Name).Connect();
    }

    public IAreaOccupant CreateAreaOccupant(MultiSensor multisensor)
    {
        if (multisensor.LightSwitch != null)
        {
            var lightSwitch = lightSwitchService[multisensor.LightSwitch];

            if (lightSwitch != null)
            {
                return new AreaOccupant(stateStorageService, multisensor.Name).Connect(lightSwitch);
            }
        }

        return new AreaOccupant(stateStorageService, multisensor.Name).Connect();
    }
}
