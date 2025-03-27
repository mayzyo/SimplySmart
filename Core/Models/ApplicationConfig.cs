using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimplySmart.Core.Models;

public class ApplicationConfig
{
    public required string Version { get; set; }

    public virtual List<Camera>? Cameras { get; set; }

    public virtual List<LightSwitch>? LightSwitches { get; set; }

    public virtual List<SmartImplant>? SmartImplants { get; set; }

    public virtual List<Fob>? Fobs { get; set; }

    public virtual List<MultiSensor>? MultiSensors { get; set; }

    public virtual List<PowerSwitch>? PowerSwitches { get; set; }

    public virtual List<DoorWindowSensor>? DoorWindowSensors { get; set; }
}

public class Camera
{
    public required string Name { get; set; }

    public string? LightSwitch { get; set; }

    public bool IsSurveillance { get; set; }
}

public class LightSwitch
{
    public required string Name { get; set; }

    public bool? IsDimmer { get; set; }
}

public class SmartImplant
{
    public required string Name { get; set; }

    public required string Type { get; set; }

    // If type is garageDoor
    public bool? CloseDetect { get; set; }

    public bool? OpenDetect { get; set; }
}

public class Fob
{
    public required string Name { get; set; }

    public required List<FobButton> FobButtons { get; set; }
}

public class FobButton
{
    public required string Name { get; set; }

    public required string Command { get; set; }
}

// Includes trisensor. We'll use boolean to determine if it has certain capability, similar to isDimmer.
public class MultiSensor
{
    public required string Name { get; set; }

    public string? LightSwitch { get; set; }
}

public class PowerSwitch
{
    public required string Name { get; set; }

    public string? Type { get; set; }
    // If type is sensor
    public WattsSensor? WattsSensor { get; set; }
}

public class WattsSensor
{
    public int Threshold { get; set; }

    public required string Type { get; set; }

    public string? LightSwitch { get; set; }
}

public class DoorWindowSensor
{
    public required string Name { get; set; }

    public string? GarageDoor { get; set; }
}