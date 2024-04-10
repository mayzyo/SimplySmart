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

internal class AreaOccupantFactory(IStateStorageService stateStorageService, ILightSwitchService lightSwitchService) : IAreaOccupantFactory
{
    public IAreaOccupant CreateAreaOccupant(Camera camera)
    {
        if(camera.lightSwitch != null)
        {
            var lightSwitch = lightSwitchService[camera.lightSwitch];

            if(lightSwitch != null)
            {
                return new AreaOccupant(stateStorageService, camera.name).Connect(lightSwitch);
            }
        }

        return new AreaOccupant(stateStorageService, camera.name).Connect();
    }

    public IAreaOccupant CreateAreaOccupant(MultiSensor multisensor)
    {
        if (multisensor.lightSwitch != null)
        {
            var lightSwitch = lightSwitchService[multisensor.lightSwitch];

            if (lightSwitch != null)
            {
                return new AreaOccupant(stateStorageService, multisensor.name).Connect(lightSwitch);
            }
        }

        return new AreaOccupant(stateStorageService, multisensor.name).Connect();
    }
}
