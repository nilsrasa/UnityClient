using System;
using Messages;
using Messages.std_msgs;
using Ros_CSharp;
using UnityEngine;

public class ROSLocomotionLinearSpeed : ROSAgent
{
    private const string TOPIC = "/waypoint/max_linear_speed";

    private NodeHandle _nodeHandle;
    private Publisher<Float32> _publisher;
    private Subscriber<Float32> _subscriber;
    private bool _isRunning;
    private AgentJob _job;

    ///<summary>
    ///Starts advertising loop
    ///</summary>
    public override void StartAgent(AgentJob job )
    {
        base.StartAgent(job);
        if (_isRunning) return;
        _nodeHandle = new NodeHandle();
        if (job == AgentJob.Publisher)
            _publisher = _nodeHandle.advertise<Float32>(TOPIC, 1, false);
        else if (job == AgentJob.Subscriber)
            _subscriber = _nodeHandle.subscribe<Float32>(TOPIC, 1, ReceivedData);
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
        Float32 f = new Float32();
        f.data = (float) data;
        _publisher.publish(f);
    }

    protected override void ReceivedData(IRosMessage data)
    {
        Float32 param = (Float32) data;
        Debug.Log("ROSLocomotionDirect: Receieved data - " + param);
    }

}
