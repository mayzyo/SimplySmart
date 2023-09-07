using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.States;

public interface IHouseManager
{
    IHouseSecurity Security { get; }

    IAutoLight AutoLight { get; }
}

internal class HouseManager : IHouseManager
{
    private readonly ILogger<HouseManager> logger;
    private readonly IHouseSecurity security;
    private readonly IAutoLight autoLight;

    public IHouseSecurity Security { get { return security; } }
    public IAutoLight AutoLight { get { return autoLight; } }

    public HouseManager(ILogger<HouseManager> logger, IServiceProvider serviceProvider, ILightSwitchManager lightSwitchManager)
    {
        this.logger = logger;

        security = InitialiseSecurity(serviceProvider);
        autoLight = InitialiseAutoLight(lightSwitchManager, serviceProvider);
    }

    private static IHouseSecurity InitialiseSecurity(IServiceProvider serviceProvider)
    {
        var security = new HouseSecurity();
        security.Initialise(serviceProvider);

        return security;
    }

    private static IAutoLight InitialiseAutoLight(ILightSwitchManager lightSwitchManager, IServiceProvider serviceProvider)
    {
        var autoLight = new AutoLight();
        autoLight.Initialise(lightSwitchManager, serviceProvider);

        return autoLight;
    }
}