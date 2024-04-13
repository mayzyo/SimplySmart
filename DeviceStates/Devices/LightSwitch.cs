using Quartz;
using SimplySmart.Core.Abstractions;
using SimplySmart.DeviceStates.Jobs;
using SimplySmart.Homebridge.Services;
using SimplySmart.HouseStates.Abstractions;
using SimplySmart.Zwave.Abstractions;
using SimplySmart.Zwave.Services;
using Stateless;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.DeviceStates.Devices;

public interface ILightSwitch : IBinarySwitch, IAutoSwitch
{
    LightSwitchState State { get; }
    Task Publish();
    Task EnableAuto();
    Task DisableAuto();
    Task CompletePendingOff();
}

public class LightSwitch : ILightSwitch
{
    const int STAY_ON_INCREMENT = 60000;

    public LightSwitchState State { get { return stateMachine.State; } }
    public bool PendingTrigger { get { return true; } }
    public readonly StateMachine<LightSwitchState, LightSwitchCommand> stateMachine;
    public readonly string name;

    protected readonly IStateStore stateStore;
    protected readonly ISchedulerFactory schedulerFactory;
    protected readonly IHomebridgeEventSender homebridgeEventSender;
    protected readonly IZwaveEventSender zwaveEventSender;
    protected int StayOnCount
    {
        get
        {
            var countString = stateStore.GetState(name + "_stayon");
            if (int.TryParse(countString, out int count))
            {
                return count;
            }

            return 1;
        }
        set { stateStore.UpdateState(name + "_stayon", value.ToString()); }
    }

    public LightSwitch(IStateStore stateStore, ISchedulerFactory schedulerFactory, IHomebridgeEventSender homebridgeEventSender, IZwaveEventSender zwaveEventSender, string name)
    {
        this.stateStore = stateStore;
        this.schedulerFactory = schedulerFactory;
        this.homebridgeEventSender = homebridgeEventSender;
        this.zwaveEventSender = zwaveEventSender;
        this.name = name;

        stateMachine = new(
            InitialState,
            s => stateStore.UpdateState(name, s.ToString())
        );
    }

    public virtual ILightSwitch Connect()
    {
        stateMachine.Configure(LightSwitchState.OFF)
            .OnEntryAsync(SendOffEvents)
            .OnEntry(() => StayOnCount = 0)
            .OnActivateAsync(SendOffEvents)
            .Permit(LightSwitchCommand.TURN_ON, LightSwitchState.ON)
            .Permit(LightSwitchCommand.ENABLE_AUTO, LightSwitchState.AUTO_OFF)
            .Ignore(LightSwitchCommand.TURN_OFF)
            .Ignore(LightSwitchCommand.AUTO_OFF)
            .Ignore(LightSwitchCommand.AUTO_ON)
            .Ignore(LightSwitchCommand.COMPLETE_PENDING);

        ConfigureOnToOnGuardClause();
        stateMachine.Configure(LightSwitchState.ON)
            .OnEntryAsync(SendOnEvents)
            .OnActivateAsync(SendOnEvents)
            .Permit(LightSwitchCommand.TURN_OFF, LightSwitchState.OFF)
            .Permit(LightSwitchCommand.ENABLE_AUTO, LightSwitchState.AUTO_FORCED_ON)
            .Ignore(LightSwitchCommand.AUTO_OFF)
            .Ignore(LightSwitchCommand.AUTO_ON)
            .Ignore(LightSwitchCommand.COMPLETE_PENDING);

        stateMachine.Configure(LightSwitchState.AUTO_OFF)
            .SubstateOf(LightSwitchState.OFF)
            .Permit(LightSwitchCommand.AUTO_ON, LightSwitchState.AUTO_ON)
            .Permit(LightSwitchCommand.TURN_ON, LightSwitchState.AUTO_FORCED_ON)
            .Permit(LightSwitchCommand.DISABLE_AUTO, LightSwitchState.OFF)
            .Ignore(LightSwitchCommand.AUTO_OFF);

        stateMachine.Configure(LightSwitchState.AUTO_ON)
            .SubstateOf(LightSwitchState.ON)
            .OnEntry(() =>
            {
                var count = StayOnCount;
                if (count < 4)
                {
                    StayOnCount = count + 1;
                }
            })
            .Permit(LightSwitchCommand.AUTO_OFF, LightSwitchState.AUTO_PENDING_OFF)
            .Permit(LightSwitchCommand.TURN_OFF, LightSwitchState.AUTO_OFF)
            .Permit(LightSwitchCommand.DISABLE_AUTO, LightSwitchState.OFF)
            .Ignore(LightSwitchCommand.AUTO_ON)
            .Ignore(LightSwitchCommand.TURN_ON);

        stateMachine.Configure(LightSwitchState.AUTO_PENDING_OFF)
            .SubstateOf(LightSwitchState.AUTO_ON)
            .OnEntryAsync(SchedulePendingOffJob)
            .OnActivateAsync(SchedulePendingOffJob)
            .OnExitAsync(CancelPendingOffJob)
            .Permit(LightSwitchCommand.AUTO_ON, LightSwitchState.AUTO_ON)
            .Permit(LightSwitchCommand.COMPLETE_PENDING, LightSwitchState.AUTO_OFF)
            .Ignore(LightSwitchCommand.AUTO_OFF);

        stateMachine.Configure(LightSwitchState.AUTO_FORCED_ON)
            .SubstateOf(LightSwitchState.ON)
            .Permit(LightSwitchCommand.TURN_OFF, LightSwitchState.AUTO_OFF)
            .Permit(LightSwitchCommand.DISABLE_AUTO, LightSwitchState.ON);

        return this;
    }

    public async Task Publish()
    {
        await stateMachine.ActivateAsync();
    }

    public virtual async Task SetToOn(bool isOn)
    {
        var command = isOn ? LightSwitchCommand.TURN_ON : LightSwitchCommand.TURN_OFF;
        await stateMachine.FireAsync(command);
    }

    public virtual async Task AutoSetToOn()
    {
        await stateMachine.FireAsync(LightSwitchCommand.AUTO_ON);
    }

    public virtual async Task AutoSetToOff()
    {
        await stateMachine.FireAsync(LightSwitchCommand.AUTO_OFF);
    }

    public async Task EnableAuto()
    {
        await stateMachine.FireAsync(LightSwitchCommand.ENABLE_AUTO);
    }

    public async Task DisableAuto()
    {
        await stateMachine.FireAsync(LightSwitchCommand.DISABLE_AUTO);
    }

    public async Task CompletePendingOff()
    {
        await stateMachine.FireAsync(LightSwitchCommand.COMPLETE_PENDING);
    }

    protected virtual void ConfigureOnToOnGuardClause()
    {
        stateMachine.Configure(LightSwitchState.ON)
            .Ignore(LightSwitchCommand.TURN_ON);
    }

    protected virtual LightSwitchState InitialState()
    {
        var stateString = stateStore.GetState(name);
        if (Enum.TryParse(stateString, out LightSwitchState state))
        {
            return state;
        }

        return LightSwitchState.OFF;
    }

    protected virtual async Task SendOnEvents()
    {
        await zwaveEventSender.BinarySwitchOn(name);
        await homebridgeEventSender.LightSwitchOn(name);
    }

    protected virtual async Task SendOffEvents()
    {
        await zwaveEventSender.BinarySwitchOff(name);
        await homebridgeEventSender.LightSwitchOff(name);
    }

    async Task SchedulePendingOffJob()
    {
        var scheduler = await schedulerFactory.GetScheduler();
        var triggerDelayJob = JobBuilder.Create<LightSwitchPendingOffJob>()
            .WithIdentity($"{name}_PendingOffJob")
            .Build();

        var count = StayOnCount;
        var trigger = TriggerBuilder.Create()
            .WithIdentity($"{name}_PendingOffTrigger")
            .StartAt(DateTimeOffset.Now.AddMilliseconds(STAY_ON_INCREMENT * (count * count)))
            .Build();

        await scheduler.ScheduleJob(triggerDelayJob, trigger);
    }

    async Task CancelPendingOffJob()
    {
        var scheduler = await schedulerFactory.GetScheduler();
        var jobKey = new JobKey($"{name}_PendingOffJob");
        if (await scheduler.CheckExists(jobKey))
        {
            await scheduler.DeleteJob(jobKey);
        }
    }
}

public enum LightSwitchState
{
    ON,
    OFF,
    AUTO_FORCED_ON,
    AUTO_ON,
    AUTO_OFF,
    AUTO_PENDING_OFF
}

public enum LightSwitchCommand
{
    TURN_ON,
    TURN_OFF,
    AUTO_ON,
    AUTO_OFF,
    COMPLETE_PENDING,
    ENABLE_AUTO,
    DISABLE_AUTO
}
