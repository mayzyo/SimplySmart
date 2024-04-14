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

    readonly ushort brightness;

    public DimmerLightSwitch(
        IStateStore stateStore,
        ISchedulerFactory schedulerFactory,
        IHomebridgeEventSender homebridgeEventSender,
        IZwaveEventSender zwaveEventSender,
        string name,
        bool isZwave = false
    ) : base(
        stateStore,
        schedulerFactory,
        homebridgeEventSender,
        zwaveEventSender,
        name,
        isZwave
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

    public async Task SetLevel(ushort level)
    {
        await SetBrightness(level);
        await SetToOn(level != 0);
    }

    // This distinctly only set the brightness value and not the on off state.
    // Homebridge for instance has 2 separate controls for brightness and on off.
    public async Task SetBrightness(ushort brightness)
    {
        // Brightness only affects On state. No need to mess with it when setting to off.
        if (brightness != 0 && this.brightness != brightness)
        {
            stateStore.UpdateState(name + "_brightness", brightness.ToString());
            await zwaveEventSender.MultiLevelSwitchUpdate(name, brightness);
            await homebridgeEventSender.DimmerBrightness(name, brightness);
        }
    }

    protected override async Task SendOnEvents()
    {
        if(!isZwave)
        {
            await zwaveEventSender.MultiLevelSwitchUpdate(name, brightness);
        }
        await homebridgeEventSender.LightSwitchOn(name);
    }

    protected override async Task SendOffEvents()
    {
        if(!isZwave)
        {
            await zwaveEventSender.MultiLevelSwitchUpdate(name, 0);
        }
        await homebridgeEventSender.LightSwitchOff(name);
    }
}