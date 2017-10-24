using System;
using System.Collections;
using System.Collections.Generic;
using Messages;
using Ros_CSharp;
using UnityEngine;
using XmlRpc_Wrapper;
using String = Messages.std_msgs.String;

public class ROSController : MonoBehaviour
{
    public const string NAMESPACE_VRClient = "/vrclient";
    public const string NAMESPACE_ARLOBOT = "/arlobot";

    public static ROSController Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject();
                go.name = "ROSController";
                _instance = go.AddComponent<ROSController>();
            }
            return _instance;

        }
        private set
        {
            _instance = value;
        }
    }

    private static ROSController _instance;

    private ROSLocomotion _rosLocomotion;
    private ROSUltrasound _rosUltrasound;

    void OnApplicationQuit()
    {
        if (ROS.ok || ROS.isStarted())
            StopROS();
    }

    public void StartROS(string ros_master_uri)
    {
        if (!ros_master_uri.Contains("http://"))
            ros_master_uri = "http://" + ros_master_uri;
        ROS.ROS_MASTER_URI = ros_master_uri;
        StartROS();
    }

    public void StartROS()
    {
        Debug.Log("---Starting ROS---");
        if (ROS.isStarted()) return;
        ROS.Init(new string[0], "VRClient");
        XmlRpcUtil.SetLogLevel(XmlRpcUtil.XMLRPC_LOG_LEVEL.ERROR);
        _rosLocomotion = new ROSLocomotion();
        _rosLocomotion.StartAgent(ROSAgent.AgentJob.Publisher);
        _rosUltrasound = new ROSUltrasound();
        _rosUltrasound.StartAgent(ROSAgent.AgentJob.Subscriber);
    }

    public void StopROS()
    {
        Debug.Log("---Stopping ROS---");
        ROS.shutdown();
        //ROS.waitForShutdown(); Do we need this? 
    }

    public void Move(Vector2 movement)
    {
        _rosLocomotion.PublishData(movement);
    }

}
