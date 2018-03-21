using System;
using ROSBridgeLib;
using ROSBridgeLib.std_msgs;

public class ROSLocomotionWaypointState : ROSAgent
{
    public enum RobotWaypointState { RUNNING, STOP, PARK }

    private ROSGenericSubscriber<StringMsg> _subscriber;
    private ROSGenericPublisher _publisher;

    public delegate void DataReceived(ROSBridgeMsg msg);
    public event ROSLocomotionControlParams.DataReceived OnDataReceived;

    public ROSLocomotionWaypointState(AgentJob job, ROSBridgeWebSocketConnection rosConnection, string topicName)
    {
        if (job == AgentJob.Publisher)
        {
            _publisher = new ROSGenericPublisher(rosConnection, topicName, StringMsg.GetMessageType());
            rosConnection.AddPublisher(_publisher);
        }
        else if (job == AgentJob.Subscriber)
        {
            _subscriber = new ROSGenericSubscriber<StringMsg>(rosConnection, topicName, StringMsg.GetMessageType(), (msg) => new StringMsg(msg));
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
        throw new NotImplementedException();
    }

    public void PublishData(RobotWaypointState data)
    {
        PublishData(data.ToString());
    }

    public void PublishData(string data)
    {
        if (_publisher == null) return;

        StringMsg state = new StringMsg(data);
        _publisher.PublishData(state);
    }

}
