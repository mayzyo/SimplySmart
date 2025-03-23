using MQTTnet.Extensions.ManagedClient;
using SimplySmart.Frigate.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimplySmart.Frigate.Stubs;

internal class FrigateWebhookSenderStub(IManagedMqttClient mqttClient) : FrigateWebhookSender(mqttClient), IFrigateWebhookSender
{
    public async new Task<string> CreateGarageDoorSnapshot(string cameraName)
    {
        Console.WriteLine($"Create Garage Door Snapshot - {cameraName}");
        return await Task.FromResult("Stubbed Result");
    }
}
