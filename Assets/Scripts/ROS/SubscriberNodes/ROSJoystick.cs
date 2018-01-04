using System.Collections;
using System.Collections.Generic;
using Messages.geometry_msgs;
using Messages.std_msgs;
using Ros_CSharp;
using UnityEngine;

public class ROSJoystick : ROSAgent {

    private const string TOPIC = "/teleop_velocity_smoother/raw_cmd_vel";

    private NodeHandle _nodeHandle;
    private Subscriber<Twist> _subscriber;
    private Publisher<Twist> _publisher;
    private bool _isRunning;
    private AgentJob _job;

    ///<summary>
    ///Starts advertising loop
    /// <param name="job">Defines behaviour of agent</param>
    /// <param name="rosNamespace">Namespace the agent listens or writes to + topic</param>
    ///</summary>
    public override void StartAgent(AgentJob job, string rosNamespace) {
        base.StartAgent(job, rosNamespace);
        if (_isRunning) return;
        _nodeHandle = new NodeHandle();
        if (job == AgentJob.Subscriber)
            _subscriber = _nodeHandle.subscribe<Twist>(TOPIC, 1, ReceivedData);
        else if (job == AgentJob.Publisher)
            _publisher = _nodeHandle.advertise<Twist>(TOPIC, 1, false);
        _isRunning = true;
        _job = job;
        //Application.logMessageReceived += LogMessage;
    }

    public override void PublishData(object data) {
        if (_job != AgentJob.Publisher) return;
        Twist dataString = (Twist)data;
        _publisher.publish(dataString);
    }

    ///<summary>
    ///Stops advertising loop
    ///</summary>
    public void Stop() {
        if (!_isRunning) return;
        _nodeHandle.shutdown();
        _subscriber = null;
        _nodeHandle = null;

    }
}
