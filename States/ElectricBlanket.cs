using Microsoft.Extensions.DependencyInjection;
using SimplySmart.Homebridge;
using SimplySmart.Zwave;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.States;

public interface IElectricBlanket : IAppliance
{
    ApplianceState State { get; }

    void Initialise(IServiceProvider serviceProvider);

    Task Trigger(ApplianceCommand command);
}

internal class ElectricBlanket : IElectricBlanket
{
    public ApplianceState State { get { return stateMachine.State; } }
    public readonly StateMachine<ApplianceState, ApplianceCommand> stateMachine = new(ApplianceState.OFF);
    private readonly string name;
    private IServiceProvider? serviceProvider;

    public ElectricBlanket(string name)
    {
        this.name = name;
    }

    public void Initialise(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;

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
        if (serviceProvider != null)
        {
            using var scope = serviceProvider.CreateScope();

            IHomebridgeHeaterCoolerHandler homebridgeHandler = scope.ServiceProvider.GetRequiredService<IHomebridgeHeaterCoolerHandler>();
            await homebridgeHandler.HandleOn(name);
            IZwaveBinarySwitchHandler zwaveHandler = scope.ServiceProvider.GetRequiredService<IZwaveBinarySwitchHandler>();
            await zwaveHandler.HandleOn(name);
        }
    }

    private async Task SetToOff()
    {
        if (serviceProvider != null)
        {
            using var scope = serviceProvider.CreateScope();

            IHomebridgeHeaterCoolerHandler homebridgeHandler = scope.ServiceProvider.GetRequiredService<IHomebridgeHeaterCoolerHandler>();
            await homebridgeHandler.HandleOff(name);
            IZwaveBinarySwitchHandler zwaveHandler = scope.ServiceProvider.GetRequiredService<IZwaveBinarySwitchHandler>();
            await zwaveHandler.HandleOff(name);
        }
    }
}