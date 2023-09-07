using Microsoft.Extensions.Logging;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.States;

public interface IAreaOccupant
{
    AreaOccupantState State { get; }

    void Trigger(AreaOccupantCommand command);
}

public class AreaOccupant : IAreaOccupant
{
    public AreaOccupantState State { get { return stateMachine.State; } }
    public readonly StateMachine<AreaOccupantState, AreaOccupantCommand> stateMachine;

    public AreaOccupant()
    {
        stateMachine = new(AreaOccupantState.EMPTY);

        stateMachine.Configure(AreaOccupantState.EMPTY)
            .Permit(AreaOccupantCommand.SET_MOVING, AreaOccupantState.MOVING);

        stateMachine.Configure(AreaOccupantState.MOVING)
            .Permit(AreaOccupantCommand.SET_EMPTY, AreaOccupantState.EMPTY)
            .Permit(AreaOccupantCommand.SET_STATIONARY, AreaOccupantState.STATIONARY);

        stateMachine.Configure(AreaOccupantState.STATIONARY)
            .Permit(AreaOccupantCommand.SET_MOVING, AreaOccupantState.MOVING);
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