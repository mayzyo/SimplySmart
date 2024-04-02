using SimplySmart.Core.Models;
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

public interface IApplianceFactory
{
    IFan CreateFan(PowerSwitch config);
}

internal class ApplianceFactory(IStateStorageService stateStorageService, IHomebridgeEventSender homebridgeEventSender, IZwaveEventSender zwaveEventSender) : IApplianceFactory
{
    public IFan CreateFan(PowerSwitch config) => new Fan(stateStorageService, config.name, homebridgeEventSender, zwaveEventSender);
}
