using System.Collections;
using System.Collections.Generic;
using Messages;
using Messages.geometry_msgs;
using Messages.std_msgs;
using Ros_CSharp;
using SimpleJSON;
using UnityEditor;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class ROSUltrasound : ROSAgent
{

    public delegate void OnDataReceived(SensorDataDTO message);
    public event OnDataReceived DataWasReceived;

    private const string TOPIC = "ultrasonic_data";

    private NodeHandle _nodeHandle;
    private Subscriber<String> _subscriber;
    private bool _isRunning;

    private void DataReceived(String data)
    {
        
        Debug.Log(data.data);
        var json = JSON.Parse(data.data);
        SensorData[] sdata = new SensorData[json.Count];
        int i = 0;

        foreach (KeyValuePair<string, JSONNode> pair in json.AsObject)
        {
            sdata[i] = new SensorData(pair.Key, pair.Value, Vector3.zero, Vector3.zero);
            i++;
        }
        
        SensorDataDTO dto = new SensorDataDTO(sdata);
        if (DataWasReceived != null)
            DataWasReceived(dto);

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
