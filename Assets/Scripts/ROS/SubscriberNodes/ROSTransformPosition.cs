using Messages;
using Messages.sensor_msgs;
using Ros_CSharp;
using UnityEngine;

/// <summary>
/// Ultrasound agent that sends or receives ultrasound data
/// </summary>
public class ROSTransformPosition : ROSAgent
{
    private const string TOPIC = "/robot_gps_pose";

    private NodeHandle _nodeHandle;
    private Subscriber<NavSatFix> _subscriber;
    private Publisher<NavSatFix> _publisher;
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
        if(job == AgentJob.Subscriber)
            _subscriber = _nodeHandle.subscribe<NavSatFix>(TOPIC, 1, ReceivedData);
        else if (job == AgentJob.Publisher)
            _publisher = _nodeHandle.advertise<NavSatFix>(TOPIC, 1, false);
        _isRunning = true;
        _job = job;
        //Application.logMessageReceived += LogMessage;
    }

    public override void PublishData(object data)
    {
        if (_job != AgentJob.Publisher) return;
        NavSatFix dataString = (NavSatFix) data;
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
