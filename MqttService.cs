using Microsoft.Extensions.Configuration;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using System.Text;
using System.Text.Json;

namespace SimpleFrigateSorter;

public static class MqttService
{
    private static MqttClientCredentials getCredentials()
    {
        var username = Environment.GetEnvironmentVariable("MQTT_USERNAME");
        var password = Environment.GetEnvironmentVariable("MQTT_PASSWORD");

        if(username == null || password == null)
        {
            var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

            var secretProvider = config.Providers.First();
            secretProvider.TryGet("MQTT_USERNAME", out username);
            secretProvider.TryGet("MQTT_PASSWORD", out password);
        }

        return new MqttClientCredentials(username, Encoding.ASCII.GetBytes(password));
    }

    public static async Task ConnectClient()
    {
        /*
         * The managed client extends the existing _MqttClient_. It adds the following features.
         * - Reconnecting when connection is lost.
         * - Storing pending messages in an internal queue so that an enqueue is possible while the client remains not connected.
         */

        var mqttFactory = new MqttFactory();

        using var managedMqttClient = mqttFactory.CreateManagedMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(Environment.GetEnvironmentVariable("MQTT_URL"))
            .WithCredentials(getCredentials())
            .Build();

        var managedMqttClientOptions = new ManagedMqttClientOptionsBuilder()
            .WithClientOptions(mqttClientOptions)
            .Build();

        await managedMqttClient.StartAsync(managedMqttClientOptions);
        var col = new List<MqttTopicFilter>
            {
                new MqttTopicFilterBuilder().WithTopic("frigate/events").Build()
            };


        managedMqttClient.ApplicationMessageReceivedAsync += async (e) =>
        {
            var message = e.ApplicationMessage.ConvertPayloadToString();
            try
            {
                var frigateEvent = JsonSerializer.Deserialize<FrigateEvent>(message);
                Console.WriteLine(frigateEvent.type);

                if (frigateEvent.type == "new")
                {
                    await managedMqttClient.EnqueueAsync("frigate/new-events", message);
                }
                else if (frigateEvent.type == "end")
                {
                    await managedMqttClient.EnqueueAsync("frigate/end-events", message);
                }
            }
            catch
            {
                Console.WriteLine("ERROR: message not in JSON format.");
            }
        };

        await managedMqttClient.SubscribeAsync(col);
        Console.WriteLine("The managed MQTT client is connected.");

        while (true)
        {
            //string json = JsonSerializer.Serialize(new { message = "Hi Mqtt", sent = DateTime.UtcNow });
            //managedMqttClient.ApplicationMessageReceivedAsync = (e) => Console.WriteLine("Connected");
            //await _mqttClient.PublishAsync("behroozbc.ir/topic/json", json);

            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
}

