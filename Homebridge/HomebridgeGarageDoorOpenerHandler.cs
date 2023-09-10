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

public interface IHomebridgeGarageDoorOpenerHandler
{
    public const string MQTT_TOPIC = "homebridge/garage_door_opener/+/+/+/+/targetDoorState";
    Task HandleEvent(MqttApplicationMessageReceivedEventArgs e);

    Task HandleOn(string name);

    Task HandleOff(string name);

    Task HandleMoving(string name);

    Task HandleStopped(string name);
}

internal class HomebridgeGarageDoorOpenerHandler : IHomebridgeGarageDoorOpenerHandler
{
    private readonly ILogger<HomebridgeGarageDoorOpenerHandler> logger;
    private readonly IManagedMqttClient mqttClient;
    private readonly IAccessPointManager accessPointManager;

    public HomebridgeGarageDoorOpenerHandler(ILogger<HomebridgeGarageDoorOpenerHandler> logger, IManagedMqttClient mqttClient, IAccessPointManager accessPointManager)
    {
        this.logger = logger;
        this.mqttClient = mqttClient;
        this.accessPointManager = accessPointManager;
    }

    public async Task HandleOn(string name)
    {
        await mqttClient.EnqueueAsync($"homebridge/garage_door_opener/{name}/targetDoorState/set", "O");
    }

    public async Task HandleOff(string name)
    {
        await mqttClient.EnqueueAsync($"homebridge/garage_door_opener/{name}/targetDoorState/set", "C");
    }

    public async Task HandleMoving(string name)
    {
        await mqttClient.EnqueueAsync($"homebridge/garage_door_opener/{name}/doorMoving/set", "true");
    }

    public async Task HandleStopped(string name)
    {
        await mqttClient.EnqueueAsync($"homebridge/garage_door_opener/{name}/doorMoving/set", "false");
    }

    public async Task HandleEvent(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("homebridge/garage_door_opener/", "").Replace("/targetDoorState", "");
        var message = e.ApplicationMessage.ConvertPayloadToString();

        await UpdateState(name, message);
    }

    private async Task UpdateState(string name, string message)
    {
        var command = ConvertMessage(message);
        var garageDoor = (IGarageDoor)accessPointManager[name];
        await garageDoor.Trigger(command);
    }

    private static GarageDoorCommand ConvertMessage(string message)
    {
        switch (message)
        {
            case "O": return GarageDoorCommand.OPEN;
            case "C": return GarageDoorCommand.CLOSE;
            default: throw new Exception("Undefined value received from homebridge garage door opener");
        }
    }
}
