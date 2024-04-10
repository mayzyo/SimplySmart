using SimplySmart.Core.Abstractions;
using SimplySmart.DeviceStates.Services;
using SimplySmart.Homebridge.Services;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.HouseStates.Features;

public interface IAutoLight
{
    AutoLightState State { get; }
    IAutoLight Connect();
    void Publish();
    void Trigger(AutoLightCommand command);
}

internal class AutoLight(IStateStorageService stateStorageService, IHomebridgeEventSender homebridgeEventSender, ILightSwitchService lightSwitchService) : IAutoLight
{
    public AutoLightState State { get { return stateMachine.State; } }
    public readonly StateMachine<AutoLightState, AutoLightCommand> stateMachine = new(
        () =>
        {
            var stateString = stateStorageService.GetState("auto_light");
            if (Enum.TryParse(stateString, out AutoLightState state))
            {
                return state;
            }

            return AutoLightState.OFF;
        },
        s => stateStorageService.UpdateState("auto_light", s.ToString())
    );

    public IAutoLight Connect()
    {
        stateMachine.Configure(AutoLightState.OFF)
            .Permit(AutoLightCommand.ON, AutoLightState.ON)
            .OnEntryAsync(DisableAuto);

        stateMachine.Configure(AutoLightState.ON)
            .Permit(AutoLightCommand.OFF, AutoLightState.OFF)
            .OnEntryAsync(EnableAuto);

        return this;
    }

    public void Publish()
    {
        stateMachine.Activate();
    }

    public void Trigger(AutoLightCommand command)
    {
        stateMachine.FireAsync(command);
    }

    private async Task DisableAuto()
    {
        lightSwitchService.SetAllToAuto(false);
        await homebridgeEventSender.SwitchOff("auto_light");
    }

    private async Task EnableAuto()
    {
        lightSwitchService.SetAllToAuto(true);
        await homebridgeEventSender.SwitchOn("auto_light");
    }
}

public enum AutoLightState
{
    ON,
    OFF,
}

public enum AutoLightCommand
{
    ON,
    OFF,
}