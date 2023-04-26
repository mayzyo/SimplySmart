using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleFrigateSorter;

public class MqttClientService : IHostedService
{
    private readonly ILogger<MqttClientService> logger;
    private readonly IServiceProvider serviceProvider;
    private readonly IManagedMqttClient managedMqttClient;

    public MqttClientService(ILogger<MqttClientService> logger, IServiceProvider serviceProvider, IManagedMqttClient managedMqttClient)
    {
        this.logger = logger;
        this.serviceProvider = serviceProvider;
        this.managedMqttClient = managedMqttClient;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        managedMqttClient.ApplicationMessageReceivedAsync += async (e) =>
        {
            var message = e.ApplicationMessage.ConvertPayloadToString();
            using var scope = serviceProvider.CreateScope();

            var frigateEventHandler = scope.ServiceProvider.GetRequiredService<IFrigateEventHandler>();
            frigateEventHandler.HandleEvents(message);
        };

        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(Environment.GetEnvironmentVariable("MQTT_URL"))
            .WithCredentials(GetCredentials())
            .Build();

        var managedMqttClientOptions = new ManagedMqttClientOptionsBuilder()
            .WithClientOptions(mqttClientOptions)
            .Build();

        try
        {
            await managedMqttClient.StartAsync(managedMqttClientOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error connecting to MQTT server");
        }

        try
        {
            var topics = new List<MqttTopicFilter>
            {
                new MqttTopicFilterBuilder().WithTopic("frigate/test").Build()
            };

            await managedMqttClient.SubscribeAsync(topics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error subscribing to MQTT server");
        }

        logger.LogInformation("MQTT client started");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await managedMqttClient.StopAsync();
        logger.LogInformation("MQTT client stopped");
    }

    private MqttClientCredentials GetCredentials()
    {
        var username = Environment.GetEnvironmentVariable("MQTT_USERNAME");
        var password = Environment.GetEnvironmentVariable("MQTT_PASSWORD");

        if (username == null || password == null)
        {
            var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

            var secretProvider = config.Providers.First();
            secretProvider.TryGet("MQTT_USERNAME", out username);
            secretProvider.TryGet("MQTT_PASSWORD", out password);
        }

        return new MqttClientCredentials(username, Encoding.ASCII.GetBytes(password));
    }
}
