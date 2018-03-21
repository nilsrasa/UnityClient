using System;
using System.Collections.Generic;
using ROSBridgeLib;
using UnityEngine;

public abstract class ROSController : MonoBehaviour
{
    public delegate void RosStarted(ROSBridgeWebSocketConnection rosBridge);
    public event RosStarted OnRosStarted;

    protected bool _robotModelInitialised;
    protected ROSBridgeWebSocketConnection _rosBridge;
    protected RobotConfigFile _robotConfig;

    protected virtual void OnApplicationQuit()
    {
        StopROS();
    }

    protected abstract void StartROS();

    protected virtual void StopROS() { }

    public abstract void MoveDirect(Vector2 movementCommand);

    public abstract void MoveToPoint(GeoPointWGS84 point);

    public abstract void MovePath(List<GeoPointWGS84> waypoints);

    public abstract void PausePath();

    public abstract void ResumePath();

    public abstract void StopRobot();

    public virtual void InitialiseRobot(ROSBridgeWebSocketConnection rosBridge, RobotConfigFile robotConfig)
    {
        _rosBridge = rosBridge;
        _robotConfig = robotConfig;
        StartROS();
        if (OnRosStarted != null)
            OnRosStarted(rosBridge);

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
