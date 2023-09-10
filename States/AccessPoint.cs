using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.States;

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