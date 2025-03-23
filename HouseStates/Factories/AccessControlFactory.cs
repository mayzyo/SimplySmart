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
public interface IAccessControlFactory
{
    IAccessControl CreateAccessControl(DoorWindowSensor doorWindowSensor);
}

internal class AccessControlFactory(IStateStore stateStore, IGarageDoorService garageDoorService) : IAccessControlFactory
{
    public IAccessControl CreateAccessControl(DoorWindowSensor doorWindowSensor)
    {
        if(doorWindowSensor.smartImplant != null)
        {
            var garageDoor = garageDoorService[doorWindowSensor.smartImplant];

            if(garageDoor != null)
            {
                return new AccessControl(stateStore, doorWindowSensor.name).Connect(garageDoor);
            }
        }

        return new AccessControl(stateStore, doorWindowSensor.name).Connect();
    }
}
