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
    Task SetBrightness(ushort brightness);
}

public class DimmerLightSwitch : LightSwitch, IDimmerLightSwitch
{
    public ushort Brightness
    {
        get { return brightness; }
    }

    ushort brightness;

    public DimmerLightSwitch(
        IStateStore stateStore,
        ISchedulerFactory schedulerFactory,
        IHomebridgeEventSender homebridgeEventSender,
        IZwaveEventSender zwaveEventSender,
        string name
    ) : base(
        stateStore,
        schedulerFactory,
        homebridgeEventSender,
        zwaveEventSender,
        name
    )
    {
        if (ushort.TryParse(stateStore.GetState(name + "_brightness"), out ushort brightness))
        {
            this.brightness = brightness;
        }

        if (brightness == 0)
        {
            stateStore.UpdateState(name, LightSwitchState.OFF.ToString());
        }
    }

    public new IDimmerLightSwitch Connect()
    {
        base.Connect();
        return this;
    }

    public async Task SetCurrentLevel(ushort level)
    {
        await SetCurrentValue(level != 0);

        if (brightness == level)
        {
            await SendBrightnessEvents();
        }
    }

    // This distinctly only set the brightness value and not the on off state.
    // Homebridge for instance has 2 separate controls for brightness and on off.
    public async Task SetBrightness(ushort brightness)
    {
        // Brightness only affects On state. No need to mess with it when setting to off.
        if (brightness != 0 && this.brightness != brightness)
        {
            this.brightness = brightness;
            stateStore.UpdateState(name + "_brightness", brightness.ToString());
            await SendSetLevelEvents();
        }
    }

    protected override async Task SendSetToOnEvents()
    {
        await zwaveEventSender.MultiLevelSwitchUpdate(name, brightness);
    }

    protected override async Task SendSetToOffEvents()
    {
        await zwaveEventSender.MultiLevelSwitchUpdate(name, 0);
    }

    protected override async Task SendCurrentlyOnEvents()
    {
        await homebridgeEventSender.LightSwitchOn(name);
    }

    protected override async Task SendCurrentlyOffEvents()
    {
        await homebridgeEventSender.LightSwitchOff(name);
    }

    async Task SendSetLevelEvents()
    {
        await zwaveEventSender.MultiLevelSwitchUpdate(name, brightness);
    }

    async Task SendBrightnessEvents()
    {
        await homebridgeEventSender.DimmerBrightness(name, brightness);
    }
}