using SimplySmart.Core.Models;
using SimplySmart.DeviceStates.Devices;
using SimplySmart.DeviceStates.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.DeviceStates.Factories;

public interface IFobFactory
{
    IFob CreateFob(IList<FobButton> fobButtons);
}

internal class FobFactory(IGarageDoorService accessPointService) : IFobFactory
{
    public IFob CreateFob(IList<FobButton> fobButtons) => new Devices.Fob(accessPointService, fobButtons);
}
