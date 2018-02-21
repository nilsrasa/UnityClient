using System;
using System.Collections.Generic;
using ROSBridgeLib;
using UnityEngine;

public class ROSController : MonoBehaviour
{
    public delegate void RosStarted();
    public event RosStarted OnRosStarted;

    protected bool _robotModelInitialised;
    protected ROSBridgeWebSocketConnection _rosBridge;
    protected RobotConfigFile _robotConfig;

    protected virtual void StartROS()
    {
        Debug.Log("Starting Robot");
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

    public virtual void InitialiseRobot(ROSBridgeWebSocketConnection rosBridge, RobotConfigFile robotConfig)
    {
        _rosBridge = rosBridge;
        _robotConfig = robotConfig;
        StartROS();
        /*
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
        */
    }

    private void GenerateRobot(string robotDescription) {
        //_robot = RobotUrdfUtility.GenerateRobotGameObjectFromDescription(robotDescription, _alwaysGenerateRobot).transform;
    }
}
