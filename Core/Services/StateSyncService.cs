using Microsoft.Extensions.Logging;
using SimplySmart.DeviceStates.Services;
using SimplySmart.HouseStates.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Core.Services;

public interface IStateSyncService
{
    Task Synchronise();
}

internal class StateSyncService(
    ILogger<IStateSyncService> logger,
    EventBusService eventBusService,
    IFanService fanService,
    ILightSwitchService lightSwitchService,
    IGarageDoorService garageDoorService,
    IHouseService houseService
) : IStateSyncService
{
    public async Task Synchronise()
    {
        logger.LogInformation("Synchronise device state between modules started");
        await fanService.PublishAll();
        await lightSwitchService.PublishAll();
        await garageDoorService.PublishAll();

        await houseService.PublishAll();

        await Task.Delay(10000);
        await eventBusService.CompleteSyncStateAsync();
    }
}
