using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using SimplySmart.HouseStates.Features;
using SimplySmart.HouseStates.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Homebridge.EventHandling;

public interface ISecurityEventHandler
{
    const string MQTT_TOPIC = "homebridge/security/setTargetState";
    Task Handle(MqttApplicationMessageReceivedEventArgs e);
}

internal class SecurityEventHandler(ILogger<SecurityEventHandler> logger, IHouseService houseService) : ISecurityEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var message = e.ApplicationMessage.ConvertPayloadToString();

        logger.LogInformation("House security triggered");
        var command = ConvertMessage(message);
        houseService.Security.Trigger(command);
    }

    private static HouseSecurityCommand ConvertMessage(string message)
    {
        switch (message)
        {
            case "D": return HouseSecurityCommand.OFF;
            case "NA": return HouseSecurityCommand.NIGHT;
            case "AA": return HouseSecurityCommand.AWAY;
            case "SA": return HouseSecurityCommand.HOME;
            default: throw new Exception("Undefined value received from homebridge security");
        }
    }
}
