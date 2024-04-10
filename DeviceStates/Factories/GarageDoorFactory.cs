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

internal class GarageDoorFactory(IStateStorageService stateStorageService, IHomebridgeEventSender homebridgeEventSender, IZwaveEventSender zwaveEventSender) : IGarageDoorFactory
{
    public IGarageDoor CreateGarageDoor(SmartImplant config) => 
        new GarageDoor(stateStorageService, config.name, homebridgeEventSender, zwaveEventSender)
            .Connect();
}
