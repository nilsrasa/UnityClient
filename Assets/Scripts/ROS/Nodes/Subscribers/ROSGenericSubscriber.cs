using System;
using ROSBridgeLib;
using SimpleJSON;

/// <summary>
/// Generic subscriber that collects data received by ROS
/// </summary>
/// <typeparam name="T">Type of ROSBridgeMsg the subscriber should return when parsing</typeparam>
public class ROSGenericSubscriber<T> : ROSBridgeSubscriber where T : ROSBridgeMsg  {

    private bool _isRunning;
    private Func<JSONNode, T> _parseAction;

    /// <param name="messageType">MessageType to receive</param>
    /// <param name="parseAction">The parse function necessary to convert the received ROSBridgeMsg into the expected type T</param>
    public ROSGenericSubscriber(ROSBridgeWebSocketConnection rosConnection, string topicName, string messageType, Func<JSONNode, T> parseAction)
    {
        _parseAction = parseAction;
        StartAgent(rosConnection, topicName, messageType);
    }
    
    ///<summary>
    ///Stops advertising loop
    ///</summary>
    public void Stop()
    {
        _isRunning = false;
    }

    public override ROSBridgeMsg ParseMessage(JSONNode msg)
    {
        return _parseAction(msg);
    }
}
