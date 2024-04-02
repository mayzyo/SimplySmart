using SimplySmart.Core.Services;
using SimplySmart.Homebridge.Services;
using SimplySmart.Zwave.Services;
using Stateless;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.DeviceStates.Services;

public interface IFan : IAppliance
{
    ApplianceState State { get; }
    Task Trigger(ApplianceCommand command);
}

internal class Fan : IFan
{
    public ApplianceState State { get { return stateMachine.State; } }
    public readonly StateMachine<ApplianceState, ApplianceCommand> stateMachine;
    public readonly string name;
    
    readonly IHomebridgeEventSender homebridgeEventSender;
    readonly IZwaveEventSender zwaveEventSender;

    public Fan(IStateStorageService stateStorage, string name, IHomebridgeEventSender homebridgeEventSender, IZwaveEventSender zwaveEventSender)
    {
        this.name = name;
        this.homebridgeEventSender = homebridgeEventSender;
        this.zwaveEventSender = zwaveEventSender;

        stateMachine = new(
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

        stateMachine.Configure(ApplianceState.OFF)
            .OnEntryAsync(SetToOff)
            .Permit(ApplianceCommand.ON, ApplianceState.ON);

        stateMachine.Configure(ApplianceState.ON)
            .OnEntryAsync(SetToOn)
            .Permit(ApplianceCommand.OFF, ApplianceState.OFF);
    }

    public async Task Trigger(ApplianceCommand command)
    {
        await stateMachine.FireAsync(command);
    }

    private async Task SetToOn()
    {
        await homebridgeEventSender.FanOn(name);
        await zwaveEventSender.BinarySwitchOn(name);
    }

    private async Task SetToOff()
    {
        await homebridgeEventSender.FanOff(name);
        await zwaveEventSender.BinarySwitchOff(name);
    }
}