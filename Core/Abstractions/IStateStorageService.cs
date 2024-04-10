using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Core.Abstractions;

public interface IStateStorageService
{
    string? GetState(string key);
    void UpdateState(string key, string value);
    void SetExpiringState(string key, string value);
}
