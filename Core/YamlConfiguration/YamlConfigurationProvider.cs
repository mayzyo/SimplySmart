using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Core.YamlConfiguration;

public class YamlConfigurationProvider(YamlConfigurationSource source) : FileConfigurationProvider(source)
{
    public override void Load(Stream stream)
    {
        var parser = new YamlConfigurationStreamParser();

        Data = parser.Parse(stream);
    }
}