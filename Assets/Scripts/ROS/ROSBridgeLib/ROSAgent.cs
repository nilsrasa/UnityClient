using ROSBridgeLib;

/// <summary>
/// Base class of any ROSBridge subscriber or publisher
/// </summary>
public abstract class ROSAgent
{
    public enum AgentJob
    {
        Publisher,
        Subscriber
    }

    public string TopicName { get; protected set; }
    public string MessageType { get; protected set; }

    /// <summary>
    /// Stops agent and unsubscribes/unadvertises from its topic.
    /// </summary>
    public virtual void Stop()
    {
    }

    protected ROSBridgeWebSocketConnection ROSConnection;

    protected abstract void StartAgent(ROSBridgeWebSocketConnection rosConnection, string topicName,
        string messageType);
}