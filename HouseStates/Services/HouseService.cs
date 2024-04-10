using SimplySmart.HouseStates.Factories;
using SimplySmart.HouseStates.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.HouseStates.Services;

public interface IHouseService
{
    IHouseSecurity Security { get; }
    IAutoLight AutoLight { get; }
    void PublishAll();
}

internal class HouseService(IAutoLightFactory autoLightFactory, IHouseSecurityFactory houseSecurityFactory) : IHouseService
{
    public IHouseSecurity Security {
        get { return houseSecurityFactory.CreateHouseSecurity(); }
    }
    public IAutoLight AutoLight {
        get { return autoLightFactory.CreateAutoLight(); }
    }

    public void PublishAll()
    {
        Security.Publish();
        AutoLight.Publish();
    }
}