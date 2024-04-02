using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.DeviceStates.Services;

public interface IAccessPoint
{

}

public enum AccessPointState
{
    ON,
    OFF
}

public enum AccessPointCommand
{
    ON,
    OFF
}