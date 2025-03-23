using SimplySmart.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Core.Stubs;

internal class PassthroughEventSenderStub : IPassthroughEventSender
{
    public async Task CarAlertEvent(string message)
    {
        Console.WriteLine($"Car Alert Event - simply_smart/house_security/car");
        await Task.CompletedTask;
    }

    public async Task AlertEvent(string message)
    {
        Console.WriteLine($"Alert Event - simply_smart/house_security/alert");
        await Task.CompletedTask;
    }
}
