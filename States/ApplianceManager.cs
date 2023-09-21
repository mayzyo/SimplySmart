using Microsoft.Extensions.Logging;
using SimplySmart.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace SimplySmart.States;

public interface IApplianceManager
{
    IAppliance this[string key] { get; }

    bool Exists(string key);
}

internal class ApplianceManager : IApplianceManager
{
    public IAppliance this[string key]
    { 
        get
        {
            if (!states.ContainsKey(key))
            {
                throw new Exception($"Appliance with {key} does not exist");
            }

            return states[key];
        }
    }

    private readonly ILogger<ApplianceManager> logger;
    private readonly IDictionary<string, IAppliance> states = new Dictionary<string, IAppliance>();

    public ApplianceManager(ILogger<ApplianceManager> logger, IDeserializer deserializer, IServiceProvider serviceProvider)
    {
        this.logger = logger;
        var config = DeserialiseConfig(deserializer);

        if (config.powerSwitches != null)
        {
            InitialisePowerSwitch(config, serviceProvider);
        }
    }

    private void InitialisePowerSwitch(ApplicationConfig appConfig, IServiceProvider serviceProvider)
    {
        foreach (var config in appConfig.powerSwitches.Where(e => e.type == "electricBlanket"))
        {
            var electricBlanket = new ElectricBlanket(config.name);
            electricBlanket.Initialise(serviceProvider);
            states.Add(config.name, electricBlanket);
        }

        foreach (var config in appConfig.powerSwitches.Where(e => e.type == "fan"))
        {
            var fan = new Fan(config.name);
            fan.Initialise(serviceProvider);
            states.Add(config.name, fan);
        }

        logger.LogInformation("Power Switches loaded successfully in Appliance Manager");
    }

    private static ApplicationConfig DeserialiseConfig(IDeserializer deserializer)
    {
        var path = Environment.GetEnvironmentVariable("CONFIG_FILE_PATH") ?? throw new Exception("Config file missing!");
        using var sr = File.OpenText(path);
        return deserializer.Deserialize<ApplicationConfig>(sr);
    }

    public bool Exists(string key)
    {
        return states.ContainsKey(key);
    }
}
