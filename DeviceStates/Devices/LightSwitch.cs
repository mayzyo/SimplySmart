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
    Task CompleteScheduledOff();
}

public class LightSwitch(
    IStateStore stateStore,
    ISchedulerFactory schedulerFactory,
    IHomebridgeEventSender homebridgeEventSender,
    IZwaveEventSender zwaveEventSender,
    string name
) : ILightSwitch
{
    const int STAY_ON_INCREMENT = 30000;
    const int MAX_BACK_OFF = 4;

    public LightSwitchState State { get { return stateMachine.State; } }
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
        stateMachine.Configure(LightSwitchState.ON)
            .OnEntryAsync(SendCurrentlyOnEvents)
            .Ignore(LightSwitchCommand.SET_ON) // Already ON, ignored
            .Permit(LightSwitchCommand.SET_OFF, LightSwitchState.OFF) // Request from Device set to OFF directly
            .Ignore(LightSwitchCommand.TURN_ON) // Already ON, ignored
            .Permit(LightSwitchCommand.TURN_OFF, LightSwitchState.PENDING_OFF) // Wait for OFF response from device
            .Permit(LightSwitchCommand.ENABLE_AUTO, LightSwitchState.ON_SCHEDULED_OFF) // Switch to scheduled off, retain ON state
            .Ignore(LightSwitchCommand.DISABLE_AUTO) // Already manual, ignored
            .Ignore(LightSwitchCommand.SET_STAY_ON) // Not in auto, ignored
            .Ignore(LightSwitchCommand.SET_SCHEDULED_OFF) // Not in auto, ignored
            .Ignore(LightSwitchCommand.COMPLETE_SCHEDULE); // Not scheduled, ignored

        stateMachine.Configure(LightSwitchState.OFF)
            .OnEntryAsync(SendCurrentlyOffEvents)
            .OnEntry(() => StayOnCount = 0)
            .Permit(LightSwitchCommand.SET_ON, LightSwitchState.ON) // Request from device set to ON directly
            .Ignore(LightSwitchCommand.SET_OFF) // Already OFF, ignored
            .Permit(LightSwitchCommand.TURN_ON, LightSwitchState.PENDING_ON) // Wait for ON response from device
            .Ignore(LightSwitchCommand.TURN_OFF) // Already OFF, ignored
            .Permit(LightSwitchCommand.ENABLE_AUTO, LightSwitchState.OFF_AUTO) // Switch to auto, retain OFF state
            .Ignore(LightSwitchCommand.DISABLE_AUTO) // Already manual, ignored
            .Ignore(LightSwitchCommand.SET_STAY_ON) // Not in auto, ignored
            .Ignore(LightSwitchCommand.SET_SCHEDULED_OFF) // Not in auto, ignored
            .Ignore(LightSwitchCommand.COMPLETE_SCHEDULE); // Not scheduled, ignored

        stateMachine.Configure(LightSwitchState.PENDING_ON)
            .OnEntryAsync(SendSetToOnEvents)
            .Permit(LightSwitchCommand.SET_ON, LightSwitchState.ON) // Request from device set to ON directly
            .Permit(LightSwitchCommand.SET_OFF, LightSwitchState.OFF) // Request from device set to OFF directly
            .Ignore(LightSwitchCommand.TURN_ON) // Already pending, ignored
            .Permit(LightSwitchCommand.TURN_OFF, LightSwitchState.PENDING_OFF) // Change to wait for OFF response from device
            .Permit(LightSwitchCommand.ENABLE_AUTO, LightSwitchState.PENDING_ON_AUTO) // Switch to auto, retain PENDING state
            .Ignore(LightSwitchCommand.DISABLE_AUTO) // Already manual, ignored
            .Ignore(LightSwitchCommand.SET_STAY_ON) // Not in auto, ignored
            .Ignore(LightSwitchCommand.SET_SCHEDULED_OFF) // Not in auto, ignored
            .Ignore(LightSwitchCommand.COMPLETE_SCHEDULE); // Not scheduled, ignored

        stateMachine.Configure(LightSwitchState.PENDING_OFF)
            .OnEntryAsync(SendSetToOffEvents)
            .Permit(LightSwitchCommand.SET_ON, LightSwitchState.ON) // Request from device set to ON directly
            .Permit(LightSwitchCommand.SET_OFF, LightSwitchState.OFF) // Request from device set to OFF directly
            .Permit(LightSwitchCommand.TURN_ON, LightSwitchState.PENDING_ON) // Change to wait for ON response from device
            .Ignore(LightSwitchCommand.TURN_OFF) // Already pending, ignored
            .Permit(LightSwitchCommand.ENABLE_AUTO, LightSwitchState.PENDING_OFF_AUTO) // Switch to auto, retain PENDING state
            .Ignore(LightSwitchCommand.DISABLE_AUTO) // Already manual, ignored
            .Ignore(LightSwitchCommand.SET_STAY_ON) // Not in auto, ignored
            .Ignore(LightSwitchCommand.SET_SCHEDULED_OFF) // Not in auto, ignored
            .Ignore(LightSwitchCommand.COMPLETE_SCHEDULE); // Not scheduled, ignored

        stateMachine.Configure(LightSwitchState.ON_AUTO) // Stays on, no schedule
            .SubstateOf(LightSwitchState.ON) // Retains all the rules and side effects of ON
            .OnEntry(() =>
            {
                var count = StayOnCount;
                if (count < MAX_BACK_OFF)
                {
                    StayOnCount = count + 1;
                }
            })
            .Permit(LightSwitchCommand.SET_OFF, LightSwitchState.OFF_AUTO) // Request from device set to OFF directly, override parent with auto
            .Permit(LightSwitchCommand.TURN_OFF, LightSwitchState.PENDING_OFF_AUTO) // Wait for OFF response from device, override parent with auto
            .Ignore(LightSwitchCommand.ENABLE_AUTO) // Already auto, override parent with ignored
            .Permit(LightSwitchCommand.DISABLE_AUTO, LightSwitchState.ON) // retain ON state, override parent with switch to manual
            .Permit(LightSwitchCommand.SET_SCHEDULED_OFF, LightSwitchState.ON_SCHEDULED_OFF); // Already ON so state change only, override parent with change to ON with scheduled off

        stateMachine.Configure(LightSwitchState.OFF_AUTO)
            .SubstateOf(LightSwitchState.OFF) // Retains all the rules and side effects of OFF
            .Permit(LightSwitchCommand.SET_ON, LightSwitchState.ON_SCHEDULED_OFF) // Request from device set to ON directly, override parent with scheduled off
            .Permit(LightSwitchCommand.TURN_ON, LightSwitchState.PENDING_ON_SCHEDULED_OFF) // Wait for ON response from device, override parent with scheduled off
            .Ignore(LightSwitchCommand.ENABLE_AUTO) // Already auto, override parent with ignored
            .Permit(LightSwitchCommand.DISABLE_AUTO, LightSwitchState.OFF) // retain OFF state, override parent with switch to manual
            .Permit(LightSwitchCommand.SET_STAY_ON, LightSwitchState.PENDING_ON_AUTO); // Wait for ON response from device, override parent with auto

        stateMachine.Configure(LightSwitchState.PENDING_ON_AUTO) // Stays on, no schedule
            .SubstateOf(LightSwitchState.PENDING_ON) // Retains all the rules and side effects of PENDING ON
            .Permit(LightSwitchCommand.SET_ON, LightSwitchState.ON_AUTO) // Request from device set to ON directly, override parent with scheduled off
            .Permit(LightSwitchCommand.SET_OFF, LightSwitchState.OFF_AUTO) // Request from device set to OFF directly, override parent with auto
            .Permit(LightSwitchCommand.TURN_OFF, LightSwitchState.PENDING_OFF_AUTO) // Change to wait for OFF response from device, override parent with auto
            .Ignore(LightSwitchCommand.ENABLE_AUTO) // Already auto, override parent with ignored
            .Permit(LightSwitchCommand.DISABLE_AUTO, LightSwitchState.PENDING_ON); // Switch to manual, retain PENDING state, override parent

        stateMachine.Configure(LightSwitchState.PENDING_OFF_AUTO)
            .SubstateOf(LightSwitchState.PENDING_OFF) // Retains all the rules and side effects of PENDING OFF
            .Permit(LightSwitchCommand.SET_ON, LightSwitchState.ON_SCHEDULED_OFF) // Request from device set to ON directly, override parent with scheduled off
            .Permit(LightSwitchCommand.SET_OFF, LightSwitchState.OFF_AUTO) // Request from device set to OFF directly, override parent with auto
            .Permit(LightSwitchCommand.TURN_ON, LightSwitchState.PENDING_ON_SCHEDULED_OFF) // Change to wait for ON response from device, override parent with scheduled off
            .Ignore(LightSwitchCommand.ENABLE_AUTO) // Already auto, override parent with ignored
            .Permit(LightSwitchCommand.DISABLE_AUTO, LightSwitchState.PENDING_OFF) // Switch to manual, retain PENDING state, override parent
            .Permit(LightSwitchCommand.SET_STAY_ON, LightSwitchState.PENDING_ON_AUTO); // Wait for ON response from device, override parent with auto

        stateMachine.Configure(LightSwitchState.PENDING_ON_SCHEDULED_OFF)
            .SubstateOf(LightSwitchState.PENDING_ON_AUTO)
            .Permit(LightSwitchCommand.SET_ON, LightSwitchState.ON_SCHEDULED_OFF) // Request from device set to ON directly, override parent with scheduled off
            .Permit(LightSwitchCommand.SET_STAY_ON, LightSwitchState.PENDING_ON_AUTO); // Wait for ON response from device, override parent with auto

        stateMachine.Configure(LightSwitchState.ON_SCHEDULED_OFF) // ON, scheduled to OFF
            .SubstateOf(LightSwitchState.ON_AUTO) // Retains all the rules and side effects of AUTO_ON and ON
            .OnEntryAsync(SchedulePendingOffJob)
            .OnExitAsync(CancelPendingOffJob)
            .Permit(LightSwitchCommand.SET_STAY_ON, LightSwitchState.ON_AUTO) // Already ON so state change only, override parent with change to ON with auto
            .Ignore(LightSwitchCommand.SET_SCHEDULED_OFF) // Already scheduled, override parent with ignored
            .Permit(LightSwitchCommand.COMPLETE_SCHEDULE, LightSwitchState.PENDING_OFF_AUTO); // Timer ran out, override parent with turn off

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
        await stateMachine.FireAsync(LightSwitchCommand.SET_STAY_ON);
    }

    public virtual async Task AutoSetToOff()
    {
        await stateMachine.FireAsync(LightSwitchCommand.SET_SCHEDULED_OFF);
    }

    public async Task EnableAuto()
    {
        await stateMachine.FireAsync(LightSwitchCommand.ENABLE_AUTO);
    }

    public async Task DisableAuto()
    {
        await stateMachine.FireAsync(LightSwitchCommand.DISABLE_AUTO);
    }

    public async Task CompleteScheduledOff()
    {
        await stateMachine.FireAsync(LightSwitchCommand.COMPLETE_SCHEDULE);
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
            // .StartAt(DateTimeOffset.Now.AddMilliseconds(10000))
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

/// <summary>
/// A light switch has an on and off state which can only be achieved by passing through
/// a intermediary "pending on" and "pending off" state. Because communication with device is
/// not immediate. Unless the state change is from the zwave device itself (which will be immediate
/// and "set to on or off"), otherwise it should be in pending until confirmed by the device.
/// </summary>
public enum LightSwitchState
{
    ON,
    OFF,
    PENDING_ON,
    PENDING_OFF,
    ON_AUTO,
    OFF_AUTO,
    PENDING_ON_AUTO,
    PENDING_OFF_AUTO,
    PENDING_ON_SCHEDULED_OFF,
    ON_SCHEDULED_OFF // ON, scheduled to OFF
}

public enum LightSwitchCommand
{
    SET_ON,
    SET_OFF,
    TURN_ON,
    TURN_OFF,
    ENABLE_AUTO,
    DISABLE_AUTO,
    // Only applicable to detection based ON,
    // needed to avoid being set to ON_SCHEDULE_OFF,
    // which only occurs for all other methods.
    SET_STAY_ON,
    SET_SCHEDULED_OFF,
    COMPLETE_SCHEDULE
}
