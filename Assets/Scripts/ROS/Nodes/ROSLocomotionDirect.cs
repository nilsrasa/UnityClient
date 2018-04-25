using System;
using ROSBridgeLib;
using ROSBridgeLib.geometry_msgs;
using UnityEngine;

public class ROSLocomotionDirect : ROSAgent
{
    private ROSGenericSubscriber<TwistMsg> _subscriber;
    private ROSGenericPublisher _publisher;

    public delegate void DataReceived(ROSBridgeMsg msg);
    public event ROSLocomotionControlParams.DataReceived OnDataReceived;

    public ROSLocomotionDirect(AgentJob job, ROSBridgeWebSocketConnection rosConnection, string topicName)
    {
        if (job == AgentJob.Publisher)
        {
            _publisher = new ROSGenericPublisher(rosConnection, topicName, TwistMsg.GetMessageType());
            rosConnection.AddPublisher(_publisher);
        }
        else if (job == AgentJob.Subscriber)
        {
            _subscriber = new ROSGenericSubscriber<TwistMsg>(rosConnection, topicName, TwistMsg.GetMessageType(), (msg) => new TwistMsg(msg));
            _subscriber.OnDataReceived += (data) =>
            {
                if (OnDataReceived != null)
                    OnDataReceived(data);
            };
            rosConnection.AddSubscriber(_subscriber);
        }
    }

    protected override void StartAgent(ROSBridgeWebSocketConnection rosConnection, string topicName, string messageType)
    {
        //throw new NotImplementedException();
    }

    /// <param name="linear">Linear movement speed in meter/second</param>
    /// <param name="angular">Angular rotational speed in meter/second</param>
    public void PublishData(float linear, float angular)
    {
        if (_publisher == null) return;

        TwistMsg twist = new TwistMsg(new Vector3Msg(linear, 0, 0), 
            new Vector3Msg(0, 0, angular));
        _publisher.PublishData(twist);
    }
    
}
