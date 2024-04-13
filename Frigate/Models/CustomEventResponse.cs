using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Frigate.Models;

internal class CustomEventResponse
{
    public string event_id { get; set; }
    public string message { get; set; }
    public bool success { get; set; }
}
