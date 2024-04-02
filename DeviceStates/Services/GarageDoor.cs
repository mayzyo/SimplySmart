using SimplySmart.Core.Services;
using SimplySmart.Homebridge.Services;
using SimplySmart.Zwave.Services;
using Stateless;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace SimplySmart.DeviceStates.Services;

public interface IGarageDoor : IAccessPoint
{
    GarageDoorState State { get; }
    Task Trigger(GarageDoorCommand command);
    Task Toggle();
}

internal class GarageDoor : IGarageDoor
{
    public GarageDoorState State { get { return stateMachine.State; } }
    public readonly StateMachine<GarageDoorState, GarageDoorCommand> stateMachine;
    private readonly Timer triggerDelayTimer;
    private readonly string name;

    readonly IHomebridgeEventSender homebridgeEventSender;
    readonly IZwaveEventSender zwaveEventSender;

    public GarageDoor(IStateStorageService stateStorage, string name, IHomebridgeEventSender homebridgeEventSender, IZwaveEventSender zwaveEventSender)
    {
        this.name = name;
        this.homebridgeEventSender = homebridgeEventSender;
        this.zwaveEventSender = zwaveEventSender;

        stateMachine = new(
            () =>
            {
                var state = stateStorage.GetState(name);
                if (Enum.TryParse(state, out GarageDoorState myStatus))
                {
                    return myStatus;
                }

                return GarageDoorState.CLOSED;
            },
            s => stateStorage.UpdateState(name, s.ToString())
        );

        stateMachine.Configure(GarageDoorState.CLOSED)
            .OnEntryAsync(SetToClose)
            .Permit(GarageDoorCommand.OPEN, GarageDoorState.OPENING);

        stateMachine.Configure(GarageDoorState.OPEN)
            .OnEntryAsync(SetToOpen)
            .Permit(GarageDoorCommand.CLOSE, GarageDoorState.CLOSING);

        stateMachine.Configure(GarageDoorState.CLOSING)
            .OnEntryAsync(async () =>
            {
                await SetToClosing();
                ConfigureTimer(true, triggerDelayTimer);
            })
            .OnExit(() => ConfigureTimer(false, triggerDelayTimer))
            .Permit(GarageDoorCommand.COMPLETE_CLOSE, GarageDoorState.CLOSED);

        stateMachine.Configure(GarageDoorState.OPENING)
            .OnEntryAsync(async () =>
            {
                await SetToOpening();
                ConfigureTimer(true, triggerDelayTimer);
            })
            .OnExit(() => ConfigureTimer(false, triggerDelayTimer))
            .Permit(GarageDoorCommand.COMPLETE_OPEN, GarageDoorState.OPEN);

        triggerDelayTimer = new Timer(20000) { AutoReset = false, Enabled = false };
        triggerDelayTimer.Elapsed += DelayTimerElapsed;
    }

    public async Task Trigger(GarageDoorCommand command)
    {
        if (stateMachine.IsInState(GarageDoorState.OPENING))
        {
            await homebridgeEventSender.GarageDoorOpenerOn(name);
        }
        else if (stateMachine.IsInState(GarageDoorState.CLOSING))
        {
            await homebridgeEventSender.GarageDoorOpenerOff(name);
        }
        else
        {
            await stateMachine.FireAsync(command);
        }
    }

    public async Task Toggle()
    {
        if (State == GarageDoorState.OPEN)
        {
            await Trigger(GarageDoorCommand.CLOSE);
        }
        else if (State == GarageDoorState.CLOSED)
        {
            await Trigger(GarageDoorCommand.OPEN);
        }
    }

    private async Task SetToOpening()
    {
        await homebridgeEventSender.GarageDoorOpenerOn(name);
        await homebridgeEventSender.GarageDoorOpenerMoving(name);
        // Smart implant only triggers the device when switching from off to on. We need to switch it back after so it can be used again.
        await zwaveEventSender.BinarySwitchOn(name);
    }

    private async Task SetToClosing()
    {
        await homebridgeEventSender.GarageDoorOpenerOff(name);
        await homebridgeEventSender.GarageDoorOpenerMoving(name);
        // Smart implant only triggers the device when switching from off to on. We need to switch it back after so it can be used again.
        await zwaveEventSender.BinarySwitchOff(name);
    }

    private async Task SetToOpen()
    {
        await homebridgeEventSender.GarageDoorOpenerStopped(name);
        await zwaveEventSender.BinarySwitchOff(name);
    }

    private async Task SetToClose()
    {
        await homebridgeEventSender.GarageDoorOpenerStopped(name);
        await zwaveEventSender.BinarySwitchOff(name);
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
        if (stateMachine.IsInState(GarageDoorState.CLOSING))
        {
            stateMachine.FireAsync(GarageDoorCommand.COMPLETE_CLOSE);
        }
        else if (stateMachine.IsInState(GarageDoorState.OPENING))
        {
            stateMachine.FireAsync(GarageDoorCommand.COMPLETE_OPEN);
        }
    }
}

public enum GarageDoorState
{
    OPEN,
    CLOSED,
    OPENING,
    CLOSING
}

public enum GarageDoorCommand
{
    OPEN,
    CLOSE,
    COMPLETE_CLOSE,
    COMPLETE_OPEN
}