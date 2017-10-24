using Messages;
using UnityEngine;

public class ROSAgent
{
    public enum AgentJob { None, Subscriber, Publisher}

    public delegate void DataReceived(ROSAgent sender, IRosMessage message);
    public event DataReceived DataWasReceived;

    public virtual void StartAgent(AgentJob job)
    {
        Debug.Log("Starting Agent " + this.GetType() +" with job " + job);
    }

    protected virtual void ReceivedData(IRosMessage data)
    {
        if (DataWasReceived != null)
            DataWasReceived(this, data);
    }

    public virtual void PublishData(object data)
    {
        
    }
}
