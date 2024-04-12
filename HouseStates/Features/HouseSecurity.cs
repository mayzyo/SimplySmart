using SimplySmart.Core.Abstractions;
using SimplySmart.Homebridge.Services;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.HouseStates.Features;

public interface IHouseSecurity
{
    HouseSecurityState State { get; }
    IHouseSecurity Connect();
    Task Publish();
    Task Trigger(HouseSecurityCommand command);
}

internal class HouseSecurity(IStateStore stateStorageService, IHomebridgeEventSender homebridgeEventSender) : IHouseSecurity
{
    public HouseSecurityState State { get { return stateMachine.State; } }
    public readonly StateMachine<HouseSecurityState, HouseSecurityCommand> stateMachine = new(
        () =>
        {
            var stateString = stateStorageService.GetState("house_security");
            if (Enum.TryParse(stateString, out HouseSecurityState state))
            {
                return state;
            }

            return HouseSecurityState.OFF;
        },
        s => stateStorageService.UpdateState("house_security", s.ToString())
    );

    public IHouseSecurity Connect()
    {
        stateMachine.OnTransitionedAsync(UpdateSecurityState);

        stateMachine.Configure(HouseSecurityState.OFF)
            .Permit(HouseSecurityCommand.SET_HOME, HouseSecurityState.HOME)
            .Permit(HouseSecurityCommand.SET_AWAY, HouseSecurityState.AWAY)
            .Permit(HouseSecurityCommand.SET_NIGHT, HouseSecurityState.NIGHT)
            .Ignore(HouseSecurityCommand.SET_OFF);

        stateMachine.Configure(HouseSecurityState.AWAY)
            .Permit(HouseSecurityCommand.SET_HOME, HouseSecurityState.HOME)
            .Permit(HouseSecurityCommand.SET_NIGHT, HouseSecurityState.NIGHT)
            .Permit(HouseSecurityCommand.SET_OFF, HouseSecurityState.OFF)
            .Ignore(HouseSecurityCommand.SET_AWAY);

        stateMachine.Configure(HouseSecurityState.NIGHT)
            .Permit(HouseSecurityCommand.SET_HOME, HouseSecurityState.HOME)
            .Permit(HouseSecurityCommand.SET_AWAY, HouseSecurityState.AWAY)
            .Permit(HouseSecurityCommand.SET_OFF, HouseSecurityState.OFF)
            .Ignore(HouseSecurityCommand.SET_NIGHT);

        stateMachine.Configure(HouseSecurityState.HOME)
            .Permit(HouseSecurityCommand.SET_NIGHT, HouseSecurityState.NIGHT)
            .Permit(HouseSecurityCommand.SET_AWAY, HouseSecurityState.AWAY)
            .Permit(HouseSecurityCommand.SET_OFF, HouseSecurityState.OFF)
            .Ignore(HouseSecurityCommand.SET_HOME);

        return this;
    }

    public async Task Publish()
    {
        await homebridgeEventSender.HouseSecurityUpdate(State);
    }

    public async Task Trigger(HouseSecurityCommand command)
    {
        await stateMachine.FireAsync(command);
    }

    async Task UpdateSecurityState(StateMachine<HouseSecurityState, HouseSecurityCommand>.Transition e)
    {
        await homebridgeEventSender.HouseSecurityUpdate(e.Destination);
    }
}

public enum HouseSecurityState
{
    HOME,
    AWAY,
    NIGHT,
    OFF
}

public enum HouseSecurityCommand
{
    SET_HOME,
    SET_AWAY,
    SET_NIGHT,
    SET_OFF
}