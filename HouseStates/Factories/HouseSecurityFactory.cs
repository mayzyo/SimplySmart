using SimplySmart.Core.Abstractions;
using SimplySmart.Homebridge.Services;
using SimplySmart.HouseStates.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.HouseStates.Factories;

public interface IHouseSecurityFactory
{
    IHouseSecurity CreateHouseSecurity();
}

internal class HouseSecurityFactory(IStateStore stateStorageService, IHomebridgeEventSender homebridgeEventSender) : IHouseSecurityFactory
{
    public IHouseSecurity CreateHouseSecurity() => new HouseSecurity(stateStorageService, homebridgeEventSender).Connect();
}
