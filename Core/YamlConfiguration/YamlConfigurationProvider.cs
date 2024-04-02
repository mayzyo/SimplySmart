using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SimplySmart.Core.YamlConfiguration;

public class YamlConfigurationProvider(YamlConfigurationSource source) : FileConfigurationProvider(source)
{
    public override void Load(Stream stream)
    {
        //using var reader = new StreamReader(stream);
        //var yaml = new YamlStream();
        //yaml.Load(reader);

        var parser = new YamlConfigurationStreamParser();

        Data = parser.Parse(stream);
    }
}