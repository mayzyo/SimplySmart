using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimplySmart.Utils;

public class ApplicationConfig
{
    public string version { get; set; }

    public List<AreaOccupant> areaOccupants { get; set; }

    public List<LightSwitch> lightSwitches { get; set; }

    public List<Surveillance> surveillances { get; set; }

    public List<SmartImplant> smartImplants { get; set; }

    public List<Fob> fobs { get; set; }
}

public class AreaOccupant
{
    public string name { get; set; }

    public string lightSwitch { get; set; }
}

public class LightSwitch
{
    public string name { get; set; }

    public int? stayOn { get; set; }
    public bool? isDimmer { get; set; }
}

public class Surveillance
{
    public string name { get; set; }
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