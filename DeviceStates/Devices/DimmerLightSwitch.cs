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
    IStateStorageService stateStorage,
    IHomebridgeEventSender homebridgeEventSender,
    IZwaveEventSender zwaveEventSender,
    string name,
    int? stayOn
) : LightSwitch(
    stateStorage,
    homebridgeEventSender,
    zwaveEventSender,
    name,
    stayOn
), IDimmerLightSwitch
{
    public ushort Brightness
    {
        get {
            var brightnessString = stateStorage.GetState(name + "_brightness") ?? "0";
            if (ushort.TryParse(brightnessString, out ushort brightness))
            {
                return brightness;
            }

            return 0;
        }
        set { stateStorage.UpdateState(name + "_brightness", value.ToString()); }
    }

    ushort newBrightness = 0;

    public new IDimmerLightSwitch Connect()
    {
        base.Connect();

        stateMachine.Configure(LightSwitchState.ON)
            .OnEntry(() => Brightness = newBrightness);

        stateMachine.Configure(LightSwitchState.OFF)
            .OnEntry(() => Brightness = newBrightness);

        return this;
    }

    public async Task SetLevel(ushort level)
    {
        newBrightness = level;
        await base.SetToOn(level != 0);
    }

    public override async Task SetToOn(bool isOn)
    {
        newBrightness = (ushort)(isOn ? 100 : 0);
        await base.SetToOn(isOn);
    }

    public override async Task AutoSetToOn()
    {
        newBrightness = 100;
        await base.AutoSetToOn();
    }

    public override async Task AutoSetToOff()
    {
        newBrightness = 0;
        await base.AutoSetToOff();
    }

    protected override void ConfigureOnToOnGuardClause()
    {
        stateMachine.Configure(LightSwitchState.ON)
            .PermitReentryIf(LightSwitchCommand.TURN_ON, () => Brightness != newBrightness)
            .IgnoreIf(LightSwitchCommand.TURN_ON, () => Brightness == newBrightness);
    }

    protected override async Task SendOnEvents()
    {
        await zwaveEventSender.MultiLevelSwitchUpdate(name, newBrightness);
        await homebridgeEventSender.LightSwitchOn(name, newBrightness);
    }
}