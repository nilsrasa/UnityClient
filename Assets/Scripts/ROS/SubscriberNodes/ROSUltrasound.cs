using System;
using System.Collections;
using System.Collections.Generic;
using Messages;
using Messages.geometry_msgs;
using Ros_CSharp;
using SimpleJSON;
using UnityEngine;
using String = Messages.std_msgs.String;
using Vector3 = UnityEngine.Vector3;

public class ROSUltrasound : ROSAgent
{
    private const string TOPIC = "ultrasonic_data";

    private NodeHandle _nodeHandle;
    private Subscriber<String> _subscriber;
    private Publisher<String> _publisher;
    private bool _isRunning;
    private AgentJob _job;
    
    ///<summary>
    ///Starts advertising loop
    ///</summary>
    public override void StartAgent(AgentJob job, string rosNamespace) {
        base.StartAgent(job, rosNamespace);
        if (_isRunning) return;
        _nodeHandle = new NodeHandle();
        if(job == AgentJob.Subscriber)
            _subscriber = _nodeHandle.subscribe<String>(rosNamespace + TOPIC, 1, ReceivedData);
        else if (job == AgentJob.Publisher)
            _publisher = _nodeHandle.advertise<String>(rosNamespace + TOPIC, 1, false);
        _isRunning = true;
        _job = job;
        Application.logMessageReceived += test;
    }

    public override void PublishData(object data)
    {
        if (_job != AgentJob.Publisher) return;
        String dataString = (String) data;
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

    private void test(string condition, string stack, LogType type)
    {
        Debug.Log(condition);
        Debug.Log(stack);
        Debug.Log(type);
    }
}
