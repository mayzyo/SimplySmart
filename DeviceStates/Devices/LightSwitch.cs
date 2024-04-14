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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.DeviceStates.Devices;

public interface ILightSwitch : IBinarySwitch, IAutoSwitch
{
    LightSwitchState State { get; }
    Task Publish();
    Task SetToOn(bool isOn);
    Task EnableAuto();
    Task DisableAuto();
    Task CompletePendingOff();
}

public class LightSwitch(
    IStateStore stateStore,
    ISchedulerFactory schedulerFactory,
    IHomebridgeEventSender homebridgeEventSender,
    IZwaveEventSender zwaveEventSender,
    string name
) : ILightSwitch
{
    const int STAY_ON_INCREMENT = 60000;

    public LightSwitchState State { get { return stateMachine.State; } }
    public bool PendingTrigger { get { return true; } }
    public readonly StateMachine<LightSwitchState, LightSwitchCommand> stateMachine = new(
            () =>
            {
                var stateString = stateStore.GetState(name);
                if (Enum.TryParse(stateString, out LightSwitchState state))
                {
                    return state;
                }

                return LightSwitchState.OFF;
            },
            s => stateStore.UpdateState(name, s.ToString())
        );
    public readonly string name = name;
    protected readonly IStateStore stateStore = stateStore;
    protected readonly ISchedulerFactory schedulerFactory = schedulerFactory;
    protected readonly IHomebridgeEventSender homebridgeEventSender = homebridgeEventSender;
    protected readonly IZwaveEventSender zwaveEventSender = zwaveEventSender;
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

    public virtual ILightSwitch Connect()
    {
        stateMachine.Configure(LightSwitchState.PENDING_OFF)
            .OnEntryAsync(SendSetToOffEvents)
            .Permit(LightSwitchCommand.SET_OFF, LightSwitchState.OFF)
            .Permit(LightSwitchCommand.SET_ON, LightSwitchState.ON)
            .Ignore(LightSwitchCommand.TURN_OFF)
            .Ignore(LightSwitchCommand.TURN_ON)
            .Ignore(LightSwitchCommand.AUTO_OFF)
            .Ignore(LightSwitchCommand.AUTO_ON);

        stateMachine.Configure(LightSwitchState.OFF)
            .OnEntryAsync(SendCurrentlyOffEvents)
            .OnEntry(() => StayOnCount = 0)
            .Permit(LightSwitchCommand.SET_ON, LightSwitchState.ON)
            .Permit(LightSwitchCommand.TURN_ON, LightSwitchState.PENDING_ON)
            .Permit(LightSwitchCommand.ENABLE_AUTO, LightSwitchState.AUTO_OFF)
            .Ignore(LightSwitchCommand.SET_OFF)
            .Ignore(LightSwitchCommand.TURN_OFF)
            .Ignore(LightSwitchCommand.AUTO_OFF)
            .Ignore(LightSwitchCommand.AUTO_ON)
            .Ignore(LightSwitchCommand.COMPLETE_DELAY);

        stateMachine.Configure(LightSwitchState.PENDING_ON)
            .OnEntryAsync(SendSetToOnEvents)
            .Permit(LightSwitchCommand.SET_ON, LightSwitchState.ON)
            .Permit(LightSwitchCommand.SET_OFF, LightSwitchState.OFF)
            .Ignore(LightSwitchCommand.TURN_OFF)
            .Ignore(LightSwitchCommand.TURN_ON)
            .Ignore(LightSwitchCommand.AUTO_OFF)
            .Ignore(LightSwitchCommand.AUTO_ON);

        stateMachine.Configure(LightSwitchState.ON)
            .OnEntryAsync(SendCurrentlyOnEvents)
            .Permit(LightSwitchCommand.SET_OFF, LightSwitchState.OFF)
            .Permit(LightSwitchCommand.TURN_OFF, LightSwitchState.PENDING_OFF)
            .Permit(LightSwitchCommand.ENABLE_AUTO, LightSwitchState.AUTO_FORCED_ON)
            .Ignore(LightSwitchCommand.SET_ON)
            .Ignore(LightSwitchCommand.TURN_ON)
            .Ignore(LightSwitchCommand.AUTO_OFF)
            .Ignore(LightSwitchCommand.AUTO_ON)
            .Ignore(LightSwitchCommand.COMPLETE_DELAY);

        stateMachine.Configure(LightSwitchState.AUTO_PENDING_OFF)
            .SubstateOf(LightSwitchState.PENDING_OFF)
            .Permit(LightSwitchCommand.SET_OFF, LightSwitchState.AUTO_OFF)
            .Permit(LightSwitchCommand.SET_ON, LightSwitchState.AUTO_ON);

        stateMachine.Configure(LightSwitchState.AUTO_OFF)
            .SubstateOf(LightSwitchState.OFF)
            .Permit(LightSwitchCommand.AUTO_ON, LightSwitchState.AUTO_PENDING_ON)
            .Permit(LightSwitchCommand.SET_ON, LightSwitchState.AUTO_ON)
            .Permit(LightSwitchCommand.TURN_ON, LightSwitchState.AUTO_FORCED_PENDING_ON)
            .Permit(LightSwitchCommand.DISABLE_AUTO, LightSwitchState.OFF)
            .Ignore(LightSwitchCommand.ENABLE_AUTO);

        stateMachine.Configure(LightSwitchState.AUTO_PENDING_ON)
            .SubstateOf(LightSwitchState.PENDING_ON)
            .Permit(LightSwitchCommand.SET_ON, LightSwitchState.AUTO_ON)
            .Permit(LightSwitchCommand.SET_OFF, LightSwitchState.AUTO_OFF);

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
            .Permit(LightSwitchCommand.AUTO_OFF, LightSwitchState.AUTO_DELAYED_OFF)
            .Permit(LightSwitchCommand.SET_OFF, LightSwitchState.AUTO_PENDING_OFF)
            .Permit(LightSwitchCommand.TURN_OFF, LightSwitchState.AUTO_PENDING_OFF)
            .Permit(LightSwitchCommand.DISABLE_AUTO, LightSwitchState.PENDING_OFF)
            .Ignore(LightSwitchCommand.ENABLE_AUTO);

        stateMachine.Configure(LightSwitchState.AUTO_FORCED_PENDING_ON)
            .SubstateOf(LightSwitchState.AUTO_PENDING_ON)
            .Permit(LightSwitchCommand.SET_ON, LightSwitchState.AUTO_FORCED_ON);

        stateMachine.Configure(LightSwitchState.AUTO_FORCED_ON)
            .SubstateOf(LightSwitchState.ON)
            .Permit(LightSwitchCommand.SET_OFF, LightSwitchState.AUTO_OFF)
            .Permit(LightSwitchCommand.TURN_OFF, LightSwitchState.AUTO_PENDING_OFF)
            .Permit(LightSwitchCommand.DISABLE_AUTO, LightSwitchState.ON)
            .Ignore(LightSwitchCommand.ENABLE_AUTO);

        stateMachine.Configure(LightSwitchState.AUTO_DELAYED_OFF)
            .SubstateOf(LightSwitchState.AUTO_ON)
            .OnEntryAsync(SchedulePendingOffJob)
            .OnExitAsync(CancelPendingOffJob)
            .Permit(LightSwitchCommand.COMPLETE_DELAY, LightSwitchState.AUTO_PENDING_OFF)
            .Permit(LightSwitchCommand.AUTO_ON, LightSwitchState.AUTO_PENDING_ON)
            .Ignore(LightSwitchCommand.AUTO_OFF);

        return this;
    }

    public async Task Publish()
    {
        //await stateMachine.ActivateAsync();
    }

    public async Task SetToOn(bool isOn)
    {
        var command = isOn ? LightSwitchCommand.TURN_ON : LightSwitchCommand.TURN_OFF;
        await stateMachine.FireAsync(command);
    }

    public virtual async Task SetCurrentValue(bool isOn)
    {
        var command = isOn ? LightSwitchCommand.SET_ON : LightSwitchCommand.SET_OFF;
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
        await stateMachine.FireAsync(LightSwitchCommand.COMPLETE_DELAY);
    }

    protected virtual async Task SendSetToOnEvents()
    {
        await zwaveEventSender.BinarySwitchOn(name);
    }

    protected virtual async Task SendSetToOffEvents()
    {
        await zwaveEventSender.BinarySwitchOff(name);
    }

    protected virtual async Task SendCurrentlyOnEvents()
    {
        await homebridgeEventSender.LightSwitchOn(name);
    }

    protected virtual async Task SendCurrentlyOffEvents()
    {
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
    PENDING_ON,
    PENDING_OFF,
    AUTO_FORCED_ON,
    AUTO_ON,
    AUTO_OFF,
    AUTO_PENDING_ON,
    AUTO_PENDING_OFF,
    AUTO_FORCED_PENDING_ON,
    AUTO_DELAYED_OFF
}

public enum LightSwitchCommand
{
    TURN_ON,
    TURN_OFF,
    AUTO_ON,
    AUTO_OFF,
    SET_ON,
    SET_OFF,
    COMPLETE_DELAY,
    ENABLE_AUTO,
    DISABLE_AUTO
}
