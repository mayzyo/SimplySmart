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
using SimplySmart.DeviceStates.Factories;
using SimplySmart.HouseStates.Factories;
using SimplySmart.Core.Abstractions;
using Quartz;
using SimplySmart.Frigate.Services;
using SimplySmart.Zwave.Stubs;
using SimplySmart.Homebridge.Stubs;
using SimplySmart.Frigate.Stubs;
using SimplySmart.Core.Stubs;

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
        services.AddSingleton<EventBusService>();
        services.AddHostedService<EventBusService>();
        services.AddQuartz();
        services.AddQuartzHostedService(opt =>
        {
            opt.WaitForJobsToComplete = true;
        });
        services.AddSingleton<IStateStore, RedisStateStore>();
        if(Environment.GetEnvironmentVariable("READ_ONLY") != "true") {
            services.AddScoped<IStateSyncService, StateSyncService>();
            services.AddTransient<IPassthroughEventSender, PassthroughEventSender>();
        } else {
            services.AddScoped<IStateSyncService, StateSyncServiceStub>();
            services.AddTransient<IPassthroughEventSender, PassthroughEventSenderStub>();
        }

        // Devices Module
        services.AddTransient<IFanFactory, FanFactory>();
        services.AddTransient<ILightSwitchFactory, LightSwitchFactory>();
        services.AddTransient<IGarageDoorFactory, GarageDoorFactory>();
        services.AddScoped<IFanService, FanService>();
        services.AddScoped<ILightSwitchService, LightSwitchService>();
        services.AddScoped<IGarageDoorService, GarageDoorService>();
        //services.AddScoped<IFobService, FobService>();

        // House Module
        services.AddTransient<IAutoLightFactory, AutoLightFactory>();
        services.AddTransient<IHouseSecurityFactory, HouseSecurityFactory>();
        services.AddTransient<IAreaOccupantFactory, AreaOccupantFactory>();
        services.AddScoped<IAreaOccupantService, AreaOccupantService>();
        services.AddScoped<IHouseService, HouseService>();

        // Zwave Module
        if(Environment.GetEnvironmentVariable("READ_ONLY") != "true") {
            services.AddTransient<IZwaveEventSender, ZwaveEventSender>();
        } else {
            services.AddTransient<IZwaveEventSender, ZwaveEventSenderStub>();
        }
        services.AddTransient<IBinarySwitchEventHandler, BinarySwitchEventHandler>();
        services.AddTransient<IMultiLevelSwitchEventHandler, MultiLevelSwitchEventHandler>();
        services.AddTransient<ICentralSceneEventHandler, CentralSceneEventHandler>();
        services.AddTransient<IMotionSensorEventHandler, MotionSensorEventHandler>();
        services.AddTransient<IAccessSensorEventHandler, AccessSensorEventHandler>();
        services.AddTransient<IElectricMeterEventHandler, ElectricMeterEventHandler>();
        services.AddScoped<IBinarySwitchService, BinarySwitchService>();
        services.AddScoped<IMultiLevelSwitchService, MultiLevelSwitchService>();
        services.AddScoped<IAccessSensorService, AccessSensorService>();

        // Frigate Module
        if (Environment.GetEnvironmentVariable("READ_ONLY") != "true") {
            services.AddTransient<IFrigateWebhookSender, FrigateWebhookSender>();
        } else {
            services.AddTransient<IFrigateWebhookSender, FrigateWebhookSenderStub>();
        }
        services.AddTransient<IFrigateEventHandler, FrigateEventHandler>();
        services.AddTransient<IPersonEventHandler, PersonEventHandler>();

        // Homebridge Module
        if(Environment.GetEnvironmentVariable("READ_ONLY") != "true") {
            services.AddTransient<IHomebridgeEventSender, HomebridgeEventSender>();
        } else {
            services.AddTransient<IHomebridgeEventSender, HomebridgeEventSenderStub>();
        }
        services.AddTransient<IFanEventHandler, FanEventHandler>();
        services.AddTransient<ILightSwitchEventHandler, LightSwitchEventHandler>();
        services.AddTransient<IDimmerLightSwitchEventHandler, DimmerLightSwitchEventHandler>();
        services.AddTransient<IGarageDoorOpenerEventHandler, GarageDoorOpenerEventHandler>();
        services.AddTransient<ISwitchEventHandler, SwitchEventHandler>();
        services.AddTransient<ISecurityEventHandler, SecurityEventHandler>();
        services.AddScoped<ISwitchService, SwitchService>();
    })
    .Build()
    .Run();