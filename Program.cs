using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using SimplySmart.DeviceStates.Services;
using SimplySmart.Homebridge.EventHandling;
using SimplySmart.Homebridge.Services;
using SimplySmart.Zwave.Services;
using SimplySmart.Core.Extensions;
using System;
using System.Resources;
using SimplySmart.HouseStates.Services;
using SimplySmart.Frigate.EventHandling;
using SimplySmart.Core.Models;
using SimplySmart.Zwave.EventHandling;
using SimplySmart.Core.Services;

// Reference: https://code-maze.com/dotnet-factory-pattern-dependency-injection/

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
        var path = Environment.GetEnvironmentVariable("CONFIG_FILE_PATH") ?? throw new Exception("Config file missing!");
        builder.AddEnvironmentVariables().AddYamlFile(path);
    })
    .ConfigureServices(services =>
    {
        services.AddOptions<ApplicationConfig>().BindConfiguration("CONFIG_YAML");

        services.AddSingleton(sp =>
        {
            var mqttFactory = new MqttFactory();
            return mqttFactory.CreateManagedMqttClient();
        });
        services.AddHostedService<EventBusService>();
        services.AddSingleton<IStateStorageService, StateStorageService>();

        services.AddTransient<IFrigateEventHandler, FrigateEventHandler>();
        services.AddTransient<IPersonEventHandler, PersonEventHandler>();

        services.AddTransient<IBinarySwitchEventHandler, BinarySwitchEventHandler>();
        services.AddTransient<IMultiLevelSwitchEventHandler, MultiLevelSwitchEventHandler>();
        services.AddTransient<ICentralSceneEventHandler, CentralSceneEventHandler>();
        services.AddTransient<INotificationEventHandler, NotificationEventHandler>();
        services.AddTransient<IZwaveEventSender, ZwaveEventSender>();

        services.AddTransient<ILightSwitchEventHandler, LightSwitchEventHandler>();
        services.AddTransient<ISwitchEventHandler, SwitchEventHandler>();
        services.AddTransient<ISecurityEventHandler, SecurityEventHandler>();
        services.AddTransient<IGarageDoorOpenerEventHandler, GarageDoorOpenerEventHandler>();
        services.AddTransient<IFanEventHandler, FanEventHandler>();
        services.AddTransient<IHomebridgeEventSender, HomebridgeEventSender>();

        services.AddTransient<ILightSwitchService, LightSwitchService>();
        services.AddTransient<IAccessPointService, AccessPointService>();
        services.AddTransient<IApplianceService, ApplianceService>();
        services.AddTransient<IFobService, FobService>();

        services.AddTransient<IAreaOccupantService, AreaOccupantService>();
        services.AddTransient<IHouseService, HouseService>();
    })
    .Build()
    .Run();