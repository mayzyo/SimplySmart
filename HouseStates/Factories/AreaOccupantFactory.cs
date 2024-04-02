using SimplySmart.DeviceStates.Services;
using SimplySmart.HouseStates.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.HouseStates.Factories;
public interface IAreaOccupantFactory
{
    IAreaOccupant CreateAreaOccupant(string? lightSwitch);
}

internal class AreaOccupantFactory(ILightSwitchService lightSwitchService) : IAreaOccupantFactory
{
    public IAreaOccupant CreateAreaOccupant(string? lightSwitch) => new AreaOccupant(lightSwitchService, lightSwitch);
}
