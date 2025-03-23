using SimplySmart.HouseStates.Factories;
using SimplySmart.HouseStates.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.HouseStates.Services;

/// <summary>
/// Represents the state of house based smart features.
/// This excludes explicit IOT devices, and is focused on the state such as security alert level.
/// </summary>
public interface IHouseService
{
    IHouseSecurity Security { get; }
    IAutoLight AutoLight { get; }
    Task PublishAll();
}

internal class HouseService(IAutoLightFactory autoLightFactory, IHouseSecurityFactory houseSecurityFactory) : IHouseService
{
    public IHouseSecurity Security {
        get { return houseSecurityFactory.CreateHouseSecurity(); }
    }
    public IAutoLight AutoLight {
        get { return autoLightFactory.CreateAutoLight(); }
    }

    public async Task PublishAll()
    {
        await Security.Publish();
        await AutoLight.Publish();
    }
}