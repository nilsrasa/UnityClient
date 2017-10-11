using System.Collections;
using System.Collections.Generic;
using Ros_CSharp;
using UnityEngine;
using XmlRpc_Wrapper;

public class ROSController : MonoBehaviour
{
    public const string NAMESPACE_VRClient = "/vrclient";
    public const string NAMESPACE_ARLOBOT = "/arlobot";

    public static ROSController Instance { get; private set; }

    private ROSLocomotion _rosLocomotion;
    private ROSUltrasound _rosUltrasound;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartROS();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
            _rosLocomotion.PublishData(Vector2.up);
        if (Input.GetKeyDown(KeyCode.DownArrow))
            _rosLocomotion.PublishData(Vector2.down);
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            _rosLocomotion.PublishData(Vector2.left);
        if (Input.GetKeyDown(KeyCode.RightArrow))
            _rosLocomotion.PublishData(Vector2.right);
    }

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
        //_rosUltrasound = new ROSUltrasound();
        //_rosUltrasound.StartSubscriber();
    }

    public void StopROS()
    {
        Debug.Log("---Stopping ROS---");
        ROS.shutdown();
        //ROS.waitForShutdown(); Do we need this? 
    }
}
