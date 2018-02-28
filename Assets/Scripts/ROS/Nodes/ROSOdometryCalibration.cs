using System.Collections;
using System.Collections.Generic;
using Messages.nav_msgs;
using Messages.std_msgs;
using Ros_CSharp;
using UnityEngine;

//TODO: Should be consolidated with ROSOdometry class and not use seperate class just to change topic name
public class ROSOdometryCalibration : ROSAgent {

    private const string TOPIC = "/odo_calib_pose";

    private NodeHandle _nodeHandle;
    private Subscriber<Odometry> _subscriber;
    private Publisher<Odometry> _publisher;
    private bool _isRunning;
    private AgentJob _job;

    ///<summary>
    ///Starts advertising loop
    /// <param name="job">Defines behaviour of agent</param>
    /// <param name="rosNamespace">Namespace the agent listens or writes to + topic</param>
    ///</summary>
    public override void StartAgent(AgentJob job)
    {
        base.StartAgent(job);
        if (_isRunning) return;
        _nodeHandle = new NodeHandle();
        if (job == AgentJob.Subscriber)
            _subscriber = _nodeHandle.subscribe<Odometry>(TOPIC, 1, ReceivedData);
        else if (job == AgentJob.Publisher)
            _publisher = _nodeHandle.advertise<Odometry>(TOPIC, 1, false);
        _isRunning = true;
        _job = job;
        //Application.logMessageReceived += LogMessage;
    }

    public override void PublishData(object data)
    {
        if (_job != AgentJob.Publisher) return;
        Odometry odometry = (Odometry)data;
        _publisher.publish(odometry);
    }

    ///<summary>
    ///Stops advertising loop
    ///</summary>
    public void Stop()
    {
        if (!_isRunning) return;
        _nodeHandle.shutdown();
        _subscriber = null;
        _nodeHandle = null;

    }
}
