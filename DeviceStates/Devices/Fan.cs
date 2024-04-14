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
}

internal class Fan(
    IStateStore stateStorage,
    IHomebridgeEventSender homebridgeEventSender,
    IZwaveEventSender zwaveEventSender,
    string name,
    bool isZwave = false
) : IFan
{
    public ApplianceState State { get { return stateMachine.State; } }
    public readonly StateMachine<ApplianceState, ApplianceCommand> stateMachine = new(
        () =>
        {
            var state = stateStorage.GetState(name);
            if (Enum.TryParse(state, out ApplianceState myStatus))
            {
                return myStatus;
            }

            return ApplianceState.OFF;
        },
        s => stateStorage.UpdateState(name, s.ToString())
    );
    public readonly string name = name;

    public IFan Connect()
    {
        stateMachine.Configure(ApplianceState.OFF)
            .OnEntryAsync(SetToOff)
            .OnActivateAsync(SetToOff)
            .Permit(ApplianceCommand.ON, ApplianceState.ON)
            .Ignore(ApplianceCommand.OFF);

        stateMachine.Configure(ApplianceState.ON)
            .OnEntryAsync(SetToOn)
            .OnActivateAsync(SetToOn)
            .Permit(ApplianceCommand.OFF, ApplianceState.OFF)
            .Ignore(ApplianceCommand.ON);

        return this;
    }

    public async Task Publish()
    {
        await stateMachine.ActivateAsync();
    }

    public async Task SetToOn(bool isOn)
    {
        var command = isOn ? ApplianceCommand.ON : ApplianceCommand.OFF;
        await stateMachine.FireAsync(command);
    }

    async Task SetToOn()
    {
        await homebridgeEventSender.FanOn(name);
        if(isZwave)
        {
            await zwaveEventSender.BinarySwitchOn(name);
        }
    }

    async Task SetToOff()
    {
        await homebridgeEventSender.FanOff(name);
        if(isZwave)
        {
            await zwaveEventSender.BinarySwitchOff(name);
        }
    }
}