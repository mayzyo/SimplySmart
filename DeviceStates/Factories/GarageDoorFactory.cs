using Microsoft.Extensions.Logging;
using Quartz;
using SimplySmart.Core.Abstractions;
using SimplySmart.Core.Models;
using SimplySmart.DeviceStates.Devices;
using SimplySmart.Frigate.Services;
using SimplySmart.Homebridge.Services;
using SimplySmart.Zwave.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace SimplySmart.DeviceStates.Factories;
public interface IGarageDoorFactory
{
    IGarageDoor CreateGarageDoor(SmartImplant config);
}

internal class GarageDoorFactory(
    ILogger<IGarageDoor> logger,
    IStateStore stateStore,
    ISchedulerFactory schedulerFactory,
    IHomebridgeEventSender homebridgeEventSender,
    IZwaveEventSender zwaveEventSender,
    IFrigateWebhookSender frigateWebhookSender
) : IGarageDoorFactory
{
    public IGarageDoor CreateGarageDoor(SmartImplant config) => 
        new GarageDoor(logger, stateStore, schedulerFactory, homebridgeEventSender, zwaveEventSender, frigateWebhookSender, config.Name, config.CloseDetect, config.OpenDetect)
            .Connect();
}
