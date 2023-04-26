using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using SimpleFrigateSorter;

Console.WriteLine("Simple Frigate Sorter - Welcome");

Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton(sp =>
        {
            var mqttFactory = new MqttFactory();
            return mqttFactory.CreateManagedMqttClient();
        });
        services.AddHostedService<MqttClientService>();
        services.AddTransient<IFrigateEventHandler, FrigateEventHandler>();
    })
    .Build()
    .Run();