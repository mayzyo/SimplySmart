using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using SimplySmart.Core;
using SimplySmart.Frigate;
using SimplySmart.Homebridge;
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
        services.AddSingleton<IAccessPointManager, AccessPointManager>();
        services.AddSingleton<IApplianceManager, ApplianceManager>();
        services.AddSingleton<IFobManager, FobManager>();
        services.AddSingleton<IHouseManager, HouseManager>();

        services.AddTransient<IFrigateEventHandler, FrigateEventHandler>();
        services.AddTransient<IFrigateAreaHandler, FrigateAreaHandler>();
        services.AddTransient<INodemationDaylightHandler, NodemationDaylightHandler>();
        services.AddTransient<IZwaveBinarySwitchHandler, ZwaveBinarySwitchHandler>();
        services.AddTransient<IZwaveMultiLevelSwitchHandler, ZwaveMultiLevelSwitchHandler>();
        services.AddTransient<IZwaveCentralSceneHandler, ZwaveCentralSceneHandler>();
        services.AddTransient<IZwaveNotificationHandler, ZwaveNotificationHandler>();

        services.AddTransient<IHomebridgeLightSwitchHandler, HomebridgeLightSwitchHandler>();
        services.AddTransient<IHomebridgeSwitchHandler, HomebridgeSwitchHandler>();
        services.AddTransient<IHomebridgeSecurityHandler, HomebridgeSecurityHandler>();
        services.AddTransient<IHomebridgeGarageDoorOpenerHandler, HomebridgeGarageDoorOpenerHandler>();
        services.AddTransient<IHomebridgeHeaterCoolerHandler, HomebridgeHeaterCoolerHandler>();
        services.AddTransient<IHomebridgeFanHandler, HomebridgeFanHandler>();
    })
    .Build()
    .Run();