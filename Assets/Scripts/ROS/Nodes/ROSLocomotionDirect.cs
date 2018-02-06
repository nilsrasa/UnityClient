using System;
using Messages;
using Messages.geometry_msgs;
using Ros_CSharp;
using UnityEngine;

public class ROSLocomotionDirect : ROSAgent
{
    private const string TOPIC = "/cmd_vel";

    private NodeHandle _nodeHandle;
    private Publisher<Twist> _publisher;
    private Subscriber<Twist> _subscriber;
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
            _publisher = _nodeHandle.advertise<Twist>(TOPIC, 1, false);
        else if (job == AgentJob.Subscriber)
            _subscriber = _nodeHandle.subscribe<Twist>(TOPIC, 1, ReceivedData);
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
        Vector2 vector = (Vector2) data;
        Twist twist = new Twist
        {
            angular = new Messages.geometry_msgs.Vector3
            {
                x = 0,
                y = 0,
                z = vector.x
            },
            linear = new Messages.geometry_msgs.Vector3 {
                x = vector.y,
                y = 0,
                z = 0
            }
        };
        _publisher.publish(twist);
    }
    
}
