using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimplySmart.States;

public interface IAreaOccupant
{
    AreaOccupantState State { get; }

    void Trigger(AreaOccupantCommand command);
}

public class AreaOccupant : IAreaOccupant
{
    public AreaOccupantState State { get { return stateMachine.State; } }
    public readonly StateMachine<AreaOccupantState, AreaOccupantCommand> stateMachine = new(AreaOccupantState.EMPTY);

    public void Initialise()
    {
        stateMachine.Configure(AreaOccupantState.EMPTY)
            .Permit(AreaOccupantCommand.SET_MOVING, AreaOccupantState.MOVING);

        stateMachine.Configure(AreaOccupantState.MOVING)
            .Permit(AreaOccupantCommand.SET_EMPTY, AreaOccupantState.EMPTY)
            .Permit(AreaOccupantCommand.SET_STATIONARY, AreaOccupantState.STATIONARY);

        stateMachine.Configure(AreaOccupantState.STATIONARY)
            .Permit(AreaOccupantCommand.SET_MOVING, AreaOccupantState.MOVING);
    }

    public void Initialise(ILightSwitch? lightSwitch)
    {
        Initialise();

        if(lightSwitch != default)
        {
            stateMachine.Configure(AreaOccupantState.EMPTY)
                .OnEntry(() => lightSwitch.Trigger(LightSwitchCommand.AUTO_OFF));

            stateMachine.Configure(AreaOccupantState.MOVING)
                .OnEntry(() => lightSwitch.Trigger(LightSwitchCommand.AUTO_ON));

            stateMachine.Configure(AreaOccupantState.STATIONARY)
                .OnEntry(() => lightSwitch.Trigger(LightSwitchCommand.AUTO_ON));
        }
    }

    public void Trigger(AreaOccupantCommand command)
    {
        stateMachine.Fire(command);
    }
}

public enum AreaOccupantState
{
    EMPTY,
    MOVING,
    STATIONARY
}

public enum AreaOccupantCommand
{
    SET_EMPTY,
    SET_MOVING,
    SET_STATIONARY
}