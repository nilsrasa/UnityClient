using System;
using Messages;
using Messages.geometry_msgs;
using Ros_CSharp;
using UnityEngine;
using String = Messages.std_msgs.String;

public class ROSLocomotionSpeedParams : ROSAgent
{
    private const string TOPIC = "/waypoint/control_parameters";

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

    public void PublishData(float linearSpeed, float angularSpeed)
    {
        PublishData(string.Format("{0},{1}", linearSpeed, angularSpeed));
    }

    public override void PublishData(object data)
    {
        if (_job != AgentJob.Publisher) return;
        String msg = new String((string)data);
        _publisher.publish(msg);
        
    }

    protected override void ReceivedData(IRosMessage data)
    {
        String param = (String) data;
        Debug.Log("ROSLocomotionDirect: Receieved data - " + param);
    }

}
