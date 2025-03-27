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

public interface IFanFactory
{
    IFan CreateFan(PowerSwitch config);
}

internal class FanFactory(
    IStateStore stateStore,
    IHomebridgeEventSender homebridgeEventSender,
    IZwaveEventSender zwaveEventSender
) : IFanFactory
{
    public IFan CreateFan(PowerSwitch config) =>
        new Fan(stateStore, homebridgeEventSender, zwaveEventSender, config.Name)
            .Connect();
}
