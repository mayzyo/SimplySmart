using SimplySmart.Zwave.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Zwave.Stubs;

internal class ZwaveEventSenderStub : IZwaveEventSender
{
    public Task BinarySwitchOff(string triggerUri)
    {
        Console.WriteLine($"Binary Switch Off - {triggerUri}");
        return Task.CompletedTask;
    }

    public Task BinarySwitchOn(string triggerUri)
    {
        Console.WriteLine($"Binary Switch On - {triggerUri}");
        return Task.CompletedTask;
    }

    public Task MultiLevelSwitchUpdate(string triggerUri, ushort brightness)
    {
        Console.WriteLine($"Multi-level Switch Update - {triggerUri}");
        return Task.CompletedTask;
    }
}
