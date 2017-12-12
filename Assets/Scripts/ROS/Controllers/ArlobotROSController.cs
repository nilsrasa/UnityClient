using System;
using System.Collections;
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
    [SerializeField] private List<GeoPointWGS84> _waypoints;
    [SerializeField] private float _maxLinearSpeed = 3;
    [SerializeField] private float _linearSpeedParam = 3;
    [SerializeField] private float _angularSpeedParam = 1;
    [SerializeField] private int _waypointStartIndex = 0;

    private enum RobotLocomotionState { MOVING, STOPPED }
    private enum RobotLocomotionType { WAYPOINT, DIRECT, WAYPOINT_ROUTE }

    public static ArlobotROSController Instance { get; private set; }

    private ROSLocomotionDirect _rosLocomotionDirect;
    private ROSLocomotionWaypoint _rosLocomotionWaypoint;
    private ROSLocomotionWaypointState _rosLocomotionWaypointState;
    private ROSUltrasound _rosUltrasound;
    private ROSTransformPosition _rosTransformPosition;
    private ROSTransformHeading _rosTransformHeading;
    private ROSLocomotionState _rosLocomotionState;
    private ROSLocomotionLinearSpeed _rosLocomotionLinear;
    private ROSLocomotionSpeedParams _rosLocomotionSpeedParams;
    private RobotLocomotionState _currentRobotLocomotionState;
    private RobotLocomotionType _currenLocomotionType = RobotLocomotionType.DIRECT;

    private bool _hasPositionDataToConsume;
    private Vector3 _positionDataToConsume;
    private bool _hasHeadingDataToConsume;
    private float _headingDataToConsume;
    private float _oldMaxLinearSpeed;
    private float _oldLinearSpeedParam;
    private float _oldAngularSpeedParam;

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
            linear = 0.25f;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            linear = -0.25f;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            angular = 1f;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            angular = -1f;
        }
        if (linear == 0 && angular == 0 && _currentRobotLocomotionState != RobotLocomotionState.STOPPED && _currenLocomotionType == RobotLocomotionType.DIRECT)
        {
            StopRobot();
        }
        else if (linear != 0 || angular != 0)
        {
            if (_currenLocomotionType != RobotLocomotionType.DIRECT)
                _rosLocomotionWaypointState.PublishData(ROSLocomotionWaypointState.RobotWaypointState.STOP);
            _rosLocomotionDirect.PublishData(new Vector2(angular, linear));
            _currenLocomotionType = RobotLocomotionType.DIRECT;
            _currentRobotLocomotionState = RobotLocomotionState.MOVING;
        }

        //Navigation to waypoint
        if (_currenLocomotionType != RobotLocomotionType.DIRECT && _currentRobotLocomotionState != RobotLocomotionState.STOPPED)
        {
            //Debug.Log(Vector3.Distance(transform.position, _currentWaypoint));
            //Waypoint reached
            if (Vector3.Distance(transform.position, _currentWaypoint) < _waypointDistanceThreshhold)
            {
                if (_currenLocomotionType == RobotLocomotionType.WAYPOINT_ROUTE)
                {
                    if (_waypointIndex < _waypoints.Count - 1)
                        MoveToNextWaypoint();
                    else
                    {
                        StopRobot();
                    }
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
        if (_maxLinearSpeed != _oldMaxLinearSpeed)
        {
            _oldMaxLinearSpeed = _maxLinearSpeed;
            _rosLocomotionLinear.PublishData(_maxLinearSpeed);
        }
        if (_angularSpeedParam != _oldAngularSpeedParam || _linearSpeedParam != _oldLinearSpeedParam)
        {
            _oldAngularSpeedParam = _angularSpeedParam;
            _oldLinearSpeedParam = _linearSpeedParam;

            _rosLocomotionSpeedParams.PublishData(_linearSpeedParam, _angularSpeedParam);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
             StartWaypointRoute();
        }
    }

    private void StartWaypointRoute()
    {
        _waypointIndex = _waypointStartIndex;
        _currenLocomotionType = RobotLocomotionType.WAYPOINT_ROUTE;
        _currentWaypoint = new Vector3(0, 10, 0) + _waypoints[_waypointIndex].ToMercator().ToUnity();
        Move(_currentWaypoint);
    }

    private void MoveToNextWaypoint()
    {
        _waypointIndex++;
        _currentWaypoint = new Vector3(0, 10, 0) + _waypoints[_waypointIndex].ToMercator().ToUnity();
        Move(_currentWaypoint);
    }

    public void StopRobot()
    {
        _currentRobotLocomotionState = RobotLocomotionState.STOPPED;
        _rosLocomotionWaypointState.PublishData(ROSLocomotionWaypointState.RobotWaypointState.STOP);
        _rosLocomotionDirect.PublishData(Vector2.zero);
    }

    public override void StartROS(string uri) {
        base.StartROS(uri);
        _rosLocomotionDirect = new ROSLocomotionDirect();
        _rosLocomotionDirect.StartAgent(ROSAgent.AgentJob.Publisher, _clientNamespace);
        _rosLocomotionWaypoint = new ROSLocomotionWaypoint();
        _rosLocomotionWaypoint.StartAgent(ROSAgent.AgentJob.Publisher, _clientNamespace);
        _rosLocomotionWaypointState = new ROSLocomotionWaypointState();
        _rosLocomotionWaypointState.StartAgent(ROSAgent.AgentJob.Publisher, _clientNamespace);
        _rosLocomotionSpeedParams = new ROSLocomotionSpeedParams();
        _rosLocomotionSpeedParams.StartAgent(ROSAgent.AgentJob.Publisher, _clientNamespace);
        _rosLocomotionLinear = new ROSLocomotionLinearSpeed();
        _rosLocomotionLinear.StartAgent(ROSAgent.AgentJob.Publisher, _clientNamespace);
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
        _currentRobotLocomotionState = RobotLocomotionState.MOVING;
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
        //TODO: Not implemented yet

        String s = (String) state;
        //_currentRobotLocomotionState = (RobotLocomotionState) Enum.Parse(typeof(RobotLocomotionState), s.data);
    }
}