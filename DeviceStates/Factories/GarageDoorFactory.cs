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
public interface IGarageDoorFactory
{
    IGarageDoor CreateGarageDoor(SmartImplant config);
}

internal class GarageDoorFactory(IStateStore stateStore, ISchedulerFactory schedulerFactory, IHomebridgeEventSender homebridgeEventSender, IZwaveEventSender zwaveEventSender) : IGarageDoorFactory
{
    public IGarageDoor CreateGarageDoor(SmartImplant config) => 
        new GarageDoor(stateStore, schedulerFactory, homebridgeEventSender, zwaveEventSender, config.name)
            .Connect();
}
