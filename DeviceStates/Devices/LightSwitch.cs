using SimplySmart.Core.Abstractions;
using SimplySmart.Core.Models;
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
using System.Timers;
using Timer = System.Timers.Timer;

namespace SimplySmart.DeviceStates.Devices;

public interface ILightSwitch : IBinarySwitch, IAutoSwitch
{
    LightSwitchState State { get; }
    Task Publish();
    Task EnableAuto();
    Task DisableAuto();
}

public class LightSwitch : ILightSwitch
{
    const int STAY_ON_DEFAULT = 30000;

    public LightSwitchState State { get { return stateMachine.State; } }
    public bool PendingTrigger { get { return triggerDelayTimer.Enabled; } }
    public readonly StateMachine<LightSwitchState, LightSwitchCommand> stateMachine;
    public readonly string name;

    protected BroadcastSource source;
    protected readonly IStateStore stateStorage;
    protected readonly IHomebridgeEventSender homebridgeEventSender;
    protected readonly IZwaveEventSender zwaveEventSender;

    readonly Timer triggerDelayTimer;

    public BroadcastSource Source { get { return source; } }

    public LightSwitch(IStateStore stateStorage, IHomebridgeEventSender homebridgeEventSender, IZwaveEventSender zwaveEventSender, string name, int? stayOn)
    {
        this.stateStorage = stateStorage;
        this.homebridgeEventSender = homebridgeEventSender;
        this.zwaveEventSender = zwaveEventSender;
        this.name = name;

        stateMachine = new(
            InitialState,
            s => stateStorage.UpdateState(name, s.ToString())
        );

        triggerDelayTimer = new Timer(stayOn ?? STAY_ON_DEFAULT) { AutoReset = false, Enabled = false };
        triggerDelayTimer.Elapsed += DelayTimerElapsed;
    }

    public virtual ILightSwitch Connect()
    {
        stateMachine.Configure(LightSwitchState.OFF)
            .OnEntryAsync(SendOffEvents)
            .OnActivateAsync(SendOffEvents)
            .Permit(LightSwitchCommand.TURN_ON, LightSwitchState.ON)
            .Permit(LightSwitchCommand.ENABLE_AUTO, LightSwitchState.AUTO_OFF)
            .Ignore(LightSwitchCommand.TURN_OFF)
            .Ignore(LightSwitchCommand.AUTO_OFF)
            .Ignore(LightSwitchCommand.AUTO_ON);

        ConfigureOnToOnGuardClause();
        stateMachine.Configure(LightSwitchState.ON)
            .OnEntryAsync(SendOnEvents)
            .OnActivateAsync(SendOnEvents)
            .Permit(LightSwitchCommand.TURN_OFF, LightSwitchState.OFF)
            .Permit(LightSwitchCommand.ENABLE_AUTO, LightSwitchState.AUTO_FORCED_ON)
            .Ignore(LightSwitchCommand.AUTO_OFF)
            .Ignore(LightSwitchCommand.AUTO_ON);

        stateMachine.Configure(LightSwitchState.AUTO_OFF)
            .SubstateOf(LightSwitchState.OFF)
            .Permit(LightSwitchCommand.AUTO_ON, LightSwitchState.AUTO_ON)
            .Permit(LightSwitchCommand.TURN_ON, LightSwitchState.AUTO_FORCED_ON)
            .Permit(LightSwitchCommand.DISABLE_AUTO, LightSwitchState.OFF)
            .Ignore(LightSwitchCommand.AUTO_OFF);

        stateMachine.Configure(LightSwitchState.AUTO_ON)
            .SubstateOf(LightSwitchState.ON)
            .Permit(LightSwitchCommand.AUTO_OFF, LightSwitchState.AUTO_PENDING_OFF)
            .Permit(LightSwitchCommand.TURN_OFF, LightSwitchState.AUTO_OFF)
            .Permit(LightSwitchCommand.DISABLE_AUTO, LightSwitchState.OFF)
            .Ignore(LightSwitchCommand.AUTO_ON);

        stateMachine.Configure(LightSwitchState.AUTO_PENDING_OFF)
            .SubstateOf(LightSwitchState.AUTO_ON)
            .OnEntry(() => ConfigureTimer(true, triggerDelayTimer))
            .OnActivate(() => ConfigureTimer(true, triggerDelayTimer))
            .OnExit(() => ConfigureTimer(false, triggerDelayTimer))
            .Permit(LightSwitchCommand.AUTO_ON, LightSwitchState.AUTO_ON)
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

    protected virtual void ConfigureOnToOnGuardClause()
    {
        stateMachine.Configure(LightSwitchState.ON)
            .Ignore(LightSwitchCommand.TURN_ON);
    }

    protected virtual LightSwitchState InitialState()
    {
        var stateString = stateStorage.GetState(name);
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

    private static void ConfigureTimer(bool active, Timer timer)
    {
        if (timer != null)
        {
            if (active)
            {
                timer.Start();
            }
            else
            {
                timer.Stop();
            }
        }

    }

    private void DelayTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!stateMachine.IsInState(LightSwitchState.OFF))
        {
            stateMachine.FireAsync(LightSwitchCommand.AUTO_OFF);
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
    ENABLE_AUTO,
    DISABLE_AUTO
}
