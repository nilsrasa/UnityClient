using System;
using System.Collections.Generic;
using Assets.Scripts;
using Messages;
using Messages.sensor_msgs;
using Messages.std_msgs;
using UnityEngine;
using String = Messages.std_msgs.String;

public class ArlobotROSController : ROSController {

    [SerializeField] private string _ROS_MASTER_URI = "127.0.0.1:11311";
    [SerializeField] private float _waypointDistanceThreshhold = 0.1f;
    [SerializeField] private List<Transform> _waypoints;

    private enum RobotLocomotionState { FORWARDING, TURNING, STOP }
    private enum RobotLocomotionType { WAYPOINT, DIRECT, WAYPOINT_ROUTE }

    public static ArlobotROSController Instance { get; private set; }

    private ROSLocomotionDirect _rosLocomotionDirect;
    private ROSLocomotionWaypoint _rosLocomotionWaypoint;
    private ROSLocomotionWaypointState _rosLocomotionWaypointState;
    private ROSUltrasound _rosUltrasound;
    private ROSTransformPosition _rosTransformPosition;
    private ROSTransformHeading _rosTransformHeading;
    private ROSLocomotionState _rosLocomotionState;
    private RobotLocomotionState _currentRobotLocomotionState;
    private RobotLocomotionType _currenLocomotionType = RobotLocomotionType.DIRECT;

    private bool _hasPositionDataToConsume;
    private Vector3 _positionDataToConsume;
    private bool _hasHeadingDataToConsume;
    private float _headingDataToConsume;

    //Navigation
    private Vector3 _currentWaypoint;
    private int _waypointIndex = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartROS(_ROS_MASTER_URI);
    }

    void Update()
    {
        //Direct control of robot
        float linear = 0;
        float angular = 0;
        if (Input.GetKey(KeyCode.UpArrow))
        {
            linear = 0.5f;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            linear = -0.5f;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            angular = -0.5f;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            angular = 0.5f;
        }
        if (linear == 0 && angular == 0 && _currentRobotLocomotionState != RobotLocomotionState.STOP && _currenLocomotionType == RobotLocomotionType.DIRECT)
        {
            StopRobot();
        }
        else if (linear != 0 || angular != 0)
        {
            if (_currenLocomotionType != RobotLocomotionType.DIRECT)
                _rosLocomotionWaypointState.PublishData(ROSLocomotionWaypointState.RobotWaypointState.STOP);
            _rosLocomotionDirect.PublishData(new Vector2(linear, angular));
            _currenLocomotionType = RobotLocomotionType.DIRECT;
        }

        //Navigation to waypoint
        if (_currenLocomotionType == RobotLocomotionType.WAYPOINT && _currentRobotLocomotionState != RobotLocomotionState.STOP)
        {
            Debug.Log(Vector3.Distance(transform.position, _currentWaypoint));
            //Waypoint reached
            if (Vector3.Distance(transform.position, _currentWaypoint) < _waypointDistanceThreshhold)
            {
                if (_currenLocomotionType == RobotLocomotionType.WAYPOINT_ROUTE)
                {
                    if (_waypointIndex < _waypoints.Count - 1)
                        MoveToNextWaypoint();
                }
                else
                {
                    StopRobot();
                }
            }
        }

        if (_hasHeadingDataToConsume)
        {
            transform.rotation = Quaternion.Euler(0, _headingDataToConsume, 0);
            _hasHeadingDataToConsume = false;
        }
        if (_hasPositionDataToConsume)
        {
            transform.position = _positionDataToConsume;
            _hasPositionDataToConsume = false;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
             StartWaypointRoute();
        }
    }

    private void StartWaypointRoute()
    {
        _currenLocomotionType = RobotLocomotionType.WAYPOINT_ROUTE;
        _currentWaypoint = _waypoints[_waypointIndex].position;
        Move(_currentWaypoint);
    }

    private void MoveToNextWaypoint()
    {
        _currentWaypoint = _waypoints[_waypointIndex++].position;
        Move(_currentWaypoint);
    }

    public void StopRobot()
    {
        _rosLocomotionWaypointState.PublishData(ROSLocomotionWaypointState.RobotWaypointState.PARK);
        //_rosLocomotionDirect.PublishData(Vector2.zero);
    }

    public override void StartROS(string uri) {
        base.StartROS(uri);
        _rosLocomotionDirect = new ROSLocomotionDirect();
        _rosLocomotionDirect.StartAgent(ROSAgent.AgentJob.Publisher, _clientNamespace);
        _rosLocomotionWaypoint = new ROSLocomotionWaypoint();
        _rosLocomotionWaypoint.StartAgent(ROSAgent.AgentJob.Publisher, _clientNamespace);
        _rosLocomotionWaypointState = new ROSLocomotionWaypointState();
        _rosLocomotionWaypointState.StartAgent(ROSAgent.AgentJob.Publisher, _clientNamespace);
        //_rosUltrasound = new ROSUltrasound();
       //_rosUltrasound.StartAgent(ROSAgent.AgentJob.Subscriber, _clientNamespace);
        _rosTransformPosition = new ROSTransformPosition();
        _rosTransformPosition.StartAgent(ROSAgent.AgentJob.Subscriber, _clientNamespace);
        _rosTransformPosition.DataWasReceived += ReceivedPositionUpdate;
        _rosTransformHeading = new ROSTransformHeading();
        _rosTransformHeading.StartAgent(ROSAgent.AgentJob.Subscriber, _clientNamespace);
        _rosTransformHeading.DataWasReceived += ReceivedHeadingUpdate;
        _rosLocomotionState = new ROSLocomotionState();
        _rosLocomotionState.StartAgent(ROSAgent.AgentJob.Subscriber, _clientNamespace);
        _rosLocomotionState.DataWasReceived += ReceivedLocomotionStateUpdata;
    }

    public void Move(Vector3 position)
    {
        GeoPointWGS84 point = position.ToMercator().ToWGS84();
        _rosLocomotionWaypoint.PublishData(point);
        _currentWaypoint = position;
        if (_currenLocomotionType != RobotLocomotionType.WAYPOINT_ROUTE)
            _currenLocomotionType = RobotLocomotionType.WAYPOINT;
        _rosLocomotionWaypointState.PublishData(ROSLocomotionWaypointState.RobotWaypointState.RUNNING);
    }

    public void ReceivedPositionUpdate(ROSAgent sender, IRosMessage position)
    {
        //In WGS84
        NavSatFix nav = (NavSatFix) position;
        GeoPointWGS84 geoPoint = new GeoPointWGS84
        {
            latitude = nav.latitude,
            longitude = nav.longitude,
            altitude = nav.altitude,
        };
        if (GeoUtils.MercatorOriginSet)
        {
            _positionDataToConsume = geoPoint.ToMercator().ToUnity() + new Vector3(0, 10, 0);

            _hasPositionDataToConsume = true;
            
        }
    }

    public void ReceivedHeadingUpdate(ROSAgent sender, IRosMessage heading)
    {
        Float32 f = (Float32) heading;
        _headingDataToConsume = f.data;
        _hasHeadingDataToConsume = true;
    }

    public void ReceivedLocomotionStateUpdata(ROSAgent sender, IRosMessage state)
    {
        String s = (String) state;
        _currentRobotLocomotionState = (RobotLocomotionState) Enum.Parse(typeof(RobotLocomotionState), s.data);
    }
}