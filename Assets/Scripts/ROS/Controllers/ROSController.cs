using System;
using Ros_CSharp;
using UnityEngine;
using XmlRpc_Wrapper;

public class ROSController : MonoBehaviour
{
    [SerializeField] protected string _robotNamespace;
    [SerializeField] protected string _clientNamespace;

    protected virtual void OnApplicationQuit()
    {
        if (ROS.ok || ROS.isStarted())
            StopROS();
    }

    public virtual void StartROS(string ros_master_uri)
    {
        if (!ros_master_uri.Contains("http://"))
            ros_master_uri = "http://" + ros_master_uri;
        ROS.ROS_MASTER_URI = ros_master_uri;
        StartROS();
    }

    public virtual void StartROS()
    {
        Debug.Log("---Starting ROS---");
        if (ROS.isStarted()) return;
        ROS.Init(new string[0], "VRClient");
        XmlRpcUtil.SetLogLevel(XmlRpcUtil.XMLRPC_LOG_LEVEL.ERROR);
    }

    public virtual void StopROS()
    {
        Debug.Log("---Stopping ROS---");
        ROS.shutdown();
        //ROS.waitForShutdown(); Do we need this? 
    }

    public virtual void Move(Vector2 movementCommand)
    {
        throw new NotImplementedException("Override this function");
    }
    
}
