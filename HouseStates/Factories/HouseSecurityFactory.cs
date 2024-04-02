using SimplySmart.Core.Services;
using SimplySmart.Homebridge.Services;
using SimplySmart.HouseStates.Services;
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

internal class HouseSecurityFactory(IStateStorageService stateStorageService, IHomebridgeEventSender homebridgeEventSender) : IHouseSecurityFactory
{
    public IHouseSecurity CreateHouseSecurity() => new HouseSecurity(stateStorageService, homebridgeEventSender);
}
