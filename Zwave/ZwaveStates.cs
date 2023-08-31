using Stateless;
using Stateless.Graph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace SimplySmart.Zwave;

public class ZwaveStates
{
    public StateMachine<LightSwitchState, LightSwitchCommand> laundryRoomLight = new(LightSwitchState.OFF);
    public delegate void OnTurnOffDel();
    public event OnTurnOffDel? OnTurnOff;
    private Timer? triggerDelayTimer;
    private Timer? timeoutTimer;

    public ZwaveStates()
    {
        triggerDelayTimer = new Timer(2000) { AutoReset = false, Enabled = false };
        triggerDelayTimer.Elapsed += DelayTimerElapsed;

        timeoutTimer = new Timer(10000) { AutoReset = false, Enabled = false };
        timeoutTimer.Elapsed += TimeoutTimerElapsed;

        laundryRoomLight.OnTransitioned(OnTransition);

        laundryRoomLight.Configure(LightSwitchState.OFF)
            .Permit(LightSwitchCommand.TURN_ON, LightSwitchState.ON);

        laundryRoomLight.Configure(LightSwitchState.ON)
            .OnEntry(() => ConfigureTimer(true, timeoutTimer, "Timeout"))
            .OnExit(() => ConfigureTimer(false, timeoutTimer, "Timeout"))
            .PermitReentry(LightSwitchCommand.TURN_ON)
            .Permit(LightSwitchCommand.TURN_OFF, LightSwitchState.GRACE_PERIOD);

        laundryRoomLight.Configure(LightSwitchState.GRACE_PERIOD)
            .OnEntry(() => ConfigureTimer(true, triggerDelayTimer, "Trigger delay"))
            .OnExit(() => ConfigureTimer(false, triggerDelayTimer, "Trigger delay"))
            .Permit(LightSwitchCommand.SET_OFF, LightSwitchState.OFF);

        laundryRoomLight.OnTransitioned((transition) =>
        {
            if (transition.Destination == LightSwitchState.OFF && OnTurnOff != null)
            {
                Console.WriteLine("OnTransitioned to LightSwitchState.OFF");
                OnTurnOff();
                OnTurnOff = null;
            }
        });
    }

    private void ConfigureTimer(bool active, Timer timer, string timerName)
    {
        if (timer != null)
            if (active)
            {
                timer.Start();
                Console.WriteLine($"{timerName} started.");
            }
            else
            {
                timer.Stop();
                Console.WriteLine($"{timerName} cancelled.");
            }
    }

    private void TimeoutTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        Console.WriteLine("Fired TURN OFF");
        laundryRoomLight.Fire(LightSwitchCommand.TURN_OFF);
    }

    private void DelayTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        Console.WriteLine("Fired SET OFF");
        laundryRoomLight.Fire(LightSwitchCommand.SET_OFF);
    }

    private void OnTransition(StateMachine<LightSwitchState, LightSwitchCommand>.Transition transition)
    {
        Console.WriteLine($"Transitioned from {transition.Source} to " +
            $"{transition.Destination} via {transition.Trigger}.");
    }
}

public enum LightSwitchState
{
    ON,
    OFF,
    GRACE_PERIOD,
    TIMEOUT
}

public enum LightSwitchCommand
{
    TURN_ON,
    TURN_OFF,
    SET_OFF
}