using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.DeviceStates.Services;

public interface IAppliance
{

}

public enum ApplianceState
{
    ON,
    OFF
}

public enum ApplianceCommand
{
    ON,
    OFF
}