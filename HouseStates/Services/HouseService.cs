using Microsoft.Extensions.Logging;
using SimplySmart.HouseStates.Factories;
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
}

internal class HouseService : IHouseService
{
    private readonly ILogger<HouseService> logger;
    private readonly IHouseSecurity security;
    private readonly IAutoLight autoLight;

    public IHouseSecurity Security { get { return security; } }
    public IAutoLight AutoLight { get { return autoLight; } }

    public HouseService(ILogger<HouseService> logger, IAutoLightFactory autoLightFactory, IHouseSecurityFactory houseSecurityFactory)
    {
        this.logger = logger;

        security = houseSecurityFactory.CreateHouseSecurity();
        autoLight = autoLightFactory.CreateAutoLight();
    }
}