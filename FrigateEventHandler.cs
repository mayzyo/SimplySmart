using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SimpleFrigateSorter;

public interface IFrigateEventHandler
{
    void HandleEvents(string message);
}

public class FrigateEventHandler : IFrigateEventHandler
{
    private IManagedMqttClient managedMqttClient;
    public FrigateEventHandler(IManagedMqttClient managedMqttClient)
    {
        this.managedMqttClient = managedMqttClient;
    }

    public async void HandleEvents(string message)
    {
        try
        {
            var frigateEvent = JsonSerializer.Deserialize<FrigateEvent>(message);

            if (frigateEvent?.type == "new")
            {
                await managedMqttClient.EnqueueAsync("frigate/new-events", message);
            }
            else if (frigateEvent?.type == "end")
            {
                await managedMqttClient.EnqueueAsync("frigate/end-events", message);
            }
        }
        catch
        {
            Console.WriteLine("ERROR: message not in JSON format.");
        }
    }
}
