using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using SimplySmart.States;
using SimplySmart.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SimplySmart.Zwave;
// DEPRECATED
public interface IZwaveLightSwitchHandler
{
    Task HandleEvent(MqttApplicationMessageReceivedEventArgs e);
    Task HandleOn(string triggerUri);
    Task HandleOn(string triggerUri, ushort brightness);
    Task HandleOff(string triggerUri);
}

internal class ZwaveLightSwitchHandler : IZwaveLightSwitchHandler
{
    private readonly ILogger<ZwaveLightSwitchHandler> logger;
    private readonly ILightSwitchManager lightSwitchManager;
    private readonly IManagedMqttClient mqttClient;

    public ZwaveLightSwitchHandler(ILogger<ZwaveLightSwitchHandler> logger, IManagedMqttClient mqttClient, ILightSwitchManager lightSwitchManager)
    {
        this.logger = logger;
        this.lightSwitchManager = lightSwitchManager;
        this.mqttClient = mqttClient;
    }

    public async Task HandleOff(string triggerUri)
    {
        var epoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var payload = JsonSerializer.Serialize(new BinarySwitch { value = false, time = epoch });
        await mqttClient.EnqueueAsync($"zwave/{triggerUri}/targetValue/set", payload);
    }

    public async Task HandleOn(string triggerUri)
    {
        var epoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var payload = JsonSerializer.Serialize(new BinarySwitch { value = true, time = epoch });
        await mqttClient.EnqueueAsync($"zwave/{triggerUri}/targetValue/set", payload);
    }

    public async Task HandleOn(string triggerUri, ushort brightness)
    {
        var epoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var payload = JsonSerializer.Serialize(new MultilevelSwitch { value = (ushort)(brightness == 100 ? 99 : brightness), time = epoch });
        await mqttClient.EnqueueAsync($"zwave/{triggerUri}/targetValue/set", payload);
    }

    public async Task HandleEvent(MqttApplicationMessageReceivedEventArgs e)
    {
        var name = e.ApplicationMessage.Topic.Replace("zwave/", "").Replace("/currentValue", "");
        if(!lightSwitchManager.Exists(name))
        {
            return;
        }

        var message = e.ApplicationMessage.ConvertPayloadToString();

        if (MqttTopicFilterComparer.Compare(e.ApplicationMessage.Topic, "zwave/+/+/37/+/currentValue") == MqttTopicFilterCompareResult.IsMatch)
        {
            var binarySwitch = DeserialiseMessage<BinarySwitch>(message);
            if (binarySwitch == default)
            {
                logger.LogError("message JSON was empty");
                return;
            }

            ChangeLightSwitchState(name, binarySwitch.value);
        }
        else
        {
            var dimmer = (IDimmerLightSwitch)lightSwitchManager[name];
            var dimmerSwitch = DeserialiseMessage<MultilevelSwitch>(message);
            if (dimmerSwitch == default)
            {
                logger.LogError("message JSON was empty");
                return;
            }

            dimmerSwitch.value = (ushort)(dimmerSwitch.value == 99 ? 100 : dimmerSwitch.value);

            if (dimmerSwitch.value == 0)
            {
                dimmer.Trigger(LightSwitchCommand.MANUAL_OFF, BroadcastSource.ZWAVE);
            }
            else if(dimmer.IsInState(LightSwitchState.OFF) || dimmerSwitch.value != dimmer.Brightness)
            {
                dimmer.Trigger(LightSwitchCommand.MANUAL_ON, dimmerSwitch.value, BroadcastSource.ZWAVE);
            }
        }
    }

    private void ChangeLightSwitchState(string name, bool isOn)
    {
        if (isOn)
        {
            lightSwitchManager[name].Trigger(LightSwitchCommand.MANUAL_ON, BroadcastSource.ZWAVE);
        }
        else
        {
            lightSwitchManager[name].Trigger(LightSwitchCommand.MANUAL_OFF, BroadcastSource.ZWAVE);
        }
    }

    private T? DeserialiseMessage<T>(string message)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(message);
        }
        catch
        {
            logger.LogError("message not in JSON format.");
        }

        return default;
    }
}
