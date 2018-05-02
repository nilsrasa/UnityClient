using ROSBridgeLib;

/// <summary>
/// Generic subscriber that collects data received by ROS
/// </summary>
/// <typeparam name="T">Type of ROSBridgeMsg the subscriber should return when parsing</typeparam>
public class ROSGenericPublisher : ROSBridgePublisher
{
    private bool _isRunning;

    /// <param name="messageType">MessageType to receive</param>
    /// <param name="parseAction">The parse function necessary to convert the received ROSBridgeMsg into the expected type T</param>
    public ROSGenericPublisher(ROSBridgeWebSocketConnection rosConnection, string topicName, string messageType)
    {
        StartAgent(rosConnection, topicName, messageType);
    }
}