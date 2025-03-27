using Microsoft.Extensions.Logging;
using MQTTnet.Client;
using SimplySmart.Core.Extensions;
using SimplySmart.HouseStates.Services;
using SimplySmart.Zwave.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimplySmart.Zwave.EventHandling;

public interface IMotionSensorEventHandler
{
    const string MQTT_TOPIC = "zwave/+/+/113/0/Home_Security/Motion_sensor_status";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

internal class MotionSensorEventHandler(ILogger<IMotionSensorEventHandler> logger, IAreaOccupantService areaOccupantService) : IMotionSensorEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("zwave/", "");
        if (!e.ApplicationMessage.DeserialiseMessage(out Payload? payload) || payload == default)
        {
            logger.LogError("message not in JSON format.");
            return;
        }

        areaOccupantService[name]?.SetMoving(payload.Value != 0);
    }
}
