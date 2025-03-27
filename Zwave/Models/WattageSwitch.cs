using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplySmart.DeviceStates.Devices;
using SimplySmart.Zwave.Abstractions;

namespace SimplySmart.Zwave.Models;

public abstract class WattageSwitch
{
    public required ILightSwitch BinarySwitch { get; set; }

    public int Threshold { get; set; }
}

public class OverWattageSwitch : WattageSwitch, IWattsMeter
{
    public async Task HandleWattage(float watts)
    {
        await BinarySwitch.SetToOn(watts > Threshold);
    }
}

public class UnderWattageSwitch : WattageSwitch, IWattsMeter
{
    public async Task HandleWattage(float watts)
    {
        await BinarySwitch.SetToOn(watts < Threshold);
    }
}
