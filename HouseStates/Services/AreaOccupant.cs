using SimplySmart.Core.Models;
using SimplySmart.DeviceStates.Services;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimplySmart.HouseStates.Services;

public interface IAreaOccupant
{
    AreaOccupantState State { get; }

    void Trigger(AreaOccupantCommand command);
}

public class AreaOccupant : IAreaOccupant
{
    public AreaOccupantState State { get { return stateMachine.State; } }
    public readonly StateMachine<AreaOccupantState, AreaOccupantCommand> stateMachine = new(AreaOccupantState.EMPTY);

    public AreaOccupant(ILightSwitchService lightSwitchService, string? lightSwitch)
    {
        stateMachine.Configure(AreaOccupantState.EMPTY)
            .Permit(AreaOccupantCommand.SET_MOVING, AreaOccupantState.MOVING);

        stateMachine.Configure(AreaOccupantState.MOVING)
            .Permit(AreaOccupantCommand.SET_EMPTY, AreaOccupantState.EMPTY)
            .Permit(AreaOccupantCommand.SET_STATIONARY, AreaOccupantState.STATIONARY);

        stateMachine.Configure(AreaOccupantState.STATIONARY)
            .Permit(AreaOccupantCommand.SET_MOVING, AreaOccupantState.MOVING);

        if (lightSwitch != default)
        {
            stateMachine.Configure(AreaOccupantState.EMPTY)
                .OnEntry(() => lightSwitchService[lightSwitch].Trigger(LightSwitchCommand.AUTO_OFF, BroadcastSource.EXTERNAL));

            stateMachine.Configure(AreaOccupantState.MOVING)
                .OnEntry(() => lightSwitchService[lightSwitch].Trigger(LightSwitchCommand.AUTO_ON, BroadcastSource.EXTERNAL));

            stateMachine.Configure(AreaOccupantState.STATIONARY)
                .OnEntry(() => lightSwitchService[lightSwitch].Trigger(LightSwitchCommand.AUTO_ON, BroadcastSource.EXTERNAL));
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