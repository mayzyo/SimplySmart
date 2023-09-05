using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimplySmart.Utils;

public class ApplicationConfig
{
    public string version { get; set; }

    public AreaOccupant[] areaOccupants { get; set; }

    public LightSwitch[] lightSwitches { get; set; }

    public List<Surveillance> surveillances { get; set; }
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