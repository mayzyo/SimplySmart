using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using SimpleFrigateSorter;

Console.WriteLine("Simple Frigate Sorter - Welcome");

Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
#if DEBUG
        logging.SetMinimumLevel(LogLevel.Debug);
#endif
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton(sp =>
        {
            var mqttFactory = new MqttFactory();
            return mqttFactory.CreateManagedMqttClient();
        });
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddHostedService<MqttClientService>();
        services.AddTransient<IFrigateEventHandler, FrigateEventHandler>();
        services.AddTransient<IFrigateConfigHandler, FrigateConfigHandler>();
    })
    .Build()
    .Run();