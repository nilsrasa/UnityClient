using System;
using Messages;
using Ros_CSharp;
using UnityEngine;
using String = Messages.std_msgs.String;

public class ROSLocomotionWaypointState : ROSAgent
{
    private const string TOPIC = "/waypoint/state";
    public enum RobotWaypointState { RUNNING, STOP, PARK }

    private NodeHandle _nodeHandle;
    private Publisher<String> _publisher;
    private Subscriber<String> _subscriber;
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
            _publisher = _nodeHandle.advertise<String>(TOPIC, 1, false);
        else if (job == AgentJob.Subscriber)
            _subscriber = _nodeHandle.subscribe<String>(TOPIC, 1, ReceivedData);
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
        RobotWaypointState state = (RobotWaypointState) (int)data;
        String msg = new String(state.ToString());
        _publisher.publish(msg);
    }

    protected override void ReceivedData(IRosMessage data)
    {
        base.ReceivedData(data);
        String nav = (String) data;
        Debug.Log("ROSLocomotionWaypointState: Receieved data - " + nav);
    }

}
