using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimplySmart.Core.Models;

public class ApplicationConfig
{
    public string version { get; set; }

    public virtual List<Camera> cameras { get; set; }

    public virtual List<LightSwitch> lightSwitches { get; set; }

    public virtual List<SmartImplant> smartImplants { get; set; }

    public virtual List<Fob> fobs { get; set; }

    public virtual List<MultiSensor> multiSensors { get; set; }

    public virtual List<PowerSwitch> powerSwitches { get; set; }
}

public class Camera
{
    public string name { get; set; }

    public string? lightSwitch { get; set; }

    public bool isSurveillance { get; set; }
}

public class LightSwitch
{
    public string name { get; set; }

    public int? stayOn { get; set; }
    public bool? isDimmer { get; set; }
}

public class SmartImplant
{
    public string name { get; set; }

    public string type { get; set; }
}

public class Fob
{
    public string name { get; set; }

    public List<FobButton> fobButtons { get; set; }
}

public class FobButton
{
    public string name { get; set; }

    public string command { get; set; }
}

public class MultiSensor
{
    public string name { get; set; }

    public string? lightSwitch { get; set; }
}

public class PowerSwitch
{
    public string name { get; set; }

    public string type { get; set; }
}