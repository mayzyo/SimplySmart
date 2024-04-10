using MQTTnet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimplySmart.Core.Extensions;

public static class MqttApplicationMessageExtensions
{
    public static bool DeserialiseMessage<T>(this MqttApplicationMessage applicationMessage, out T? json, out string message)
    {
        message = applicationMessage.ConvertPayloadToString();

        try
        {
            json = JsonSerializer.Deserialize<T>(message);
            return true;
        }
        catch
        {
            json = default;
            return false;
        }
    }

    public static bool DeserialiseMessage<T>(this MqttApplicationMessage applicationMessage, out T? json)
    {
        return applicationMessage.DeserialiseMessage(out json, out _);
    }
}
