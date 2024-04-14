using Quartz;
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
    ILightSwitch CreateLightSwitch(Core.Models.LightSwitch lightSwitch, bool isZwave = false);
    ILightSwitch CreateLightSwitch(PowerSwitch powerSwitch, bool isZwave = false);
    IDimmerLightSwitch CreateDimmerLightSwitch(Core.Models.LightSwitch lightSwitch, bool isZwave = false);
}

internal class LightSwitchFactory(
    IStateStore stateStore,
    ISchedulerFactory schedulerFactory,
    IHomebridgeEventSender homebridgeEventSender,
    IZwaveEventSender zwaveEventSender
) : ILightSwitchFactory
{
    public ILightSwitch CreateLightSwitch(Core.Models.LightSwitch lightSwitch, bool isZwave = false) =>
        new Devices.LightSwitch(stateStore, schedulerFactory, homebridgeEventSender, zwaveEventSender, lightSwitch.name, isZwave)
            .Connect();

    public ILightSwitch CreateLightSwitch(PowerSwitch powerSwitch, bool isZwave = false) =>
        new Devices.LightSwitch(stateStore, schedulerFactory, homebridgeEventSender, zwaveEventSender, powerSwitch.name, isZwave)
            .Connect();

    public IDimmerLightSwitch CreateDimmerLightSwitch(Core.Models.LightSwitch lightSwitch, bool isZwave = false) =>
        new DimmerLightSwitch(stateStore, schedulerFactory, homebridgeEventSender, zwaveEventSender, lightSwitch.name, isZwave)
            .Connect();
}
