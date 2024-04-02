using SimplySmart.Core.Services;
using SimplySmart.DeviceStates.Services;
using SimplySmart.Homebridge.Services;
using SimplySmart.HouseStates.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.HouseStates.Factories;

public interface IAutoLightFactory
{
    IAutoLight CreateAutoLight();
}

internal class AutoLightFactory(IStateStorageService stateStorageService, IHomebridgeEventSender homebridgeEventSender, ILightSwitchService lightSwitchService) : IAutoLightFactory
{
    public IAutoLight CreateAutoLight() => new AutoLight(stateStorageService, homebridgeEventSender, lightSwitchService);
}
