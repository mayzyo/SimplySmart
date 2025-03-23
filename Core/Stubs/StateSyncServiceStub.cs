using Microsoft.Extensions.Logging;
using SimplySmart.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Core.Stubs;

internal class StateSyncServiceStub(ILogger<IStateSyncService> logger, EventBusService eventBusService) : IStateSyncService
{
    public async Task Synchronise()
    {
        logger.LogInformation("Synchronise device state stub ran...");
        await eventBusService.CompleteSyncStateAsync();
    }
}
