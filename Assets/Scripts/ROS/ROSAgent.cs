using UnityEngine;

/// <summary>
/// Base class of any Nodehandler agent
/// </summary>
public class ROSAgent
{
    public enum AgentJob { None, Subscriber, Publisher}

    public delegate void DataReceived(ROSAgent sender, ROSBridgeMsg message);
    public event DataReceived DataWasReceived;

    public virtual void StartAgent(AgentJob job)
    {
        Debug.Log("Starting Agent " + this.GetType() +" with job " + job);
    }

    protected virtual void ReceivedData(ROSBridgeMsg data)
    {
        if (DataWasReceived != null)
            DataWasReceived(this, data);
    }

    public virtual void PublishData(object data)
    {
        
    }

    protected virtual void LogMessage(string condition, string stack, LogType type) {
        Debug.Log(condition);
        Debug.Log(stack);
        Debug.Log(type);
    }
}
