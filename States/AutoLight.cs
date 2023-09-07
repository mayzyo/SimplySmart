using Microsoft.Extensions.DependencyInjection;
using SimplySmart.Homebridge;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.States;

public interface IAutoLight
{
    AutoLightState State { get; }

    void Initialise(ILightSwitchManager lightSwitchManager, IServiceProvider serviceProvider);

    void Trigger(AutoLightCommand command);
}

internal class AutoLight : IAutoLight
{
    public AutoLightState State { get { return stateMachine.State; } }
    public readonly StateMachine<AutoLightState, AutoLightCommand> stateMachine = new(AutoLightState.OFF);

    private ILightSwitchManager? lightSwitchManager;
    private IServiceProvider? serviceProvider;

    public void Initialise(ILightSwitchManager lightSwitchManager, IServiceProvider serviceProvider)
    {
        this.lightSwitchManager = lightSwitchManager;
        this.serviceProvider = serviceProvider;

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
        lightSwitchManager?.All(LightSwitchCommand.DISABLE_AUTO);

        if(serviceProvider != null)
        {
            using var scope = serviceProvider.CreateScope();

            IHomebridgeSwitchHandler homebridgeHandler = scope.ServiceProvider.GetRequiredService<IHomebridgeSwitchHandler>();
            await homebridgeHandler.HandleOff("auto_light");
        }
    }

    private async Task EnableAuto()
    {
        lightSwitchManager?.All(LightSwitchCommand.ENABLE_AUTO);

        if (serviceProvider != null)
        {
            using var scope = serviceProvider.CreateScope();

            IHomebridgeSwitchHandler homebridgeHandler = scope.ServiceProvider.GetRequiredService<IHomebridgeSwitchHandler>();
            await homebridgeHandler.HandleOn("auto_light");
        }
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