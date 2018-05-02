using System;
using ROSBridgeLib;
using ROSBridgeLib.std_msgs;

public class ROSLocomotionControlParams : ROSAgent
{
    private ROSGenericSubscriber<StringMsg> _subscriber;
    private ROSGenericPublisher _publisher;

    public delegate void DataReceived(ROSBridgeMsg msg);

    public event DataReceived OnDataReceived;

    public ROSLocomotionControlParams(AgentJob job, ROSBridgeWebSocketConnection rosConnection, string topicName)
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

    public void PublishData(float rho, float roll, float pitch, float yaw)
    {
        if (_publisher != null)
            _publisher.PublishData(new StringMsg(string.Format("{0}, {1}, {2}, {3}", rho, roll, pitch, yaw)));
    }
}