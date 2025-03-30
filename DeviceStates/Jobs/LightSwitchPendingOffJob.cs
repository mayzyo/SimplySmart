using Quartz;
using SimplySmart.DeviceStates.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.DeviceStates.Jobs;

internal class LightSwitchPendingOffJob(ILightSwitchService lightSwitchService) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var name = context.JobDetail.Key.Name.Replace("_PendingOffJob", "");
        await (lightSwitchService[name]?.CompleteScheduledOff() ?? Task.CompletedTask);
    }
}
