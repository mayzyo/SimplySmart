using SimplySmart.Core.Models;
using SimplySmart.Core.Services;
using SimplySmart.Homebridge.Services;
using SimplySmart.Zwave.Services;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace SimplySmart.DeviceStates.Services;

public interface ILightSwitch
{
    LightSwitchState State { get; }

    void Trigger(LightSwitchCommand command, BroadcastSource source);

    bool IsInState(LightSwitchState state);
}

public class LightSwitch : ILightSwitch
{
    private const int STAY_ON_DEFAULT = 30000;

    public LightSwitchState State { get { return stateMachine.State; } }
    public readonly StateMachine<LightSwitchState, LightSwitchCommand> stateMachine;
    public readonly string name;

    protected BroadcastSource source;
    protected readonly IHomebridgeEventSender homebridgeEventSender;
    protected readonly IZwaveEventSender zwaveEventSender;
    
    readonly Timer? triggerDelayTimer;

    public BroadcastSource Source { get { return source; } }

    public LightSwitch(IStateStorageService stateStorage, IHomebridgeEventSender homebridgeEventSender, IZwaveEventSender zwaveEventSender, string name, int? stayOn)
    {
        this.homebridgeEventSender = homebridgeEventSender;
        this.zwaveEventSender = zwaveEventSender;
        this.name = name;

        triggerDelayTimer = new Timer(stayOn ?? STAY_ON_DEFAULT) { AutoReset = false, Enabled = false };
        triggerDelayTimer.Elapsed += DelayTimerElapsed;

        stateMachine = new(
            () =>
            {
                var state = stateStorage.GetState(name);
                if (Enum.TryParse(state, out LightSwitchState myStatus))
                {
                    return myStatus;
                }

                return LightSwitchState.MANUAL_OFF;
            },
            s => stateStorage.UpdateState(name, s.ToString())
        );

        ConfigureOnState();

        stateMachine.Configure(LightSwitchState.OFF)
            .OnEntryAsync(SetToOff);

        stateMachine.Configure(LightSwitchState.MANUAL_OFF)
            .SubstateOf(LightSwitchState.OFF)
            .Permit(LightSwitchCommand.MANUAL_ON, LightSwitchState.MANUAL_ON)
            .Permit(LightSwitchCommand.ENABLE_AUTO, LightSwitchState.AUTO_OFF);

        stateMachine.Configure(LightSwitchState.MANUAL_ON)
            .SubstateOf(LightSwitchState.ON)
            .Permit(LightSwitchCommand.MANUAL_OFF, LightSwitchState.MANUAL_OFF)
            .Permit(LightSwitchCommand.ENABLE_AUTO, LightSwitchState.MANUAL_AUTO_ON);

        stateMachine.Configure(LightSwitchState.AUTO_OFF)
            .SubstateOf(LightSwitchState.OFF)
            .Permit(LightSwitchCommand.AUTO_ON, LightSwitchState.AUTO_ON)
            .Permit(LightSwitchCommand.MANUAL_ON, LightSwitchState.MANUAL_AUTO_ON)
            .Permit(LightSwitchCommand.DISABLE_AUTO, LightSwitchState.MANUAL_OFF);

        stateMachine.Configure(LightSwitchState.AUTO_ON)
            .SubstateOf(LightSwitchState.ON)
            .Permit(LightSwitchCommand.AUTO_OFF, LightSwitchState.TIMED_AUTO_ON)
            .Permit(LightSwitchCommand.MANUAL_OFF, LightSwitchState.AUTO_OFF)
            .Permit(LightSwitchCommand.DISABLE_AUTO, LightSwitchState.MANUAL_OFF);

        stateMachine.Configure(LightSwitchState.TIMED_AUTO_ON)
            .SubstateOf(LightSwitchState.AUTO_ON)
            .OnEntry(() => ConfigureTimer(true, triggerDelayTimer))
            .OnExit(() => ConfigureTimer(false, triggerDelayTimer))
            .Permit(LightSwitchCommand.DELAYED_OFF, LightSwitchState.AUTO_OFF);

        stateMachine.Configure(LightSwitchState.MANUAL_AUTO_ON)
            .SubstateOf(LightSwitchState.MANUAL_ON)
            .Permit(LightSwitchCommand.MANUAL_OFF, LightSwitchState.AUTO_OFF)
            .Permit(LightSwitchCommand.DISABLE_AUTO, LightSwitchState.MANUAL_ON);
    }

    public void Trigger(LightSwitchCommand command, BroadcastSource source)
    {
        this.source = source;
        stateMachine.FireAsync(command);
    }

    public bool IsInState(LightSwitchState state)
    {
        return stateMachine.IsInState(state);
    }

    protected virtual async Task SetToOn()
    {
        if (Source != BroadcastSource.ZWAVE)
        {
            await zwaveEventSender.BinarySwitchOn(name);

        }

        if (Source != BroadcastSource.HOMEBRIDGE)
        {
            await homebridgeEventSender.LightSwitchOn(name);
        }
    }

    protected virtual async Task SetToOff()
    {
        if (Source != BroadcastSource.ZWAVE)
        {
            await zwaveEventSender.BinarySwitchOff(name);
        }

        if (Source != BroadcastSource.HOMEBRIDGE)
        {
            await homebridgeEventSender.LightSwitchOff(name);
        }
    }

    protected virtual void ConfigureOnState()
    {
        stateMachine.Configure(LightSwitchState.ON)
            .OnEntryAsync(SetToOn);
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
            stateMachine.FireAsync(LightSwitchCommand.DELAYED_OFF);
        }
    }
}

public enum LightSwitchState
{
    ON,
    OFF,
    MANUAL_ON,
    MANUAL_OFF,
    MANUAL_AUTO_ON,
    MANUAL_AUTO_OFF,
    AUTO_ON,
    AUTO_OFF,
    TIMED_AUTO_ON
}

public enum LightSwitchCommand
{
    MANUAL_ON,
    MANUAL_OFF,
    AUTO_ON,
    AUTO_OFF,
    DELAYED_OFF,
    ENABLE_AUTO,
    DISABLE_AUTO
}
