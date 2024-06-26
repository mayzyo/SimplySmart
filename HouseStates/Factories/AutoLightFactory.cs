﻿using SimplySmart.Core.Abstractions;
using SimplySmart.DeviceStates.Services;
using SimplySmart.Homebridge.Services;
using SimplySmart.HouseStates.Features;
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

internal class AutoLightFactory(IStateStore stateStorageService, IHomebridgeEventSender homebridgeEventSender, ILightSwitchService lightSwitchService) : IAutoLightFactory
{
    public IAutoLight CreateAutoLight() => new AutoLight(stateStorageService, homebridgeEventSender, lightSwitchService).Connect();
}
