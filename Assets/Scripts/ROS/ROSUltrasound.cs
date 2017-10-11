using System.Collections;
using System.Collections.Generic;
using Messages.geometry_msgs;
using Messages.std_msgs;
using Ros_CSharp;
using UnityEngine;

public class ROSUltrasound : MonoBehaviour {

    private const string TOPIC = "ultrasonic_data";

    private NodeHandle _nodeHandle;
    private Subscriber<String> _subscriber;
    private Twist _dataToSend;
    private float _messageInterval;
    private bool _isRunning;

    private void DataReceived(String data)
    {
        Debug.Log(data.data);
    }

    ///<summary>
    ///Starts advertising loop
    ///</summary>
    ///<param name="messageInterval">Minimum time (ms) between messages sent</param>
    public void StartSubscriber() {
        if (_isRunning) return;
        _nodeHandle = new NodeHandle();
        _subscriber = _nodeHandle.subscribe<String>(TOPIC, 1, DataReceived);

        _isRunning = true;
        Application.logMessageReceived += test;
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
