using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Zwave.Abstractions;

public interface IMultiLevelSwitch
{
    Task SetLevel(ushort level);
}
