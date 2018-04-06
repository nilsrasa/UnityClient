using System;
using ROSBridgeLib;
using ROSBridgeLib.sensor_msgs;

public class ROSLocomotionWaypoint : ROSAgent
{
    private ROSGenericSubscriber<NavSatFixMsg> _subscriber;
    private ROSGenericPublisher _publisher;

    public delegate void DataReceived(ROSBridgeMsg msg);
    public event DataReceived OnDataReceived;

    public ROSLocomotionWaypoint(AgentJob job, ROSBridgeWebSocketConnection rosConnection, string topicName)
    {
        if (job == AgentJob.Publisher)
        {
            _publisher = new ROSGenericPublisher(rosConnection, topicName, NavSatFixMsg.GetMessageType());
            rosConnection.AddPublisher(_publisher);
        }
        else if (job == AgentJob.Subscriber)
        {
            _subscriber = new ROSGenericSubscriber<NavSatFixMsg>(rosConnection, topicName, NavSatFixMsg.GetMessageType(), (msg) => new NavSatFixMsg(msg));
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

    /// <param name="data">X: Angular speed in meter/s, Y: Linear speed in meter/s</param>
    public void PublishData(GeoPointWGS84 data)
    {
        if (_publisher == null) return;

        NavSatFixMsg navSatFix = new NavSatFixMsg(data.latitude, data.longitude, data.altitude);
        _publisher.PublishData(navSatFix);
    }

}
