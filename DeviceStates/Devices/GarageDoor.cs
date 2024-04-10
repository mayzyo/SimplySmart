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
    void Publish();
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

    public GarageDoor(IStateStorageService stateStorage, string name, IHomebridgeEventSender homebridgeEventSender, IZwaveEventSender zwaveEventSender)
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

    public void Publish()
    {
        stateMachine.Activate();
    }

    public IGarageDoor Connect()
    {
        stateMachine.Configure(GarageDoorState.CLOSED)
            .OnEntryAsync(SetToClose)
            .Permit(GarageDoorCommand.OPEN, GarageDoorState.OPENING)
            .Ignore(GarageDoorCommand.CLOSE);

        stateMachine.Configure(GarageDoorState.OPENED)
            .OnEntryAsync(SetToOpen)
            .Permit(GarageDoorCommand.CLOSE, GarageDoorState.CLOSING)
            .Ignore(GarageDoorCommand.OPEN);

        stateMachine.Configure(GarageDoorState.CLOSING)
            .OnEntryAsync(async () =>
            {
                await SetToClosing();
                ConfigureTimer(true, triggerDelayTimer);
            })
            .OnExit(() => ConfigureTimer(false, triggerDelayTimer))
            .Permit(GarageDoorCommand.COMPLETE_CLOSE, GarageDoorState.CLOSED)
            .Ignore(GarageDoorCommand.CLOSE);

        stateMachine.Configure(GarageDoorState.OPENING)
            .OnEntryAsync(async () =>
            {
                await SetToOpening();
                ConfigureTimer(true, triggerDelayTimer);
            })
            .OnExit(() => ConfigureTimer(false, triggerDelayTimer))
            .Permit(GarageDoorCommand.COMPLETE_OPEN, GarageDoorState.OPENED)
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
        stateMachine.FireAsync(GarageDoorCommand.COMPLETE_CLOSE);
    }

    public void OpenVerified()
    {
        stateMachine.FireAsync(GarageDoorCommand.COMPLETE_OPEN);
    }

    public async Task Trigger(GarageDoorCommand command)
    {
        await stateMachine.FireAsync(command);
    }

    public async Task Toggle()
    {
        if (State == GarageDoorState.OPENED)
        {
            await Trigger(GarageDoorCommand.CLOSE);
        }
        else if (State == GarageDoorState.CLOSED)
        {
            await Trigger(GarageDoorCommand.OPEN);
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
        await zwaveEventSender.BinarySwitchOff(name);
    }

    async Task SetToOpen()
    {
        await homebridgeEventSender.GarageDoorOpenerStopped(name);
        await zwaveEventSender.BinarySwitchOff(name);
    }

    async Task SetToClose()
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
    OPENED,
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