using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.DeviceStates.Devices;

public interface IAppliance
{

}

public enum ApplianceState
{
    ON,
    OFF,
    PENDING_ON,
    PENDING_OFF
}

public enum ApplianceCommand
{
    TURN_ON,
    TURN_OFF,
    SET_ON,
    SET_OFF
}