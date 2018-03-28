using ROSBridgeLib;

public class testpubpose : ROSBridgePublisher
{
    public testpubpose(ROSBridgeWebSocketConnection rosConnection, string topicName, string messageType)
    {
        StartAgent(rosConnection, topicName, messageType);
    }
}