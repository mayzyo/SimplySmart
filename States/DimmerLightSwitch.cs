using SimplySmart.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimplySmart.States;

public interface IDimmerLightSwitch : ILightSwitch
{
    ushort Brightness { get; }
    ushort PrevBrightness { get; }
    BroadcastSource Source { get; }
    void Trigger(LightSwitchCommand command, ushort brightness, BroadcastSource source);
    bool LevelChange();
}

public class DimmerLightSwitch : LightSwitch, IDimmerLightSwitch
{
    private ushort brightness = 0;
    private ushort prevBrightness = 0;
    private BroadcastSource source;

    public BroadcastSource Source { get { return source; } }
    public ushort Brightness { get { return brightness; } }
    public ushort PrevBrightness { get { return prevBrightness; } }

    public DimmerLightSwitch(int? stayOn) : base(stayOn)
    {

    }

    public void Trigger(LightSwitchCommand command, ushort brightness, BroadcastSource source)
    {
        prevBrightness = this.brightness;
        this.brightness = brightness;
        this.source = source;
        Trigger(command);
    }

    public bool LevelChange()
    {
        return brightness != prevBrightness;
    }
}