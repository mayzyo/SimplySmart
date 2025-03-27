using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SimplySmart.Zwave.Models;

public class ElectricConsumption
{
    [JsonPropertyName("time")]
    public long Time { get; set; }

    [JsonPropertyName("value")]
    public float Value { get; set; }
}
