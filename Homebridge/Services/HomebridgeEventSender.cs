using MQTTnet.Extensions.ManagedClient;
using SimplySmart.Core.Abstractions;
using SimplySmart.HouseStates.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Homebridge.Services;

public interface IHomebridgeEventSender
{
    Task FanOn(string name);
    Task FanOff(string name);
    Task GarageDoorOpenerOn(string name);
    Task GarageDoorOpenerOff(string name);
    Task GarageDoorOpenerMoving(string name);
    Task GarageDoorOpenerStopped(string name);
    Task LightSwitchOff(string triggerUri);
    Task LightSwitchOn(string triggerUri);
    Task DimmerBrightness(string triggerUri, ushort brightness);
    Task SwitchOn(string triggerUri);
    Task SwitchOff(string triggerUri);
    Task HouseSecurityUpdate(HouseSecurityState state);
}

internal class HomebridgeEventSender(IManagedMqttClient mqttClient) : IHomebridgeEventSender
{
    public async Task FanOn(string triggerUri)
    {
        await mqttClient.EnqueueAsync($"homebridge/fan/{triggerUri}/getOn", "true");
    }

    public async Task FanOff(string triggerUri)
    {
        await mqttClient.EnqueueAsync($"homebridge/fan/{triggerUri}/getOn", "false");
    }

    public async Task GarageDoorOpenerOn(string triggerUri)
    {
        await mqttClient.EnqueueAsync($"homebridge/garage_door_opener/{triggerUri}/getTargetDoorState", "O");
    }

    public async Task GarageDoorOpenerOff(string triggerUri)
    {
        await mqttClient.EnqueueAsync($"homebridge/garage_door_opener/{triggerUri}/getTargetDoorState", "C");
    }

    public async Task GarageDoorOpenerMoving(string triggerUri)
    {
        await mqttClient.EnqueueAsync($"homebridge/garage_door_opener/{triggerUri}/getDoorMoving", "true");
    }

    public async Task GarageDoorOpenerStopped(string triggerUri)
    {
        await mqttClient.EnqueueAsync($"homebridge/garage_door_opener/{triggerUri}/getDoorMoving", "false");
    }

    public async Task LightSwitchOn(string triggerUri)
    {
        await mqttClient.EnqueueAsync($"homebridge/light_switch/{triggerUri}/getOn", "true");
    }

    public async Task LightSwitchOff(string triggerUri)
    {
        await mqttClient.EnqueueAsync($"homebridge/light_switch/{triggerUri}/getOn", "false");
    }

    public async Task DimmerBrightness(string triggerUri, ushort brightness)
    {
        await mqttClient.EnqueueAsync($"homebridge/light_switch/{triggerUri}/getBrightness", brightness.ToString());
    }

    public async Task SwitchOn(string triggerUri)
    {
        await mqttClient.EnqueueAsync($"homebridge/switch/{triggerUri}/getOn", "true");
    }

    public async Task SwitchOff(string triggerUri)
    {
        await mqttClient.EnqueueAsync($"homebridge/switch/{triggerUri}/getOn", "false");
    }

    public async Task HouseSecurityUpdate(HouseSecurityState state)
    {
        await mqttClient.EnqueueAsync("homebridge/security/getCurrentState", ConvertState(state));
    }

    protected static string ConvertState(HouseSecurityState state)
    {
        return state switch
        {
            HouseSecurityState.OFF => "D",
            HouseSecurityState.NIGHT => "NA",
            HouseSecurityState.AWAY => "AA",
            HouseSecurityState.HOME => "SA",
            _ => throw new Exception("Undefined value received from statelss"),
        };
    }
}
