using System.Collections;
using System.Collections.Generic;
using Ros_CSharp;
using UnityEngine;
using XmlRpc_Wrapper;

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
        
    public void StartROS()
    {
        Debug.Log("---Starting ROS---");
        if (ROS.isStarted()) return;
        ROS.Init(new string[0], "VRClient");
        XmlRpcUtil.SetLogLevel(XmlRpcUtil.XMLRPC_LOG_LEVEL.ERROR);
        _rosLocomotion = new ROSLocomotion();
        _rosLocomotion.Start(0);
        _rosUltrasound = new ROSUltrasound();
        _rosUltrasound.StartSubscriber();
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
