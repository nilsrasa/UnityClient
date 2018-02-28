using System;
using System.Collections.Generic;
using Ros_CSharp;
using UnityEngine;
using XmlRpc_Wrapper;

public class ROSController : MonoBehaviour
{
    public delegate void RosStarted();
    public event RosStarted OnRosStarted;

    protected bool _robotModelInitialised;
    protected RobotConfigFile _robotConfigFile;

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
        OnRosStarted?.Invoke();
    }

    public virtual void StopROS()
    {
        Debug.Log("---Stopping ROS---");
        ROS.shutdown();
        ROS.waitForShutdown(); 
    }

    public virtual void StartRobot(RobotConfigFile robotConfig)
    {
        _robotConfigFile = robotConfig;
        StartROS(robotConfig.RosMasterUri);
    }

    public virtual void MoveDirect(Vector2 movementCommand)
    {
        throw new NotImplementedException("Override this function");
    }

    public virtual void MoveToPoint(GeoPointWGS84 point) 
    {
        throw new NotImplementedException("Override this function");
    }

    public virtual void MovePath(List<GeoPointWGS84> waypoints) 
    {
        throw new NotImplementedException("Override this function");
    }

    public virtual void PausePath() 
    {
        throw new NotImplementedException("Override this function");
    }

    public virtual void ResumePath() 
    {
        throw new NotImplementedException("Override this function");
    }

    public virtual void StopRobot() 
    {
        throw new NotImplementedException("Override this function");
    }

    public virtual void OverridePositionAndOrientation(Vector3 newPosition, Quaternion newOrientation)
    {
        throw new NotImplementedException("Override this function");
    }

    protected virtual void InitialiseRobot() {
        if (Param.has("robot_description"))
        {
            string robotDescription = "";
            Debug.Log("Generating robot from robot description");
            Param.get("robot_description", ref robotDescription);
            GenerateRobot(robotDescription);
        }
        else
            Debug.Log("---No robot description available - could not automatically generate robot---");
        _robotModelInitialised = true;
    }

    private void GenerateRobot(string robotDescription) {
        //_robot = RobotUrdfUtility.GenerateRobotGameObjectFromDescription(robotDescription, _alwaysGenerateRobot).transform;
    }
}
