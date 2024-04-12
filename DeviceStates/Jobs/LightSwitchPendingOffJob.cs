using Quartz;
using SimplySmart.DeviceStates.Devices;
using SimplySmart.DeviceStates.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.DeviceStates.Jobs;

internal class LightSwitchPendingOffJob(ILightSwitchService lightSwitchService) : IJob
{
    string name = "";

    public async Task Execute(IJobExecutionContext context)
    {
        var lightSwitch = lightSwitchService[name];
        await ((LightSwitch)lightSwitch).stateMachine.FireAsync(LightSwitchCommand.AUTO_OFF);
    }
}
