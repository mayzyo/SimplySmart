using SimplySmart.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Core.Services;

internal class InMemoryStateStore : IStateStore
{
    readonly Dictionary<string, string> states = [];

    public string? GetState(string key)
    {
        return states.GetValueOrDefault(key);
    }

    public void SetExpiringState(string key, string datetime, TimeSpan duration)
    {
        throw new NotImplementedException();
    }

    public void UpdateState(string key, string value)
    {
        if(!states.TryAdd(key, value))
        {
            states[key] = value;
        }
    }
}
