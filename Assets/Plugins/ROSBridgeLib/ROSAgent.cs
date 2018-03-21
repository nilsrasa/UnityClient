using ROSBridgeLib;
using UnityEngine;

/// <summary>
/// Base class of any ROSBridge subscriber or publisher
/// </summary>
public abstract class ROSAgent
{
    public enum AgentJob { Publisher, Subscriber}
    public string TopicName { get; protected set; }
    public string MessageType { get; protected set; }
    protected ROSBridgeWebSocketConnection ROSConnection;

    protected abstract void StartAgent(ROSBridgeWebSocketConnection rosConnection, string topicName, string messageType);

}
