using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimplySmart.Core.Models;
using SimplySmart.DeviceStates.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.DeviceStates.Services;

public interface IApplianceService
{
    IAppliance this[string key] { get; }

    bool Exists(string key);
}

internal class ApplianceService : IApplianceService
{
    public IAppliance this[string key]
    {
        get
        {
            if (!states.TryGetValue(key, out IAppliance? value))
            {
                throw new Exception($"Appliance with {key} does not exist");
            }

            return value;
        }
    }

    private readonly ILogger<ApplianceService> logger;
    private readonly Dictionary<string, IAppliance> states = [];

    public ApplianceService(IOptions<ApplicationConfig> options, ILogger<ApplianceService> logger, IApplianceFactory applianceFactory)
    {
        this.logger = logger;

        if (options.Value.powerSwitches != null)
        {
            InitialisePowerSwitch(options.Value, applianceFactory);
        }
    }

    public void InitialisePowerSwitch(ApplicationConfig appConfig, IApplianceFactory applianceFactory)
    {
        foreach (var config in appConfig.powerSwitches.Where(e => e.type == "fan"))
        {
            var fan = applianceFactory.CreateFan(config);
            states.Add(config.name, fan);
        }

        logger.LogInformation("Power Switches loaded successfully in Appliance Service");
    }

    public bool Exists(string key)
    {
        return states.ContainsKey(key);
    }
}
