using Microsoft.Extensions.DependencyInjection;
using SimplySmart.Homebridge;
using SimplySmart.Zwave;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace SimplySmart.States;

public interface IGarageDoor : IAccessPoint
{
    GarageDoorState State { get; }

    void Initialise(IServiceProvider serviceProvider);

    Task Trigger(GarageDoorCommand command);

    Task Toggle();
}

internal class GarageDoor : IGarageDoor
{
    public GarageDoorState State { get { return stateMachine.State; } }
    public readonly StateMachine<GarageDoorState, GarageDoorCommand> stateMachine = new(GarageDoorState.CLOSED);
    private readonly Timer triggerDelayTimer;
    private readonly string name;
    private IServiceProvider? serviceProvider;

    public GarageDoor(string name)
    {
        triggerDelayTimer = new Timer(20000) { AutoReset = false, Enabled = false };
        triggerDelayTimer.Elapsed += DelayTimerElapsed;
        this.name = name;
    }

    public void Initialise(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;

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
    }

    public async Task Trigger(GarageDoorCommand command)
    {
        if (stateMachine.IsInState(GarageDoorState.OPENING))
        {
            if (serviceProvider != null)
            {
                using var scope = serviceProvider.CreateScope();

                IHomebridgeGarageDoorOpenerHandler homebridgeHandler = scope.ServiceProvider.GetRequiredService<IHomebridgeGarageDoorOpenerHandler>();
                await homebridgeHandler.HandleOn(name);
            }
        }
        else if (stateMachine.IsInState(GarageDoorState.CLOSING))
        {
            if (serviceProvider != null)
            {
                using var scope = serviceProvider.CreateScope();

                IHomebridgeGarageDoorOpenerHandler homebridgeHandler = scope.ServiceProvider.GetRequiredService<IHomebridgeGarageDoorOpenerHandler>();
                await homebridgeHandler.HandleOff(name);
            }
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
        else if(State == GarageDoorState.CLOSED)
        {
            await Trigger(GarageDoorCommand.OPEN);
        }
    }

    private async Task SetToOpening()
    {
        if (serviceProvider != null)
        {
            using var scope = serviceProvider.CreateScope();

            IHomebridgeGarageDoorOpenerHandler homebridgeHandler = scope.ServiceProvider.GetRequiredService<IHomebridgeGarageDoorOpenerHandler>();
            await homebridgeHandler.HandleOn(name);
            await homebridgeHandler.HandleMoving(name);
            IZwaveBinarySwitchHandler zwaveHandler = scope.ServiceProvider.GetRequiredService<IZwaveBinarySwitchHandler>();
            // Smart implant only triggers the device when switching from off to on. We need to switch it back after so it can be used again.
            await zwaveHandler.HandleOn(name);
        }
    }

    private async Task SetToClosing()
    {
        if (serviceProvider != null)
        {
            using var scope = serviceProvider.CreateScope();

            IHomebridgeGarageDoorOpenerHandler homebridgeHandler = scope.ServiceProvider.GetRequiredService<IHomebridgeGarageDoorOpenerHandler>();
            await homebridgeHandler.HandleOff(name);
            await homebridgeHandler.HandleMoving(name);
            IZwaveBinarySwitchHandler zwaveHandler = scope.ServiceProvider.GetRequiredService<IZwaveBinarySwitchHandler>();
            // Smart implant only triggers the device when switching from off to on. We need to switch it back after so it can be used again.
            await zwaveHandler.HandleOn(name);
        }
    }

    private async Task SetToOpen()
    {
        if (serviceProvider != null)
        {
            using var scope = serviceProvider.CreateScope();

            IHomebridgeGarageDoorOpenerHandler homebridgeHandler = scope.ServiceProvider.GetRequiredService<IHomebridgeGarageDoorOpenerHandler>();
            await homebridgeHandler.HandleStopped(name);
            IZwaveBinarySwitchHandler zwaveHandler = scope.ServiceProvider.GetRequiredService<IZwaveBinarySwitchHandler>();
            await zwaveHandler.HandleOff(name);
        }
    }

    private async Task SetToClose()
    {
        if (serviceProvider != null)
        {
            using var scope = serviceProvider.CreateScope();

            IHomebridgeGarageDoorOpenerHandler homebridgeHandler = scope.ServiceProvider.GetRequiredService<IHomebridgeGarageDoorOpenerHandler>();
            await homebridgeHandler.HandleStopped(name);
            IZwaveBinarySwitchHandler zwaveHandler = scope.ServiceProvider.GetRequiredService<IZwaveBinarySwitchHandler>();
            await zwaveHandler.HandleOff(name);
        }
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
        else if(stateMachine.IsInState(GarageDoorState.OPENING))
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