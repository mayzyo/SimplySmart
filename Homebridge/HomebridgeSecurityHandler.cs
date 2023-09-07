using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using SimplySmart.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Homebridge;

public interface IHomebridgeSecurityHandler
{
    Task HandleEvent(MqttApplicationMessageReceivedEventArgs e);

    Task HandleUpdateCurrent(HouseSecurityState state);
}

internal class HomebridgeSecurityHandler : IHomebridgeSecurityHandler
{
    private readonly ILogger<HomebridgeSecurityHandler> logger;
    private readonly IManagedMqttClient mqttClient;
    private readonly IHouseManager houseManager;

    public HomebridgeSecurityHandler(ILogger<HomebridgeSecurityHandler> logger, IManagedMqttClient mqttClient, IHouseManager houseManager)
    {
        this.logger = logger;
        this.mqttClient = mqttClient;
        this.houseManager = houseManager;
    }

    public async Task HandleUpdateCurrent(HouseSecurityState state)
    {
        await mqttClient.EnqueueAsync("homebridge/security/getCurrentState", ConvertState(state));
    }

    public async Task HandleEvent(MqttApplicationMessageReceivedEventArgs e)
    {
        var message = e.ApplicationMessage.ConvertPayloadToString();

        logger.LogInformation("House security triggered");
        var command = ConvertMessage(message);
        houseManager.Security.Trigger(command);
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

    private static string ConvertState(HouseSecurityState state)
    {
        switch (state)
        {
            case HouseSecurityState.OFF: return "D";
            case HouseSecurityState.NIGHT: return "NA";
            case HouseSecurityState.AWAY: return "AA";
            case HouseSecurityState.HOME: return "SA";
            default: throw new Exception("Undefined value received from statelss");
        }
    }
}
