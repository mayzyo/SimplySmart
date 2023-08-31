using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using SimplySmart.Core;
using SimplySmart.Frigate;
using SimplySmart.Nodemation;
using SimplySmart.States;
using SimplySmart.Zwave;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

Console.WriteLine("Simply Smart - Welcome");

Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
#if DEBUG
        logging.SetMinimumLevel(LogLevel.Debug);
#endif
    })
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddEnvironmentVariables();
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton(sp =>
        {
            var mqttFactory = new MqttFactory();
            return mqttFactory.CreateManagedMqttClient();
        });
        services.AddHostedService<MqttService>();

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        services.AddSingleton(deserializer);
        services.AddSingleton<ILightSwitchManager, LightSwitchManager>();
        services.AddSingleton<IAreaOccupantManager, AreaOccupantManager>();

        services.AddTransient<IFrigateEventHandler, FrigateEventHandler>();
        services.AddTransient<IFrigateAreaHandler, FrigateAreaHandler>();
        services.AddTransient<INodemationDaylightHandler, NodemationDaylightHandler>();
        services.AddTransient<IZwaveLightSwitchHandler, ZwaveLightSwitchHandler>();
    })
    .Build()
    .Run();