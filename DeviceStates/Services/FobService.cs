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

public interface IFobService
{
    IFob this[string key] { get; }

    bool Exists(string key);
}

internal class FobService : IFobService
{
    public IFob this[string key]
    {
        get
        {
            if (!states.TryGetValue(key, out IFob? value))
            {
                throw new Exception($"Fob with {key} does not exist");
            }

            return value;
        }
    }

    private readonly ILogger<FobService> logger;
    private readonly Dictionary<string, IFob> states = [];

    public FobService(IOptions<ApplicationConfig> options, ILogger<FobService> logger, IFobFactory fobFactory)
    {
        this.logger = logger;

        if (options.Value.fobs != null)
        {
            Initialise(options.Value, fobFactory);
        }
    }

    private void Initialise(ApplicationConfig config, IFobFactory fobFactory)
    {
        foreach (var fobConfig in config.fobs)
        {
            var fob = fobFactory.CreateFob(fobConfig.fobButtons);
            states.Add(fobConfig.name, fob);
        }

        logger.LogInformation("Fobs loaded successfully in Fob Service");
    }

    public bool Exists(string key)
    {
        return states.ContainsKey(key);
    }
}
