using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFrigateSorter;

public class Configuration
{
    public IEnumerable<string>? OutdoorCameras { get; set; }
    public bool IsHome { get; set; }
}
