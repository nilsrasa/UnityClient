using System;
using System.Collections;
using System.Collections.Generic;
using ROSBridgeLib.geometry_msgs;
using ROSBridgeLib.nav_msgs;
using ROSBridgeLib.std_msgs;
using UnityEngine;

public class VirtualRobot : ROSController
{
    [SerializeField] private float _publishInterval = 0.05f;

    private SensorBusController _sensorBusController;
    private Dictionary<Type, ROSAgent> _rosAgents;
    private List<Type> _agentsWaitingToStart;
    private Rigidbody _rigidbody;

    //Subscribers
    private ROSLocomotionDirect _rosLocomotionDirect;

    private bool _hasLocomotionDirectDataToConsume;
    private TwistMsg _locomotionDirectDataToConsume;
    private ROSGenericSubscriber<TwistMsg> _rosJoystick;
    private bool _hasJoystickDataToConsume;
    private TwistMsg _joystickDataToConsume;

    //Publishers
    private Coroutine _transformUpdateCoroutine;

    private ROSLocomotionWaypointState _rosLocomotionWaypointState;
    private ROSLocomotionWaypoint _rosLocomotionWaypoint;
    private ROSGenericPublisher _rosLocomotionLinear;
    private ROSGenericPublisher _rosLocomotionAngular;
    private ROSLocomotionControlParams _rosLocomotionControlParams;
    private ROSGenericPublisher _rosOdometry;

    //Modules
    private WaypointNavigation _waypointNavigationModule;

    //Navigation
    private Vector3 _currentWaypoint;

    private float _waypointDistanceThreshhold = 0.1f;

    void Awake()
    {
        _rosAgents = new Dictionary<Type, ROSAgent>();
        _agentsWaitingToStart = new List<Type>();
        _rigidbody = GetComponent<Rigidbody>();
        _waypointNavigationModule = GetComponent<WaypointNavigation>();
        OnRosStarted += _waypointNavigationModule.InitialiseRos;

        CurrenLocomotionType = RobotLocomotionType.DIRECT;
        CurrentRobotLocomotionState = RobotLocomotionState.STOPPED;
    }

    void Start()
    {
        _sensorBusController = new SensorBusController(this);
    }

    void Update()
    {
        if (_agentsWaitingToStart.Count > 0)
        {
            foreach (Type type in _agentsWaitingToStart)
            {
                StartAgent(type);
            }
            _agentsWaitingToStart = new List<Type>();
        }

        //Navigation to waypoint
        if (CurrenLocomotionType != RobotLocomotionType.DIRECT &&
            CurrentRobotLocomotionState != RobotLocomotionState.STOPPED)
        {
            //Waypoint reached
            if (Vector3.Distance(transform.position, _currentWaypoint) < _waypointDistanceThreshhold)
            {
                if (Waypoints.Count > 1)
                    MoveToNextWaypoint();
                else
                {
                    EndWaypointPath();
                }
            }
        }

        if (_hasJoystickDataToConsume)
        {
            _rigidbody.velocity = transform.forward * (float) _joystickDataToConsume._linear._x;
            _rigidbody.angularVelocity = new Vector3(0, (float) -_joystickDataToConsume._angular._z, 0);
            _hasJoystickDataToConsume = false;
        }
        if (_hasLocomotionDirectDataToConsume)
        {
            _rigidbody.velocity = transform.forward * (float) _locomotionDirectDataToConsume._linear._x;
            _rigidbody.angularVelocity = new Vector3(0, (float) -_locomotionDirectDataToConsume._angular._z, 0);
            _hasLocomotionDirectDataToConsume = false;
        }
    }

    private void ReceivedJoystickUpdate(ROSBridgeMsg data)
    {
        _joystickDataToConsume = (TwistMsg) data;
        _hasJoystickDataToConsume = true;
    }

    private void ReceivedLocomotionDirectUpdate(ROSBridgeMsg data)
    {
        _locomotionDirectDataToConsume = (TwistMsg) data;
        _hasLocomotionDirectDataToConsume = true;
    }

    private IEnumerator SendTransformUpdate(float interval)
    {
        while (true)
        {
            GeoPointWGS84 wgs = transform.position.ToUTM().ToWGS84();
            Quaternion rot = transform.rotation;
            PoseMsg pose = new PoseMsg(new PointMsg(wgs.longitude, wgs.latitude, wgs.altitude),
                new QuaternionMsg(rot.x, rot.y, rot.z, rot.w));
            PoseWithCovarianceMsg poseWithCovariance = new PoseWithCovarianceMsg(pose, new double[36]);

            OdometryMsg odometry = new OdometryMsg(poseWithCovariance);
            odometry._pose = poseWithCovariance;
            _rosOdometry.PublishData(odometry);

            yield return new WaitForSeconds(interval);
        }
    }

    //TODO: Rework SIM
    private void TransmitSensorData()
    {
        /*
        foreach (SensorBus sensorBus in _sensorBusController.SensorBusses)
        {
            if (!_rosAgents.ContainsKey(sensorBus.ROSAgentType)) continue;
            _rosAgents[sensorBus.ROSAgentType].PublishData(sensorBus.GetSensorData());
        }
        */
    }

    protected override void StopROS()
    {
        base.StopROS();
        StopCoroutine(_transformUpdateCoroutine);
    }

    //TODO: Rework SIM
    public void StartAgent(Type agentType)
    {
        /*
        if (_rosBridge != null)
        {
            _agentsWaitingToStart.Add(agentType);
            return;
        }
        ROSAgent agent = (ROSAgent) Activator.CreateInstance(agentType);
        agent.StartAgent();
        _rosAgents.Add(agentType, agent);
        */
    }

    protected override void StartROS()
    {
        _rosLocomotionDirect = new ROSLocomotionDirect(ROSAgent.AgentJob.Subscriber, _rosBridge, "/cmd_vel");
        _rosLocomotionDirect.OnDataReceived += ReceivedLocomotionDirectUpdate;
        _rosJoystick = new ROSGenericSubscriber<TwistMsg>(_rosBridge, "/teleop_velocity_smoother/raw_cmd_vel",
            TwistMsg.GetMessageType(), (msg) => new TwistMsg(msg));
        _rosJoystick.OnDataReceived += ReceivedJoystickUpdate;

        _rosOdometry = new ROSGenericPublisher(_rosBridge, "/robot_gps_pose", OdometryMsg.GetMessageType());
        _transformUpdateCoroutine = StartCoroutine(SendTransformUpdate(_publishInterval));

        _rosLocomotionWaypointState = new ROSLocomotionWaypointState(ROSAgent.AgentJob.Publisher, _rosBridge, "/waypoint/state");
        _rosLocomotionWaypoint = new ROSLocomotionWaypoint(ROSAgent.AgentJob.Publisher, _rosBridge, "/waypoint");
        _rosLocomotionLinear = new ROSGenericPublisher(_rosBridge, "/waypoint/max_linear_speed", Float32Msg.GetMessageType());
        _rosLocomotionAngular = new ROSGenericPublisher(_rosBridge, "/waypoint/max_angular_speed", Float32Msg.GetMessageType());
        _rosLocomotionControlParams = new ROSLocomotionControlParams(ROSAgent.AgentJob.Publisher, _rosBridge, "/waypoint/control_parameters");

        _waypointDistanceThreshhold = RobotConfig.WaypointDistanceThreshold;

        _rosLocomotionLinear.PublishData(new Float32Msg(RobotConfig.MaxLinearSpeed));
        _rosLocomotionAngular.PublishData(new Float32Msg(RobotConfig.MaxAngularSpeed));
        _rosLocomotionControlParams.PublishData(RobotConfig.LinearSpeedParameter, RobotConfig.RollSpeedParameter, RobotConfig.PitchSpeedParameter, RobotConfig.AngularSpeedParameter);
    }

    public override void MoveDirect(Vector2 command)
    {
        if (CurrenLocomotionType != RobotLocomotionType.DIRECT)
            _rosLocomotionWaypointState.PublishData(ROSLocomotionWaypointState.RobotWaypointState.STOP);
        _rosLocomotionDirect.PublishData(command.y, command.x);
        CurrenLocomotionType = RobotLocomotionType.DIRECT;
        CurrentRobotLocomotionState = RobotLocomotionState.MOVING;
    }

    private void StartWaypointRoute()
    {
        if (Waypoints.Count == 0) return;
        CurrenLocomotionType = RobotLocomotionType.WAYPOINT;
        _currentWaypoint = Waypoints[0].ToUTM().ToUnity();
        Move(_currentWaypoint);
    }

    private void MoveToNextWaypoint()
    {
        Waypoints = Waypoints.GetRange(1, Waypoints.Count - 1);
        _currentWaypoint = Waypoints[0].ToUTM().ToUnity();
        Move(_currentWaypoint);
        WaypointController.Instance.CreateRoute(Waypoints);
    }

    private void EndWaypointPath()
    {
        StopRobot();
        if (RobotMasterController.SelectedRobot == this)
            PlayerUIController.Instance.UpdateUI(this);
        Waypoints = new List<GeoPointWGS84>();
        WaypointController.Instance.ClearAllWaypoints();
    }

    private void Move(Vector3 position)
    {
        GeoPointWGS84 point = position.ToUTM().ToWGS84();
        _rosLocomotionWaypoint.PublishData(point);
        _currentWaypoint = position;
        CurrenLocomotionType = RobotLocomotionType.WAYPOINT;
        _rosLocomotionWaypointState.PublishData(ROSLocomotionWaypointState.RobotWaypointState.RUNNING);
        CurrentRobotLocomotionState = RobotLocomotionState.MOVING;
    }

    public override void MovePath(List<GeoPointWGS84> waypoints)
    {
        Waypoints = waypoints;
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
        CurrentRobotLocomotionState = RobotLocomotionState.STOPPED;
        _rosLocomotionWaypointState.PublishData(ROSLocomotionWaypointState.RobotWaypointState.STOP);
        _rosLocomotionDirect.PublishData(0, 0);
    }

    public override void OverridePositionAndOrientation(Vector3 newPosition, Quaternion newOrientation)
    {
        transform.SetPositionAndRotation(newPosition, newOrientation);
    }

    public override void OnDeselected()
    {
        //throw new NotImplementedException();
    }
}