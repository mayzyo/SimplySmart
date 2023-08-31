using Microsoft.Extensions.Logging;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace SimplySmart.States;

public interface ILightSwitch
{
    LightSwitchState State { get; }

    void Trigger(LightSwitchCommand command);
}

public class LightSwitch : ILightSwitch
{
    private const int STAY_ON_DEFAULT = 30000;

    public LightSwitchState State { get { return stateMachine.State; } }
    public readonly StateMachine<LightSwitchState, LightSwitchCommand> stateMachine;
    private readonly Timer? triggerDelayTimer;

    public LightSwitch(int? stayOn)
    {
        triggerDelayTimer = new Timer(stayOn ?? STAY_ON_DEFAULT) { AutoReset = false, Enabled = false };
        triggerDelayTimer.Elapsed += DelayTimerElapsed;

        stateMachine = new(LightSwitchState.OFF);

        stateMachine.Configure(LightSwitchState.OFF)
            .Permit(LightSwitchCommand.ON, LightSwitchState.ON)
            .Permit(LightSwitchCommand.FORCE_ON, LightSwitchState.FORCED_ON)
            .Permit(LightSwitchCommand.FORCE_OFF, LightSwitchState.FORCED_OFF);

        stateMachine.Configure(LightSwitchState.PERSIST_ON)
            .OnEntry(() => ConfigureTimer(true, triggerDelayTimer))
            .OnExit(() => ConfigureTimer(false, triggerDelayTimer))
            .Permit(LightSwitchCommand.TIMED_OFF, LightSwitchState.OFF)
            .Permit(LightSwitchCommand.FORCE_ON, LightSwitchState.FORCED_ON)
            .Permit(LightSwitchCommand.FORCE_OFF, LightSwitchState.FORCED_OFF);

        stateMachine.Configure(LightSwitchState.ON)
            .Permit(LightSwitchCommand.OFF, LightSwitchState.PERSIST_ON)
            .Permit(LightSwitchCommand.FORCE_ON, LightSwitchState.FORCED_ON)
            .Permit(LightSwitchCommand.FORCE_OFF, LightSwitchState.FORCED_OFF);

        stateMachine.Configure(LightSwitchState.FORCED_OFF)
            .Permit(LightSwitchCommand.FORCE_ON, LightSwitchState.FORCED_ON)
            .Permit(LightSwitchCommand.SET_OFF, LightSwitchState.OFF);

        stateMachine.Configure(LightSwitchState.FORCED_ON)
            .Permit(LightSwitchCommand.FORCE_OFF, LightSwitchState.FORCED_OFF)
            .Permit(LightSwitchCommand.SET_OFF, LightSwitchState.OFF);
    }

    public void Trigger(LightSwitchCommand command)
    {
        stateMachine.Fire(command);
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
        if (stateMachine.State != LightSwitchState.OFF)
        {
            stateMachine.Fire(LightSwitchCommand.TIMED_OFF);
        }
    }
}

public enum LightSwitchState
{
    ON,
    OFF,
    PERSIST_ON,
    FORCED_ON,
    FORCED_OFF
}

public enum LightSwitchCommand
{
    SET_OFF,
    ON,
    OFF,
    FORCE_ON,
    FORCE_OFF,
    TIMED_OFF
}
