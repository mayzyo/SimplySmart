using Quartz;
using SimplySmart.Core.Abstractions;
using SimplySmart.Homebridge.Services;
using SimplySmart.Zwave.Abstractions;
using SimplySmart.Zwave.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.DeviceStates.Devices;

public interface IDimmerLightSwitch : ILightSwitch, IMultiLevelSwitch
{
    ushort Brightness { get; }
}

public class DimmerLightSwitch(
    IStateStore stateStore,
    ISchedulerFactory schedulerFactory,
    IHomebridgeEventSender homebridgeEventSender,
    IZwaveEventSender zwaveEventSender,
    string name
) : LightSwitch(
    stateStore,
    schedulerFactory,
    homebridgeEventSender,
    zwaveEventSender,
    name
), IDimmerLightSwitch
{
    public ushort Brightness
    {
        get { return brightness; }
    }

    ushort brightness = 0;
    ushort newBrightness = 0;

    public new IDimmerLightSwitch Connect()
    {
        stateMachine.Configure(LightSwitchState.ON)
            .OnEntry(SetBrightness);

        stateMachine.Configure(LightSwitchState.OFF)
            .OnEntry(SetBrightness);

        base.Connect();

        return this;
    }

    public async Task SetLevel(ushort level)
    {
        newBrightness = level;
        await base.SetToOn(level != 0);
    }

    public override async Task SetToOn(bool isOn)
    {
        if(isOn && brightness == 0)
        {
            newBrightness = 100;
        }
        else
        {
            newBrightness = brightness;
        }

        await base.SetToOn(isOn);
    }

    public override async Task AutoSetToOn()
    {
        if (brightness == 0)
        {
            newBrightness = 100;
        }

        await base.AutoSetToOn();
    }

    public override async Task AutoSetToOff()
    {
        newBrightness = brightness;
        await base.AutoSetToOff();
    }

    protected override void ConfigureOnToOnGuardClause()
    {
        stateMachine.Configure(LightSwitchState.ON)
            .PermitReentryIf(LightSwitchCommand.TURN_ON, () => Brightness != newBrightness)
            .IgnoreIf(LightSwitchCommand.TURN_ON, () => Brightness == newBrightness);
    }

    protected override LightSwitchState InitialState()
    {
        var brightnessString = stateStore.GetState(name + "_brightness") ?? "0";
        if (ushort.TryParse(brightnessString, out ushort brightness))
        {
            this.brightness = brightness;
        }

        var stateString = stateStore.GetState(name);
        if (Enum.TryParse(stateString, out LightSwitchState state))
        {
            if(this.brightness == 0)
            {
                return LightSwitchState.OFF;
            }
            return state;
        }

        return LightSwitchState.OFF;
    }

    protected override async Task SendOnEvents()
    {
        await zwaveEventSender.MultiLevelSwitchUpdate(name, Brightness);
        await homebridgeEventSender.DimmerBrightness(name, Brightness);
        await homebridgeEventSender.LightSwitchOn(name);
    }

    protected override async Task SendOffEvents()
    {
        await zwaveEventSender.MultiLevelSwitchUpdate(name, 0);
        await homebridgeEventSender.DimmerBrightness(name, Brightness);
        await homebridgeEventSender.LightSwitchOff(name);
    }

    void SetBrightness()
    {
        brightness = newBrightness;
        stateStore.UpdateState(name + "_brightness", newBrightness.ToString());
    }
}