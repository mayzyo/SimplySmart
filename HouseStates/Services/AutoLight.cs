using SimplySmart.Core.Services;
using SimplySmart.DeviceStates.Services;
using SimplySmart.Homebridge.Services;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.HouseStates.Services;

public interface IAutoLight
{
    AutoLightState State { get; }
    void Trigger(AutoLightCommand command);
}

internal class AutoLight : IAutoLight
{
    public AutoLightState State { get { return stateMachine.State; } }
    public readonly StateMachine<AutoLightState, AutoLightCommand> stateMachine;

    private readonly ILightSwitchService lightSwitchService;
    private readonly IHomebridgeEventSender homebridgeEventSender;

    public AutoLight(IStateStorageService stateStorageService, IHomebridgeEventSender homebridgeEventSender, ILightSwitchService lightSwitchService)
    {
        this.lightSwitchService = lightSwitchService;
        this.homebridgeEventSender = homebridgeEventSender;

        stateMachine = new(
            () =>
            {
                var state = stateStorageService.GetState("auto_light");
                if (Enum.TryParse(state, out AutoLightState myStatus))
                {
                    return myStatus;
                }

                return AutoLightState.OFF;
            },
            s => stateStorageService.UpdateState("auto_light", s.ToString())
        );

        stateMachine.Configure(AutoLightState.OFF)
            .Permit(AutoLightCommand.ON, AutoLightState.ON)
            .OnEntryAsync(DisableAuto);

        stateMachine.Configure(AutoLightState.ON)
            .Permit(AutoLightCommand.OFF, AutoLightState.OFF)
            .OnEntryAsync(EnableAuto);
    }

    public void Trigger(AutoLightCommand command)
    {
        stateMachine.FireAsync(command);
    }

    private async Task DisableAuto()
    {
        lightSwitchService.All(LightSwitchCommand.DISABLE_AUTO);
        await homebridgeEventSender.SwitchOff("auto_light");
    }

    private async Task EnableAuto()
    {
        lightSwitchService.All(LightSwitchCommand.ENABLE_AUTO);
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