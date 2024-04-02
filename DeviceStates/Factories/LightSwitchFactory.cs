using SimplySmart.Core.Services;
using SimplySmart.DeviceStates.Services;
using SimplySmart.Homebridge.Services;
using SimplySmart.Zwave.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.DeviceStates.Factories;

public interface ILightSwitchFactory
{
    ILightSwitch CreateLightSwitch(string name, int? stayOn);
    ILightSwitch CreateDimmerLightSwitch(string name, int? stayOn);
}

internal class LightSwitchFactory(IStateStorageService stateStorageService, IHomebridgeEventSender homebridgeEventSender, IZwaveEventSender zwaveEventSender) : ILightSwitchFactory
{
    public ILightSwitch CreateLightSwitch(string name, int? stayOn) => new LightSwitch(stateStorageService, homebridgeEventSender, zwaveEventSender, name, stayOn);

    public ILightSwitch CreateDimmerLightSwitch(string name, int? stayOn) => new DimmerLightSwitch(stateStorageService, homebridgeEventSender, zwaveEventSender, name, stayOn);
}
