using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Homebridge.Abstractions;

public interface ISwitch
{
    Task SetToOn(bool isOn);
}
