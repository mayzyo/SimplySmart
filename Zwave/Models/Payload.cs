using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Zwave.Models;

public class Payload
{
    public long time { get; set; }

    public ushort value { get; set; }
}
