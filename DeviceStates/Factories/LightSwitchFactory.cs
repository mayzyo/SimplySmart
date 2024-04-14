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
    ILightSwitch CreateLightSwitch(Core.Models.LightSwitch lightSwitch);
    ILightSwitch CreateLightSwitch(PowerSwitch powerSwitch);
    IDimmerLightSwitch CreateDimmerLightSwitch(Core.Models.LightSwitch lightSwitch);
}

internal class LightSwitchFactory(
    IStateStore stateStore,
    ISchedulerFactory schedulerFactory,
    IHomebridgeEventSender homebridgeEventSender,
    IZwaveEventSender zwaveEventSender
) : ILightSwitchFactory
{
    public ILightSwitch CreateLightSwitch(Core.Models.LightSwitch lightSwitch) =>
        new Devices.LightSwitch(stateStore, schedulerFactory, homebridgeEventSender, zwaveEventSender, lightSwitch.name)
            .Connect();

    public ILightSwitch CreateLightSwitch(PowerSwitch powerSwitch) =>
        new Devices.LightSwitch(stateStore, schedulerFactory, homebridgeEventSender, zwaveEventSender, powerSwitch.name)
            .Connect();

    public IDimmerLightSwitch CreateDimmerLightSwitch(Core.Models.LightSwitch lightSwitch) =>
        new DimmerLightSwitch(stateStore, schedulerFactory, homebridgeEventSender, zwaveEventSender, lightSwitch.name)
            .Connect();
}
