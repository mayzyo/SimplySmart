using MQTTnet.Extensions.ManagedClient;
using SimplySmart.HouseStates.Services;
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
    Task LightSwitchOn(string triggerUri, ushort brightness);
    Task SwitchOn(string triggerUri);
    Task SwitchOff(string triggerUri);
    Task HouseSecurityUpdate(HouseSecurityState state);
}

internal class HomebridgeEventSender(IManagedMqttClient mqttClient) : IHomebridgeEventSender
{
    public async Task FanOn(string triggerUri)
    {
        await mqttClient.EnqueueAsync($"homebridge/fan/{triggerUri}/on/set", "true");
    }

    public async Task FanOff(string triggerUri)
    {
        await mqttClient.EnqueueAsync($"homebridge/fan/{triggerUri}/on/set", "false");
    }

    public async Task GarageDoorOpenerOn(string triggerUri)
    {
        await mqttClient.EnqueueAsync($"homebridge/garage_door_opener/{triggerUri}/targetDoorState/set", "O");
    }

    public async Task GarageDoorOpenerOff(string triggerUri)
    {
        await mqttClient.EnqueueAsync($"homebridge/garage_door_opener/{triggerUri}/targetDoorState/set", "C");
    }

    public async Task GarageDoorOpenerMoving(string triggerUri)
    {
        await mqttClient.EnqueueAsync($"homebridge/garage_door_opener/{triggerUri}/doorMoving/set", "true");
    }

    public async Task GarageDoorOpenerStopped(string triggerUri)
    {
        await mqttClient.EnqueueAsync($"homebridge/garage_door_opener/{triggerUri}/doorMoving/set", "false");
    }

    public async Task LightSwitchOff(string triggerUri)
    {
        await mqttClient.EnqueueAsync($"homebridge/light_switch/{triggerUri}/set", "false");
    }

    public async Task LightSwitchOn(string triggerUri)
    {
        await mqttClient.EnqueueAsync($"homebridge/light_switch/{triggerUri}/set", "true");
    }

    public async Task LightSwitchOn(string triggerUri, ushort brightness)
    {
        await mqttClient.EnqueueAsync($"homebridge/light_switch/{triggerUri}/brightness/set", brightness.ToString());
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
