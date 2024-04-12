using SimplySmart.Core.Abstractions;
using SimplySmart.Core.Models;
using SimplySmart.DeviceStates.Devices;
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
    ILightSwitch CreateLightSwitch(Core.Models.LightSwitch lightSwitch, int? stayOn);
    ILightSwitch CreateLightSwitch(PowerSwitch powerSwitch, int? stayOn);
    IDimmerLightSwitch CreateDimmerLightSwitch(Core.Models.LightSwitch lightSwitch, int? stayOn);
}

internal class LightSwitchFactory(IStateStore stateStorageService, IHomebridgeEventSender homebridgeEventSender, IZwaveEventSender zwaveEventSender) : ILightSwitchFactory
{
    public ILightSwitch CreateLightSwitch(Core.Models.LightSwitch lightSwitch, int? stayOn) =>
        new Devices.LightSwitch(stateStorageService, homebridgeEventSender, zwaveEventSender, lightSwitch.name, stayOn)
            .Connect();

    public ILightSwitch CreateLightSwitch(PowerSwitch powerSwitch, int? stayOn) =>
        new Devices.LightSwitch(stateStorageService, homebridgeEventSender, zwaveEventSender, powerSwitch.name, stayOn)
            .Connect();

    public IDimmerLightSwitch CreateDimmerLightSwitch(Core.Models.LightSwitch lightSwitch, int? stayOn) =>
        new DimmerLightSwitch(stateStorageService, homebridgeEventSender, zwaveEventSender, lightSwitch.name, stayOn)
            .Connect();
}
