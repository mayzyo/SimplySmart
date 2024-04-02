using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using SimplySmart.Frigate.EventHandling;
using SimplySmart.Homebridge.EventHandling;
using SimplySmart.Zwave.EventHandling;
using System.Text;

namespace SimplySmart.Core.Services;

public class EventBusService(ILogger<EventBusService> logger, IServiceProvider serviceProvider, IManagedMqttClient managedMqttClient) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var topics = AssignTopicControllers();

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

    List<MqttTopicFilter> AssignTopicControllers()
    {
        var topics = new List<MqttTopicFilter>
        {
            new MqttTopicFilterBuilder().WithTopic(IFrigateEventHandler.MQTT_TOPIC).Build(),
            new MqttTopicFilterBuilder().WithTopic(IPersonEventHandler.MQTT_TOPIC).Build(),
            new MqttTopicFilterBuilder().WithTopic(ILightSwitchEventHandler.MQTT_TOPIC).Build(),
            new MqttTopicFilterBuilder().WithTopic(ISwitchEventHandler.MQTT_TOPIC).Build(),
            new MqttTopicFilterBuilder().WithTopic(ISecurityEventHandler.MQTT_TOPIC).Build(),
            new MqttTopicFilterBuilder().WithTopic(IGarageDoorOpenerEventHandler.MQTT_TOPIC).Build(),
            new MqttTopicFilterBuilder().WithTopic(IFanEventHandler.MQTT_TOPIC).Build(),
            new MqttTopicFilterBuilder().WithTopic(IBinarySwitchEventHandler.MQTT_TOPIC).Build(),
            new MqttTopicFilterBuilder().WithTopic(IMultiLevelSwitchEventHandler.MQTT_TOPIC).Build(),
            new MqttTopicFilterBuilder().WithTopic(ICentralSceneEventHandler.MQTT_TOPIC).Build(),
            new MqttTopicFilterBuilder().WithTopic(INotificationEventHandler.MQTT_TOPIC).Build(),
        };
        managedMqttClient.ApplicationMessageReceivedAsync += CreateScopedMessageReceived;

        return topics;
    }

    async Task CreateScopedMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        using var scope = serviceProvider.CreateScope();

        if (MqttTopicFilterComparer.Compare(topic, IFrigateEventHandler.MQTT_TOPIC) == MqttTopicFilterCompareResult.IsMatch)
        {
            IFrigateEventHandler handler = scope.ServiceProvider.GetRequiredService<IFrigateEventHandler>();
            await handler.Handle(e);
        }
        else if (MqttTopicFilterComparer.Compare(topic, IPersonEventHandler.MQTT_TOPIC) == MqttTopicFilterCompareResult.IsMatch)
        {
            IPersonEventHandler handler = scope.ServiceProvider.GetRequiredService<IPersonEventHandler>();
            await handler.Handle(e);
        }
        else if (MqttTopicFilterComparer.Compare(topic, ILightSwitchEventHandler.MQTT_TOPIC) == MqttTopicFilterCompareResult.IsMatch)
        {
            ILightSwitchEventHandler handler = scope.ServiceProvider.GetRequiredService<ILightSwitchEventHandler>();
            await handler.Handle(e);
        }
        else if (MqttTopicFilterComparer.Compare(topic, ISwitchEventHandler.MQTT_TOPIC) == MqttTopicFilterCompareResult.IsMatch)
        {
            ISwitchEventHandler handler = scope.ServiceProvider.GetRequiredService<ISwitchEventHandler>();
            await handler.Handle(e);
        }
        else if (MqttTopicFilterComparer.Compare(topic, ISecurityEventHandler.MQTT_TOPIC) == MqttTopicFilterCompareResult.IsMatch)
        {
            ISecurityEventHandler handler = scope.ServiceProvider.GetRequiredService<ISecurityEventHandler>();
            await handler.Handle(e);
        }
        else if (MqttTopicFilterComparer.Compare(topic, IGarageDoorOpenerEventHandler.MQTT_TOPIC) == MqttTopicFilterCompareResult.IsMatch)
        {
            IGarageDoorOpenerEventHandler handler = scope.ServiceProvider.GetRequiredService<IGarageDoorOpenerEventHandler>();
            await handler.Handle(e);
        }
        else if (MqttTopicFilterComparer.Compare(topic, IFanEventHandler.MQTT_TOPIC) == MqttTopicFilterCompareResult.IsMatch)
        {
            IFanEventHandler handler = scope.ServiceProvider.GetRequiredService<IFanEventHandler>();
            await handler.Handle(e);
        }
        else if (MqttTopicFilterComparer.Compare(topic, IBinarySwitchEventHandler.MQTT_TOPIC) == MqttTopicFilterCompareResult.IsMatch)
        {
            IBinarySwitchEventHandler handler = scope.ServiceProvider.GetRequiredService<IBinarySwitchEventHandler>();
            await handler.Handle(e);
        }
        else if (MqttTopicFilterComparer.Compare(topic, IMultiLevelSwitchEventHandler.MQTT_TOPIC) == MqttTopicFilterCompareResult.IsMatch)
        {
            IMultiLevelSwitchEventHandler handler = scope.ServiceProvider.GetRequiredService<IMultiLevelSwitchEventHandler>();
            await handler.Handle(e);
        }
        else if (MqttTopicFilterComparer.Compare(topic, ICentralSceneEventHandler.MQTT_TOPIC) == MqttTopicFilterCompareResult.IsMatch)
        {
            ICentralSceneEventHandler handler = scope.ServiceProvider.GetRequiredService<ICentralSceneEventHandler>();
            await handler.Handle(e);
        }
        else if (MqttTopicFilterComparer.Compare(topic, INotificationEventHandler.MQTT_TOPIC) == MqttTopicFilterCompareResult.IsMatch)
        {
            INotificationEventHandler handler = scope.ServiceProvider.GetRequiredService<INotificationEventHandler>();
            await handler.HandleEvent(e);
        }
    }

    static MqttClientCredentials GetCredentials()
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
