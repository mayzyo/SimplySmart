using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.HouseStates.Abstractions;

public interface IAutoSwitch
{
    Task AutoSetToOn();
    Task AutoSetToOff();
}
