using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using SimplySmart.Frigate;
using SimplySmart.Homebridge;
using SimplySmart.Nodemation;
using SimplySmart.Zwave;
using System.Text;

namespace SimplySmart.Core;

public class MqttService : IHostedService
{
    private readonly ILogger<MqttService> logger;
    private readonly IManagedMqttClient managedMqttClient;
    private readonly IServiceProvider serviceProvider;

    public MqttService(ILogger<MqttService> logger, IServiceProvider serviceProvider, IManagedMqttClient managedMqttClient)
    {
        this.logger = logger;
        this.serviceProvider = serviceProvider;
        this.managedMqttClient = managedMqttClient;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var topics = AssignTopicHandlers();

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

    private List<MqttTopicFilter> AssignTopicHandlers()
    {
        var topics = new List<MqttTopicFilter>
        {
            new MqttTopicFilterBuilder().WithTopic("frigate/events").Build(),
            new MqttTopicFilterBuilder().WithTopic("frigate/+/person").Build(),
            new MqttTopicFilterBuilder().WithTopic("nodemation/daylight").Build(),
            new MqttTopicFilterBuilder().WithTopic("homebridge/light_switch/#").Build(),
            new MqttTopicFilterBuilder().WithTopic("homebridge/switch/+/setOn").Build(),
            new MqttTopicFilterBuilder().WithTopic("homebridge/security/setTargetState").Build(),
            new MqttTopicFilterBuilder().WithTopic(IHomebridgeGarageDoorOpenerHandler.MQTT_TOPIC).Build(),
            new MqttTopicFilterBuilder().WithTopic(IHomebridgeHeaterCoolerHandler.MQTT_TOPIC).Build(),
            new MqttTopicFilterBuilder().WithTopic(IHomebridgeFanHandler.MQTT_TOPIC).Build(),
            new MqttTopicFilterBuilder().WithTopic(IZwaveBinarySwitchHandler.MQTT_TOPIC).Build(),
            new MqttTopicFilterBuilder().WithTopic(IZwaveMultiLevelSwitchHandler.MQTT_TOPIC).Build(),
            new MqttTopicFilterBuilder().WithTopic(IZwaveCentralSceneHandler.MQTT_TOPIC).Build(),
            new MqttTopicFilterBuilder().WithTopic(IZwaveNotificationHandler.MQTT_TOPIC).Build(),
        };
        managedMqttClient.ApplicationMessageReceivedAsync += CreateScopedMessageReceived;

        return topics;
    }

    private async Task CreateScopedMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        using var scope = serviceProvider.CreateScope();

        if (MqttTopicFilterComparer.Compare(topic, "frigate/events") == MqttTopicFilterCompareResult.IsMatch)
        {
            IFrigateEventHandler handler = scope.ServiceProvider.GetRequiredService<IFrigateEventHandler>();
            await handler.HandleEvent(e);
        }
        else if (MqttTopicFilterComparer.Compare(topic, "frigate/+/person") == MqttTopicFilterCompareResult.IsMatch)
        {
            IFrigateAreaHandler handler = scope.ServiceProvider.GetRequiredService<IFrigateAreaHandler>();
            await handler.HandleEvent(e);
        }
        else if (MqttTopicFilterComparer.Compare(topic, "nodemation/daylight") == MqttTopicFilterCompareResult.IsMatch)
        {
            INodemationDaylightHandler handler = scope.ServiceProvider.GetRequiredService<INodemationDaylightHandler>();
            await handler.HandleEvent(e);
        }
        else if (MqttTopicFilterComparer.Compare(topic, "homebridge/light_switch/#") == MqttTopicFilterCompareResult.IsMatch)
        {
            IHomebridgeLightSwitchHandler handler = scope.ServiceProvider.GetRequiredService<IHomebridgeLightSwitchHandler>();
            await handler.HandleEvent(e);
        }
        else if (MqttTopicFilterComparer.Compare(topic, "homebridge/switch/+/setOn") == MqttTopicFilterCompareResult.IsMatch)
        {
            IHomebridgeSwitchHandler handler = scope.ServiceProvider.GetRequiredService<IHomebridgeSwitchHandler>();
            await handler.HandleEvent(e);
        }
        else if (MqttTopicFilterComparer.Compare(topic, "homebridge/security/setTargetState") == MqttTopicFilterCompareResult.IsMatch)
        {
            IHomebridgeSecurityHandler handler = scope.ServiceProvider.GetRequiredService<IHomebridgeSecurityHandler>();
            await handler.HandleEvent(e);
        }
        else if (MqttTopicFilterComparer.Compare(topic, IHomebridgeGarageDoorOpenerHandler.MQTT_TOPIC) == MqttTopicFilterCompareResult.IsMatch)
        {
            IHomebridgeGarageDoorOpenerHandler handler = scope.ServiceProvider.GetRequiredService<IHomebridgeGarageDoorOpenerHandler>();
            await handler.HandleEvent(e);
        }
        else if (MqttTopicFilterComparer.Compare(topic, IHomebridgeHeaterCoolerHandler.MQTT_TOPIC) == MqttTopicFilterCompareResult.IsMatch)
        {
            IHomebridgeHeaterCoolerHandler handler = scope.ServiceProvider.GetRequiredService<IHomebridgeHeaterCoolerHandler>();
            await handler.HandleEvent(e);
        }
        else if (MqttTopicFilterComparer.Compare(topic, IHomebridgeFanHandler.MQTT_TOPIC) == MqttTopicFilterCompareResult.IsMatch)
        {
            IHomebridgeFanHandler handler = scope.ServiceProvider.GetRequiredService<IHomebridgeFanHandler>();
            await handler.HandleEvent(e);
        }
        else if (MqttTopicFilterComparer.Compare(topic, IZwaveBinarySwitchHandler.MQTT_TOPIC) == MqttTopicFilterCompareResult.IsMatch)
        {
            IZwaveBinarySwitchHandler handler = scope.ServiceProvider.GetRequiredService<IZwaveBinarySwitchHandler>();
            await handler.HandleEvent(e);
        }
        else if (MqttTopicFilterComparer.Compare(topic, IZwaveMultiLevelSwitchHandler.MQTT_TOPIC) == MqttTopicFilterCompareResult.IsMatch)
        {
            IZwaveMultiLevelSwitchHandler handler = scope.ServiceProvider.GetRequiredService<IZwaveMultiLevelSwitchHandler>();
            await handler.HandleEvent(e);
        }
        else if (MqttTopicFilterComparer.Compare(topic, IZwaveCentralSceneHandler.MQTT_TOPIC) == MqttTopicFilterCompareResult.IsMatch)
        {
            IZwaveCentralSceneHandler handler = scope.ServiceProvider.GetRequiredService<IZwaveCentralSceneHandler>();
            await handler.HandleEvent(e);
        }
        else if (MqttTopicFilterComparer.Compare(topic, IZwaveNotificationHandler.MQTT_TOPIC) == MqttTopicFilterCompareResult.IsMatch)
        {
            IZwaveNotificationHandler handler = scope.ServiceProvider.GetRequiredService<IZwaveNotificationHandler>();
            await handler.HandleEvent(e);
        }
    }

    private static MqttClientCredentials GetCredentials()
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
