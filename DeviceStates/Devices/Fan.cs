using SimplySmart.Core.Abstractions;
using SimplySmart.Homebridge.Services;
using SimplySmart.Zwave.Abstractions;
using SimplySmart.Zwave.Services;
using Stateless;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.DeviceStates.Devices;

public interface IFan : IAppliance, IBinarySwitch
{
    ApplianceState State { get; }
    IFan Connect();
    Task Publish();
    Task SetToOn(bool isOn);
}

internal class Fan(
    IStateStore stateStore,
    IHomebridgeEventSender homebridgeEventSender,
    IZwaveEventSender zwaveEventSender,
    string name
) : IFan
{
    public ApplianceState State { get { return stateMachine.State; } }
    public readonly StateMachine<ApplianceState, ApplianceCommand> stateMachine = new(
        () =>
        {
            var state = stateStore.GetState(name);
            if (Enum.TryParse(state, out ApplianceState myStatus))
            {
                return myStatus;
            }

            return ApplianceState.OFF;
        },
        s => stateStore.UpdateState(name, s.ToString())
    );

    public IFan Connect()
    {
        stateMachine.Configure(ApplianceState.PENDING_OFF)
            .OnEntryAsync(SendSetToOffEvents)
            .Permit(ApplianceCommand.SET_OFF, ApplianceState.OFF)
            .Permit(ApplianceCommand.SET_ON, ApplianceState.ON)
            .Ignore(ApplianceCommand.TURN_OFF);

        stateMachine.Configure(ApplianceState.OFF)
            .OnEntryAsync(SendCurrentlyOffEvents)
            .Permit(ApplianceCommand.TURN_ON, ApplianceState.PENDING_ON)
            .Permit(ApplianceCommand.SET_ON, ApplianceState.ON)
            .Ignore(ApplianceCommand.SET_OFF)
            .Ignore(ApplianceCommand.TURN_OFF);

        stateMachine.Configure(ApplianceState.PENDING_ON)
            .OnEntryAsync(SendSetToOnEvents)
            .Permit(ApplianceCommand.SET_ON, ApplianceState.ON)
            .Permit(ApplianceCommand.SET_OFF, ApplianceState.OFF)
            .Ignore(ApplianceCommand.TURN_ON);

        stateMachine.Configure(ApplianceState.ON)
            .OnEntryAsync(SendCurrentlyOnEvents)
            .Permit(ApplianceCommand.TURN_OFF, ApplianceState.PENDING_OFF)
            .Permit(ApplianceCommand.SET_OFF, ApplianceState.OFF)
            .Ignore(ApplianceCommand.SET_ON)
            .Ignore(ApplianceCommand.TURN_ON);

        return this;
    }

    public async Task Publish()
    {
        //await stateMachine.ActivateAsync();
    }

    public async Task SetToOn(bool isOn)
    {
        var command = isOn ? ApplianceCommand.TURN_ON : ApplianceCommand.TURN_OFF;
        await stateMachine.FireAsync(command);
    }

    public async Task SetCurrentValue(bool isOn)
    {
        var command = isOn ? ApplianceCommand.SET_ON : ApplianceCommand.SET_OFF;
        await stateMachine.FireAsync(command);
    }

    async Task SendSetToOnEvents()
    {
        await zwaveEventSender.BinarySwitchOn(name);
    }

    async Task SendSetToOffEvents()
    {   
        await zwaveEventSender.BinarySwitchOff(name);
    }

    async Task SendCurrentlyOnEvents()
    {
        await homebridgeEventSender.FanOn(name);
    }

    async Task SendCurrentlyOffEvents()
    {
        await homebridgeEventSender.FanOff(name);
    }
}