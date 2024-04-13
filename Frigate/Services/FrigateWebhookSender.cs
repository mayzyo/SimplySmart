using Microsoft.Extensions.Configuration;
using MQTTnet.Extensions.ManagedClient;
using SimplySmart.Frigate.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimplySmart.Frigate.Services;

public interface IFrigateWebhookSender
{
    Task<string> CreateGarageDoorSnapshot(string cameraName);
}

internal class FrigateWebhookSender(IManagedMqttClient mqttClient) : IFrigateWebhookSender
{
    public async Task<string> CreateGarageDoorSnapshot(string cameraName)
    {
        using HttpClient client = new();
        client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("FRIGATE_URL") ?? "");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.ConnectionClose = true;
        client.DefaultRequestHeaders.Add("Authorization", "Basic " + GetCredentials());
        client.DefaultRequestHeaders.Add("X-Csrf-Token", "1");

        using StringContent jsonContent = new(
            JsonSerializer.Serialize(new {}),
            Encoding.UTF8,
            "application/json"
        );

        using HttpResponseMessage response =
            await client.PostAsync($"api/events/{cameraName}/garage_door/create", jsonContent);

        var jsonResponse = await response.Content.ReadFromJsonAsync<CustomEventResponse>();

        if(jsonResponse != null)
        {
            await mqttClient.EnqueueAsync($"frigate/{cameraName}/garage_door/event_id", jsonResponse.event_id);

            return jsonResponse.event_id;
        }

        return "";
    }

    static string GetCredentials()
    {
        var username = Environment.GetEnvironmentVariable("FRIGATE_USERNAME");
        var password = Environment.GetEnvironmentVariable("FRIGATE_PASSWORD");

        if (username == null || password == null)
        {
            var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

            var secretProvider = config.Providers.First();
            secretProvider.TryGet("FRIGATE_USERNAME", out username);
            secretProvider.TryGet("FRIGATE_PASSWORD", out password);
        }

        var credentialString = $"{username}:{password}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(credentialString));
    }
}
