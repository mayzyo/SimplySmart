using SimplySmart.Core.Services;
using SimplySmart.Homebridge.Services;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.HouseStates.Services;

public interface IHouseSecurity
{
    HouseSecurityState State { get; }

    void Trigger(HouseSecurityCommand command);
}

internal class HouseSecurity : IHouseSecurity
{
    public HouseSecurityState State { get { return stateMachine.State; } }
    public readonly StateMachine<HouseSecurityState, HouseSecurityCommand> stateMachine;
    readonly IHomebridgeEventSender homebridgeEventSender;

    public HouseSecurity(IStateStorageService stateStorageService, IHomebridgeEventSender homebridgeEventSender)
    {
        this.homebridgeEventSender = homebridgeEventSender;
        stateMachine = new(
            () =>
            {
                var state = stateStorageService.GetState("house_security");
                if (Enum.TryParse(state, out HouseSecurityState myStatus))
                {
                    return myStatus;
                }

                return HouseSecurityState.OFF;
            },
            s => stateStorageService.UpdateState("house_security", s.ToString())
        );

        stateMachine.OnTransitionedAsync(Transitioned);

        stateMachine.Configure(HouseSecurityState.OFF)
            .Permit(HouseSecurityCommand.HOME, HouseSecurityState.HOME)
            .Permit(HouseSecurityCommand.AWAY, HouseSecurityState.AWAY)
            .Permit(HouseSecurityCommand.NIGHT, HouseSecurityState.NIGHT);

        stateMachine.Configure(HouseSecurityState.AWAY)
            .Permit(HouseSecurityCommand.HOME, HouseSecurityState.HOME)
            .Permit(HouseSecurityCommand.NIGHT, HouseSecurityState.NIGHT)
            .Permit(HouseSecurityCommand.OFF, HouseSecurityState.OFF);

        stateMachine.Configure(HouseSecurityState.NIGHT)
            .Permit(HouseSecurityCommand.HOME, HouseSecurityState.HOME)
            .Permit(HouseSecurityCommand.AWAY, HouseSecurityState.AWAY)
            .Permit(HouseSecurityCommand.OFF, HouseSecurityState.OFF);

        stateMachine.Configure(HouseSecurityState.HOME)
            .Permit(HouseSecurityCommand.NIGHT, HouseSecurityState.NIGHT)
            .Permit(HouseSecurityCommand.AWAY, HouseSecurityState.AWAY)
            .Permit(HouseSecurityCommand.OFF, HouseSecurityState.OFF);
    }

    public void Trigger(HouseSecurityCommand command)
    {
        stateMachine.FireAsync(command);
    }

    private async Task Transitioned(StateMachine<HouseSecurityState, HouseSecurityCommand>.Transition e)
    {
        if (e.Source == e.Destination)
        {
            return;
        }

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
    HOME,
    AWAY,
    NIGHT,
    OFF
}