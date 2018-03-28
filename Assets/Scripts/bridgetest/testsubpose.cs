using ROSBridgeLib;
using ROSBridgeLib.geometry_msgs;
using ROSBridgeLib.std_msgs;
using SimpleJSON;

public class testsubpose : ROSBridgeSubscriber
{
    public static StringMsg DataReceived = null;

    public testsubpose(ROSBridgeWebSocketConnection rosConnection, string topicName, string messageType)
    {
        StartAgent(rosConnection, topicName, messageType);
    }

    public static StringMsg TryGetReceivedMessage()
    {
        StringMsg data = DataReceived;
        DataReceived = null;
        return data;
    }

    public override ROSBridgeMsg ParseMessage(JSONNode msg)
    {
        return new StringMsg(msg);
    }
}