using MQTTnet.Client;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Tests.Utils;

public class MockBuilder
{
    public static MqttApplicationMessageReceivedEventArgs CreateMockEventArgs(string topic, string payload)
    {
        // Create a new MqttApplicationMessage
        var applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(payload))
            .WithRetainFlag()
            .Build();

        // Create a new MqttPublishPacket
        var publishPacket = new MqttPublishPacket
        {
            Topic = topic,
            QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce,
            Retain = true
        };

        // Create a mock acknowledgeHandler
        Func<MqttApplicationMessageReceivedEventArgs, CancellationToken, Task> acknowledgeHandler = (args, token) => Task.CompletedTask;

        // Create a new MqttApplicationMessageReceivedEventArgs
        return new MqttApplicationMessageReceivedEventArgs(
            "testClientId", // clientId
            applicationMessage, // applicationMessage
            publishPacket, // publishPacket
            acknowledgeHandler // acknowledgeHandler
        );
    }
}
