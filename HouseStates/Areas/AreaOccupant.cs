using SimplySmart.Core.Abstractions;
using SimplySmart.HouseStates.Abstractions;
using SimplySmart.Zwave.Abstractions;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimplySmart.HouseStates.Areas;

public interface IAreaOccupant
{
    AreaOccupantState State { get; }
    void SetMoving(bool isMoving);
}

public class AreaOccupant(IStateStore stateStorageService, string name) : IAreaOccupant
{
    public AreaOccupantState State { get { return stateMachine.State; } }
    public readonly StateMachine<AreaOccupantState, AreaOccupantCommand> stateMachine = new(
        () =>
        {
            var stateString = stateStorageService.GetState(name);
            if (Enum.TryParse(stateString, out AreaOccupantState state))
            {
                return state;
            }

            return AreaOccupantState.EMPTY;
        },
        s => stateStorageService.UpdateState(name, s.ToString())
    );

    public IAreaOccupant Connect()
    {
        stateMachine.Configure(AreaOccupantState.EMPTY)
            .Permit(AreaOccupantCommand.SET_MOVING, AreaOccupantState.MOVING);

        stateMachine.Configure(AreaOccupantState.MOVING)
            .Permit(AreaOccupantCommand.SET_EMPTY, AreaOccupantState.EMPTY)
            .Permit(AreaOccupantCommand.SET_STATIONARY, AreaOccupantState.STATIONARY);

        stateMachine.Configure(AreaOccupantState.STATIONARY)
            .Permit(AreaOccupantCommand.SET_MOVING, AreaOccupantState.MOVING);

        return this;
    }

    public IAreaOccupant Connect(IAutoSwitch lightSwitch)
    {
        Connect();

        stateMachine.Configure(AreaOccupantState.EMPTY)
            .OnEntry(() => lightSwitch.AutoSetToOff());

        stateMachine.Configure(AreaOccupantState.MOVING)
            .OnEntry(() => lightSwitch.AutoSetToOn());

        stateMachine.Configure(AreaOccupantState.STATIONARY)
            .OnEntry(() => lightSwitch.AutoSetToOn());

        return this;
    }

    public void SetMoving(bool isMoving)
    {
        stateMachine.Fire(isMoving ? AreaOccupantCommand.SET_MOVING : AreaOccupantCommand.SET_EMPTY);
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