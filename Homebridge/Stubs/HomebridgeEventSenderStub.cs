using MQTTnet.Extensions.ManagedClient;
using SimplySmart.Homebridge.Services;
using SimplySmart.HouseStates.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Homebridge.Stubs;

internal class HomebridgeEventSenderStub(IManagedMqttClient mqttClient) : HomebridgeEventSender(mqttClient), IHomebridgeEventSender
{
    public async new Task FanOn(string triggerUri)
    {
        Console.WriteLine($"Fan On - {triggerUri}");
        await Task.CompletedTask;
    }

    public async new Task FanOff(string triggerUri)
    {
        Console.WriteLine($"Fan Off - {triggerUri}");
        await Task.CompletedTask;
    }

    public async new Task GarageDoorOpenerOn(string triggerUri)
    {
        Console.WriteLine($"Garage Door Opener On - {triggerUri}");
        await Task.CompletedTask;
    }

    public async new Task GarageDoorOpenerOff(string triggerUri)
    {
        Console.WriteLine($"Garage Door Opener Off - {triggerUri}");
        await Task.CompletedTask;
    }

    public async new Task GarageDoorOpenerMoving(string triggerUri)
    {
        Console.WriteLine($"Garage Door Opener Moving - {triggerUri}");
        await Task.CompletedTask;
    }

    public async new Task GarageDoorOpenerStopped(string triggerUri)
    {
        Console.WriteLine($"Garage Door Opener Stopped - {triggerUri}");
        await Task.CompletedTask;
    }

    public async new Task LightSwitchOn(string triggerUri)
    {
        Console.WriteLine($"Light Switch On - {triggerUri}");
        await Task.CompletedTask;
    }

    public async new Task LightSwitchOff(string triggerUri)
    {
        Console.WriteLine($"Light Switch Off - {triggerUri}");
        await Task.CompletedTask;
    }

    public async new Task DimmerBrightness(string triggerUri, ushort brightness)
    {
        Console.WriteLine($"Dimmer Brightness - {triggerUri}, Brightness: {brightness}");
        await Task.CompletedTask;
    }

    public async new Task SwitchOn(string triggerUri)
    {
        Console.WriteLine($"Switch On - {triggerUri}");
        await Task.CompletedTask;
    }

    public async new Task SwitchOff(string triggerUri)
    {
        Console.WriteLine($"Switch Off - {triggerUri}");
        await Task.CompletedTask;
    }

    public async new Task HouseSecurityUpdate(HouseSecurityState state)
    {
        Console.WriteLine($"House Security Update - State: {ConvertState(state)}");
        await Task.CompletedTask;
    }
}