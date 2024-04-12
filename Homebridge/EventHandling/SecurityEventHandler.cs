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

internal class SecurityEventHandler(ILogger<ISecurityEventHandler> logger, IHouseService houseService) : ISecurityEventHandler
{
    public async Task Handle(MqttApplicationMessageReceivedEventArgs e)
    {
        var message = e.ApplicationMessage.ConvertPayloadToString();

        logger.LogInformation("House security triggered");
        var command = ConvertMessage(message);
        await houseService.Security.Trigger(command);
    }

    private static HouseSecurityCommand ConvertMessage(string message)
    {
        switch (message)
        {
            case "D": return HouseSecurityCommand.SET_OFF;
            case "NA": return HouseSecurityCommand.SET_NIGHT;
            case "AA": return HouseSecurityCommand.SET_AWAY;
            case "SA": return HouseSecurityCommand.SET_HOME;
            default: throw new Exception("Undefined value received from homebridge security");
        }
    }
}
