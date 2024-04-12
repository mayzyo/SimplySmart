using SimplySmart.Core.Abstractions;
using SimplySmart.Homebridge.Services;
using SimplySmart.Zwave.Abstractions;
using SimplySmart.Zwave.Services;
using Stateless;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace SimplySmart.DeviceStates.Devices;

public interface IGarageDoor : IBinarySwitch
{
    IGarageDoor Connect();
    GarageDoorState State { get; }
    Task Publish();
    Task Toggle();
    void CloseVerified();
    void OpenVerified();
}

internal class GarageDoor : IGarageDoor
{
    const int DELAY_DEFAULT = 20000;

    public GarageDoorState State { get { return stateMachine.State; } }
    public readonly StateMachine<GarageDoorState, GarageDoorCommand> stateMachine;
    readonly Timer triggerDelayTimer;
    readonly string name;

    readonly IHomebridgeEventSender homebridgeEventSender;
    readonly IZwaveEventSender zwaveEventSender;

    public GarageDoor(IStateStore stateStorage, string name, IHomebridgeEventSender homebridgeEventSender, IZwaveEventSender zwaveEventSender)
    {
        this.name = name;
        this.homebridgeEventSender = homebridgeEventSender;
        this.zwaveEventSender = zwaveEventSender;

        stateMachine = new(
            () =>
            {
                var stateString = stateStorage.GetState(name);
                if (Enum.TryParse(stateString, out GarageDoorState state))
                {
                    return state;
                }

                return GarageDoorState.CLOSED;
            },
            s => stateStorage.UpdateState(name, s.ToString())
        );

        triggerDelayTimer = new Timer(DELAY_DEFAULT) { AutoReset = false, Enabled = false };
        triggerDelayTimer.Elapsed += DelayTimerElapsed;
    }

    public async Task Publish()
    {
        await stateMachine.ActivateAsync();

        if(stateMachine.IsInState(GarageDoorState.OPENED))
        {
            await homebridgeEventSender.GarageDoorOpenerOn(name);
        }
        else if(stateMachine.IsInState(GarageDoorState.CLOSED))
        {
            await homebridgeEventSender.GarageDoorOpenerOff(name);
        }
    }

    public IGarageDoor Connect()
    {
        stateMachine.Configure(GarageDoorState.CLOSED)
            .OnEntryAsync(CompleteGarageDoorActivity)
            .OnActivateAsync(CompleteGarageDoorActivity)
            .Permit(GarageDoorCommand.OPEN, GarageDoorState.OPENING)
            .Ignore(GarageDoorCommand.CLOSE);

        stateMachine.Configure(GarageDoorState.OPENED)
            .OnEntryAsync(CompleteGarageDoorActivity)
            .OnActivateAsync(CompleteGarageDoorActivity)
            .Permit(GarageDoorCommand.CLOSE, GarageDoorState.CLOSING)
            .Ignore(GarageDoorCommand.OPEN);

        stateMachine.Configure(GarageDoorState.CLOSING)
            .OnEntryAsync(SetToClosing)
            .OnEntry(() => ConfigureTimer(true, triggerDelayTimer))
            .OnActivate(() => ConfigureTimer(true, triggerDelayTimer))
            .OnExit(() => ConfigureTimer(false, triggerDelayTimer))
            .Permit(GarageDoorCommand.CLOSE_COMPLETE, GarageDoorState.CLOSED)
            .Ignore(GarageDoorCommand.CLOSE);

        stateMachine.Configure(GarageDoorState.OPENING)
            .OnEntryAsync(SetToOpening)
            .OnEntry(() => ConfigureTimer(true, triggerDelayTimer))
            .OnActivate(() => ConfigureTimer(true, triggerDelayTimer))
            .OnExit(() => ConfigureTimer(false, triggerDelayTimer))
            .Permit(GarageDoorCommand.OPEN_COMPLETE, GarageDoorState.OPENED)
            .Ignore(GarageDoorCommand.OPEN);

        return this;
    }

    public async Task SetToOn(bool isOn)
    {
        var command = isOn ? GarageDoorCommand.OPEN : GarageDoorCommand.CLOSE;
        await stateMachine.FireAsync(command);
    }

    public void CloseVerified()
    {
        stateMachine.FireAsync(GarageDoorCommand.CLOSE_COMPLETE);
    }

    public void OpenVerified()
    {
        stateMachine.FireAsync(GarageDoorCommand.OPEN_COMPLETE);
    }

    public async Task Toggle()
    {
        if (State == GarageDoorState.OPENED)
        {
            await stateMachine.FireAsync(GarageDoorCommand.CLOSE);
        }
        else if (State == GarageDoorState.CLOSED)
        {
            await stateMachine.FireAsync(GarageDoorCommand.OPEN);
        }
    }

    async Task SetToOpening()
    {
        await homebridgeEventSender.GarageDoorOpenerOn(name);
        await homebridgeEventSender.GarageDoorOpenerMoving(name);
        // Smart implant only triggers the device when switching from off to on. We need to switch it back after so it can be used again.
        await zwaveEventSender.BinarySwitchOn(name);
    }

    async Task SetToClosing()
    {
        await homebridgeEventSender.GarageDoorOpenerOff(name);
        await homebridgeEventSender.GarageDoorOpenerMoving(name);
        // Smart implant only triggers the device when switching from off to on. We need to switch it back after so it can be used again.
        await zwaveEventSender.BinarySwitchOn(name);
    }

    async Task CompleteGarageDoorActivity()
    {
        await homebridgeEventSender.GarageDoorOpenerStopped(name);
        await zwaveEventSender.BinarySwitchOff(name);
    }

    static void ConfigureTimer(bool active, Timer timer)
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

    void DelayTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (stateMachine.IsInState(GarageDoorState.CLOSING))
        {
            stateMachine.FireAsync(GarageDoorCommand.CLOSE_COMPLETE);
        }
        else if (stateMachine.IsInState(GarageDoorState.OPENING))
        {
            stateMachine.FireAsync(GarageDoorCommand.OPEN_COMPLETE);
        }
    }
}

public enum GarageDoorState
{
    OPENED,
    CLOSED,
    OPENING,
    CLOSING
}

public enum GarageDoorCommand
{
    OPEN,
    CLOSE,
    CLOSE_COMPLETE,
    OPEN_COMPLETE
}