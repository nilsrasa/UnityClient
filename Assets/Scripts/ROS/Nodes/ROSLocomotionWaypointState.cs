using System;
using Messages;
using Messages.geometry_msgs;
using Messages.sensor_msgs;
using Ros_CSharp;
using UnityEngine;

public class ROSLocomotionWaypoint : ROSAgent
{
    private const string TOPIC = "/waypoint";

    private NodeHandle _nodeHandle;
    private Publisher<NavSatFix> _publisher;
    private Subscriber<NavSatFix> _subscriber;
    private bool _isRunning;
    private AgentJob _job;

    ///<summary>
    ///Starts advertising loop
    ///</summary>
    public override void StartAgent(AgentJob job)
    {
        base.StartAgent(job);
        if (_isRunning) return;
        _nodeHandle = new NodeHandle();
        if (job == AgentJob.Publisher)
            _publisher = _nodeHandle.advertise<NavSatFix>(TOPIC, 1, false);
        else if (job == AgentJob.Subscriber)
            _subscriber = _nodeHandle.subscribe<NavSatFix>(TOPIC, 1, ReceivedData);
        _job = job;
        _isRunning = true;
    }

    ///<summary>
    ///Stops advertising loop
    ///</summary>
    public void Stop() {
        if (!_isRunning) return;
        _nodeHandle.shutdown();
        _publisher = null;
        _nodeHandle = null;
    }

    public override void PublishData(object data)
    {
        if (_job != AgentJob.Publisher) return;
        GeoPointWGS84 navToPoint = (GeoPointWGS84) data;
        NavSatFix nav = new NavSatFix
        {
            latitude = navToPoint.latitude,
            longitude = navToPoint.longitude,
            altitude = navToPoint.altitude,
        };

        _publisher.publish(nav);
    }

    protected override void ReceivedData(IRosMessage data)
    {
        NavSatFix nav = (NavSatFix) data;
        Debug.Log("ROSLocomotionWaypoint: Receieved data - " + nav);
    }

}
