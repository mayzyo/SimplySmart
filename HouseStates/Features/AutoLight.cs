using SimplySmart.Core.Abstractions;
using SimplySmart.DeviceStates.Services;
using SimplySmart.Homebridge.Abstractions;
using SimplySmart.Homebridge.Services;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.HouseStates.Features;

public interface IAutoLight : ISwitch
{
    AutoLightState State { get; }
    IAutoLight Connect();
    Task Publish();
}

internal class AutoLight(IStateStore stateStorageService, IHomebridgeEventSender homebridgeEventSender, ILightSwitchService lightSwitchService) : IAutoLight
{
    public AutoLightState State { get { return stateMachine.State; } }
    public readonly StateMachine<AutoLightState, AutoLightCommand> stateMachine = new(
        () =>
        {
            var stateString = stateStorageService.GetState("auto_light/Office/0/0");
            if (Enum.TryParse(stateString, out AutoLightState state))
            {
                return state;
            }

            return AutoLightState.OFF;
        },
        s => stateStorageService.UpdateState("auto_light/Office/0/0", s.ToString())
    );

    public IAutoLight Connect()
    {
        stateMachine.Configure(AutoLightState.OFF)
            .OnEntryAsync(DisableAuto)
            .OnActivateAsync(DisableAuto)
            .Permit(AutoLightCommand.ON, AutoLightState.ON)
            .Ignore(AutoLightCommand.OFF);

        stateMachine.Configure(AutoLightState.ON)
            .OnEntryAsync(EnableAuto)
            .OnActivateAsync(EnableAuto)
            .Permit(AutoLightCommand.OFF, AutoLightState.OFF)
            .Ignore(AutoLightCommand.ON);

        return this;
    }

    public async Task Publish()
    {
        await stateMachine.ActivateAsync();
    }

    public async Task SetToOn(bool isOn)
    {
        var command = isOn ? AutoLightCommand.ON : AutoLightCommand.OFF;
        await stateMachine.FireAsync(command);
    }

    private async Task DisableAuto()
    {
        lightSwitchService.SetAllToAuto(false);
        await homebridgeEventSender.SwitchOff("auto_light/Office/0/0");
    }

    private async Task EnableAuto()
    {
        lightSwitchService.SetAllToAuto(true);
        await homebridgeEventSender.SwitchOn("auto_light/Office/0/0");
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