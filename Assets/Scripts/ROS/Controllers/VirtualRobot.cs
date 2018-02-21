using System;
using System.Collections;
using System.Collections.Generic;
using Messages;
using Messages.geometry_msgs;
using Messages.sensor_msgs;
using Messages.std_msgs;
using Ros_CSharp;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class VirtualRobot : ROSController
{

    public ArlobotROSController.RobotLocomotionState CurrentRobotLocomotionState { get; private set; }
    public ArlobotROSController.RobotLocomotionType CurrenLocomotionType { get; private set; }

    private SensorBusController _sensorBusController;
    private Dictionary<Type, ROSAgent> _rosAgents;
    private List<Type> _agentsWaitingToStart;
    private Rigidbody _rigidbody;
    private string _rosMasterUri;

    //Subscribers
    private ROSLocomotionDirect _rosLocomotionDirect;
    private bool _hasLocomotionDirectDataToConsume;
    private Twist _locomotionDirectDataToConsume;
    private ROSJoystick _rosJoystick;
    private bool _hasJoystickDataToConsume;
    private Twist _joystickDataToConsume;

    //Publishers
    private ROSTransformPosition _rosTransformPosition;
    private ROSTransformHeading _rosTransformHeading;
    private Coroutine _transformUpdateCoroutine;
    private ROSLocomotionWaypointState _rosLocomotionWaypointState;
    private ROSLocomotionWaypoint _rosLocomotionWaypoint;
    private ROSLocomotionLinearSpeed _rosLocomotionLinear;
    private ROSLocomotionAngularSpeed _rosLocomotionAngular;
    private ROSLocomotionSpeedParams _rosLocomotionSpeedParams;

    //Modules
    private WaypointNavigation _waypointNavigationModule;

    //Navigation
    private Vector3 _currentWaypoint;
    private int _waypointIndex;
    private float _waypointDistanceThreshhold = 0.1f;
    private List<GeoPointWGS84> _waypoints;

    void Awake() {
        _rosAgents = new Dictionary<Type, ROSAgent>();
        _agentsWaitingToStart = new List<Type>();
        _rigidbody = GetComponent<Rigidbody>();
        _rosMasterUri = ConfigManager.ConfigFile.RosMasterUri;
        _waypointNavigationModule = GetComponent<WaypointNavigation>();
        OnRosStarted += _waypointNavigationModule.InitialiseRos;

        _waypointDistanceThreshhold = ConfigManager.ConfigFile.WaypointDistanceThreshold;
        CurrenLocomotionType = ArlobotROSController.RobotLocomotionType.DIRECT;
        CurrentRobotLocomotionState = ArlobotROSController.RobotLocomotionState.STOPPED;
    }

    void Start() {
        _sensorBusController = new SensorBusController(this);
        StartROS(_rosMasterUri);
    }

    void Update() {
        if (!(ROS.ok || ROS.isStarted()))
            return;
        if (_agentsWaitingToStart.Count > 0) {
            foreach (Type type in _agentsWaitingToStart) {
                StartAgent(type);
            }
            _agentsWaitingToStart = new List<Type>();
        }

        //Navigation to waypoint
        if (CurrenLocomotionType != ArlobotROSController.RobotLocomotionType.DIRECT && CurrentRobotLocomotionState != ArlobotROSController.RobotLocomotionState.STOPPED) {
            //Waypoint reached
            if (Vector3.Distance(transform.position, _currentWaypoint) < _waypointDistanceThreshhold) {
                if (_waypointIndex < _waypoints.Count - 1)
                    MoveToNextWaypoint();
                else {
                    EndWaypointPath();
                }
            }
        }

        if (_hasJoystickDataToConsume) {
            _rigidbody.velocity = transform.forward * (float)_joystickDataToConsume.linear.x;
            _rigidbody.angularVelocity = new Vector3(0, (float)-_joystickDataToConsume.angular.z, 0);
            _hasJoystickDataToConsume = false;
        }
        if (_hasLocomotionDirectDataToConsume)
        {
            _rigidbody.velocity = transform.forward * (float)_locomotionDirectDataToConsume.linear.x;
            _rigidbody.angularVelocity = new Vector3(0, (float)-_locomotionDirectDataToConsume.angular.z, 0);
            _hasLocomotionDirectDataToConsume = false;
        }
    }

    private void ReceivedJoystickUpdate(ROSAgent sender, IRosMessage data) {
        _joystickDataToConsume = (Twist)data;
        _hasJoystickDataToConsume = true;
    }

    private void ReceivedLocomotionDirectUpdate(ROSAgent sender, IRosMessage data)
    {
        _locomotionDirectDataToConsume = (Twist) data;
        _hasLocomotionDirectDataToConsume = true;
    }

    private IEnumerator SendTransformUpdate( )
    {
        while (true)
        {
            GeoPointWGS84 wgs = transform.position.ToUTM().ToWGS84();
            NavSatFix pos = new NavSatFix
            {
                altitude = wgs.altitude,
                longitude = wgs.longitude,
                latitude = wgs.latitude,
            };
            _rosTransformPosition.PublishData(pos);

            Float32 rot = new Float32();
            rot.data = transform.eulerAngles.y;
            _rosTransformHeading.PublishData(rot);

            yield return new WaitForEndOfFrame();
        }
    }

    private void TransmitSensorData() {
        foreach (SensorBus sensorBus in _sensorBusController.SensorBusses) {
            if (!_rosAgents.ContainsKey(sensorBus.ROSAgentType)) continue;
            _rosAgents[sensorBus.ROSAgentType].PublishData(sensorBus.GetSensorData());
        }
    }

    public override void StopROS() {
        StopCoroutine(_transformUpdateCoroutine);
        base.StopROS();
    }

    public void StartAgent(Type agentType)
    {
        if (!(ROS.ok || ROS.isStarted()))
        {
            _agentsWaitingToStart.Add(agentType);
            return;
        }
        ROSAgent agent = (ROSAgent) Activator.CreateInstance(agentType);
        agent.StartAgent(ROSAgent.AgentJob.Publisher);
        _rosAgents.Add(agentType, agent);
    }

    public override void StartROS(string uri) {
        base.StartROS(uri);

        _rosLocomotionDirect = new ROSLocomotionDirect();
        _rosLocomotionDirect.StartAgent(ROSAgent.AgentJob.Subscriber);
        _rosLocomotionDirect.DataWasReceived += ReceivedLocomotionDirectUpdate;
        _rosJoystick = new ROSJoystick();
        _rosJoystick.StartAgent(ROSAgent.AgentJob.Subscriber);
        _rosJoystick.DataWasReceived += ReceivedJoystickUpdate;

        _rosTransformPosition = new ROSTransformPosition();
        _rosTransformPosition.StartAgent(ROSAgent.AgentJob.Publisher);
        _rosTransformHeading = new ROSTransformHeading();
        _rosTransformHeading.StartAgent(ROSAgent.AgentJob.Publisher);
        _transformUpdateCoroutine = StartCoroutine(SendTransformUpdate());

        _rosLocomotionWaypointState = new ROSLocomotionWaypointState();
        _rosLocomotionWaypointState.StartAgent(ROSAgent.AgentJob.Publisher);
        _rosLocomotionWaypoint = new ROSLocomotionWaypoint();
        _rosLocomotionWaypoint.StartAgent(ROSAgent.AgentJob.Publisher);

        _rosLocomotionLinear = new ROSLocomotionLinearSpeed();
        _rosLocomotionLinear.StartAgent(ROSAgent.AgentJob.Publisher);
        _rosLocomotionAngular = new ROSLocomotionAngularSpeed();
        _rosLocomotionAngular.StartAgent(ROSAgent.AgentJob.Publisher);
        _rosLocomotionSpeedParams = new ROSLocomotionSpeedParams();
        _rosLocomotionSpeedParams.StartAgent(ROSAgent.AgentJob.Publisher);

        _rosLocomotionLinear.PublishData(ConfigManager.ConfigFile.MaxLinearSpeed);
        _rosLocomotionAngular.PublishData(ConfigManager.ConfigFile.MaxAngularSpeed);
        _rosLocomotionSpeedParams.PublishData(ConfigManager.ConfigFile.LinearSpeedParameter, ConfigManager.ConfigFile.AngularSpeedParameter);
}

    public override void MoveDirect(Vector2 command) {
        if (CurrenLocomotionType != ArlobotROSController.RobotLocomotionType.DIRECT)
            _rosLocomotionWaypointState.PublishData(ROSLocomotionWaypointState.RobotWaypointState.STOP);
        _rosLocomotionDirect.PublishData(command);
        CurrenLocomotionType = ArlobotROSController.RobotLocomotionType.DIRECT;
        CurrentRobotLocomotionState = ArlobotROSController.RobotLocomotionState.MOVING;
    }

    private void StartWaypointRoute() {
        _waypointIndex = 0;
        CurrenLocomotionType = ArlobotROSController.RobotLocomotionType.WAYPOINT;
        _currentWaypoint = _waypoints[_waypointIndex].ToUTM().ToUnity();
        Move(_currentWaypoint);
    }

    private void MoveToNextWaypoint() {
        _waypointIndex++;
        _currentWaypoint = _waypoints[_waypointIndex].ToUTM().ToUnity();
        Move(_currentWaypoint);
    }

    private void EndWaypointPath() {
        StopRobot();
        PlayerUIController.Instance.SetDriveMode(false);
    }

    private void Move(Vector3 position) {
        GeoPointWGS84 point = position.ToUTM().ToWGS84();
        _rosLocomotionWaypoint.PublishData(point);
        _currentWaypoint = position;
        CurrenLocomotionType = ArlobotROSController.RobotLocomotionType.WAYPOINT;
        _rosLocomotionWaypointState.PublishData(ROSLocomotionWaypointState.RobotWaypointState.RUNNING);
        CurrentRobotLocomotionState = ArlobotROSController.RobotLocomotionState.MOVING;
    }

    public override void MoveToPoint(GeoPointWGS84 point)
    {
        _waypoints.Clear();
        _waypoints.Add(point);
        _waypointIndex = 0;
    }

    public override void MovePath(List<GeoPointWGS84> waypoints) 
    {
        _waypoints = waypoints;
        StartWaypointRoute();
    }

    public override void PausePath()
    {
        _rosLocomotionWaypointState.PublishData(ROSLocomotionWaypointState.RobotWaypointState.PARK);
    }

    public override void ResumePath()
    {
        _rosLocomotionWaypointState.PublishData(ROSLocomotionWaypointState.RobotWaypointState.RUNNING);
    }

    public override void StopRobot()
    {
        CurrentRobotLocomotionState = ArlobotROSController.RobotLocomotionState.STOPPED;
        _rosLocomotionWaypointState.PublishData(ROSLocomotionWaypointState.RobotWaypointState.STOP);
        _rosLocomotionDirect.PublishData(Vector2.zero);
    }
}