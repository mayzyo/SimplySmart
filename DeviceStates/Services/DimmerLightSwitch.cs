using SimplySmart.Core.Models;
using SimplySmart.Core.Services;
using SimplySmart.Homebridge.Services;
using SimplySmart.Zwave.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.DeviceStates.Services;

public interface IDimmerLightSwitch : ILightSwitch
{
    ushort Brightness { get; }
    ushort PrevBrightness { get; }
    BroadcastSource Source { get; }
    void Trigger(LightSwitchCommand command, ushort brightness, BroadcastSource source);
    bool LevelChange();
}

public class DimmerLightSwitch(IStateStorageService stateStorage, IHomebridgeEventSender homebridgeEventSender, IZwaveEventSender zwaveEventSender, string name, int? stayOn) : LightSwitch(stateStorage, homebridgeEventSender, zwaveEventSender, name, stayOn), IDimmerLightSwitch
{
    private ushort brightness = 0;
    private ushort prevBrightness = 0;

    public ushort Brightness { get { return brightness; } }
    public ushort PrevBrightness { get { return prevBrightness; } }

    public void Trigger(LightSwitchCommand command, ushort brightness, BroadcastSource source)
    {
        prevBrightness = this.brightness;
        this.brightness = brightness;
        Trigger(command, source);
    }

    public bool LevelChange()
    {
        return brightness != prevBrightness;
    }

    protected override async Task SetToOn()
    {
        if (Source != BroadcastSource.ZWAVE)
        {
            await zwaveEventSender.MultiLevelSwitchUpdate(name, Brightness);
        }

        if (Source != BroadcastSource.HOMEBRIDGE)
        {
            await homebridgeEventSender.LightSwitchOn(name, Brightness);

        }
    }

    protected override void ConfigureOnState()
    {
        stateMachine.Configure(LightSwitchState.MANUAL_ON)
            .OnEntryAsync(SetToOn)
            .PermitReentryIf(LightSwitchCommand.MANUAL_ON, LevelChange);
    }
}