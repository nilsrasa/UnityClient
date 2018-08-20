using System;
using System.Collections;
using System.Collections.Generic;
using ROSBridgeLib.geometry_msgs;
using ROSBridgeLib.nav_msgs;
using ROSBridgeLib.std_msgs;
using UnityEngine;




public class VirtualRobot : ROSController
{
    private struct OdometryData
    {
        public GeoPointWGS84 Position;
        public Quaternion Orientation;
    }

    [SerializeField] private float _publishInterval = 0.05f;

    private Rigidbody _rigidbody;

    //Subscribers
    private ROSLocomotionDirect _rosLocomotionDirect;

    private bool _hasLocomotionDirectDataToConsume;
    private TwistMsg _locomotionDirectDataToConsume;
    private ROSGenericSubscriber<TwistMsg> _rosJoystick;

    //VIRTUAL ROBOT IS MISSING ODOMETRY subscriber : check ArlobotROSController 
    // By having a subscriber I want to receive and update my odometry based on what changed from the robot.
    // ROS does not have a odometry generation information. This was received by ROS through the physical sensors. 
    // The virtual robot is the one that will calculate the odometry now and essentially will simulate the physical
    // sensors.
    private ROSGenericSubscriber<OdometryMsg> _rosOdometrySubscriber;

   
    // Are these needed ?  The virtual robot does not have odometry data to consume since it is the one doing
    // the calculations. 
    private OdometryData _odometryDataToConsume;
    private bool _hasOdometryDataToConsume;

    private bool _hasJoystickDataToConsume;
    private TwistMsg _joystickDataToConsume;

    //Publishers
    private Coroutine _transformUpdateCoroutine;
    private ROSLocomotionWaypointState _rosLocomotionWaypointState;
    private ROSLocomotionWaypoint _rosLocomotionWaypoint;
    private ROSGenericPublisher _rosLocomotionLinear;
    private ROSGenericPublisher _rosLocomotionAngular;
    private ROSLocomotionControlParams _rosLocomotionControlParams;
    private ROSGenericPublisher _rosLogger;
    private ROSGenericPublisher _rosOdometryOverride;

    //odometry
    private ROSGenericPublisher _rosOdometryGPSPose;
    private ROSGenericPublisher _rosOdometry;
    private OdometryData _odometryDataToPublish;
    private bool _hasOdometryDataToPublish;
    private float _PublishOdomTimer;
    private Vector3 InitialPosition;
    private Quaternion InitialRotation;
    

    //Navigation
    private Vector3 _currentWaypoint;

    protected override void Awake()
    {
        base.Awake();
        _rigidbody = GetComponent<Rigidbody>();
        _robotLogger = GetComponent<RobotLogger>();
        CurrenLocomotionType = RobotLocomotionType.DIRECT;
        CurrentRobotLocomotionState = RobotLocomotionState.STOPPED;
    }

    private IEnumerator adsjio()
    {
        while (true)
        {
            yield return new WaitForSeconds(2);

            _rosLogger.PublishData(new StringMsg("Hi!"));
        }

    }

    void Start()
    {
        StartCoroutine(adsjio());

        //initialise odometry 
        _hasOdometryDataToPublish = true;
        InitialPosition = gameObject.transform.position;
        PublishOdometryData(InitialPosition, InitialRotation);
        _PublishOdomTimer = 0.0f;

       
    }

    protected override void Update()
    {
        base.Update();

        Debug.Log("UPDATE VIRTUAL ROBOT");
        //Navigation to waypoint
        if (CurrenLocomotionType != RobotLocomotionType.DIRECT &&
            CurrentRobotLocomotionState != RobotLocomotionState.STOPPED)
        {
            //Waypoint reached
            if (Vector3.Distance(transform.position, _currentWaypoint) < Waypoints[0].ThresholdZone.Threshold)
            {
                _rosLogger.PublishData(new StringMsg("Reached waypoint"));
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
            Debug.Log("Received velocity data");
            _rigidbody.velocity = transform.forward * (float) _locomotionDirectDataToConsume._linear._x;
            _rigidbody.angularVelocity = new Vector3(0, (float) -_locomotionDirectDataToConsume._angular._z, 0);
            _hasLocomotionDirectDataToConsume = false;
            _hasOdometryDataToPublish = true;
        }

        //publish odometry every publish interval seconds

        _PublishOdomTimer += Time.deltaTime;
        if (_PublishOdomTimer > _publishInterval)
        {
            //Debug.Log("Publishing odometry data");
            //calculate the difference between the current position and the initial position
            PublishOdometryData(gameObject.transform.position - InitialPosition , gameObject.transform.rotation);
            _PublishOdomTimer = 0.0f;
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


    //Publisher to /robot_gps_pose every interval time
    private IEnumerator SendTransformUpdate(float interval)
    {
        while (true)
        {
           //Add the Noise here. Unity should have the ACTUAL position of the physical world
           // and what it publishes and we see on the webmap is the where it thinks it is.
            GeoPointWGS84 wgs = transform.position.ToUTM().ToWGS84();
            Quaternion rot = transform.rotation;
            
            PoseMsg pose = new PoseMsg(new PointMsg(wgs.longitude, wgs.latitude, wgs.altitude),
                new QuaternionMsg(rot.x, rot.y, rot.z, rot.w));
            PoseWithCovarianceMsg poseWithCovariance = new PoseWithCovarianceMsg(pose, new double[36]);

            OdometryMsg odometry = new OdometryMsg(poseWithCovariance);
            odometry._pose = poseWithCovariance;
            _rosOdometryGPSPose.PublishData(odometry);

            yield return new WaitForSeconds(interval);
        }
    }

    protected override void StopROS()
    {
        base.StopROS();
        StopCoroutine(_transformUpdateCoroutine);
    }

    public void ManualStartRos()
    {
        StartROS();
    }

    protected override void StartROS()
    {

        Debug.Log("Started ROS");
        Debug.Log(_rosBridge);
        _rosLocomotionDirect = new ROSLocomotionDirect(ROSAgent.AgentJob.Subscriber, _rosBridge, "/cmd_vel");
        _rosLocomotionDirect.OnDataReceived += ReceivedLocomotionDirectUpdate;
        _rosJoystick = new ROSGenericSubscriber<TwistMsg>(_rosBridge, "/teleop_velocity_smoother/raw_cmd_vel",
            TwistMsg.GetMessageType(), (msg) => new TwistMsg(msg));
        _rosJoystick.OnDataReceived += ReceivedJoystickUpdate;
       
        //odometry subscriber is not needed for the virtual robot
        //_rosOdometrySubscriber = new ROSGenericSubscriber<OdometryMsg>(_rosBridge, "/odom", OdometryMsg.GetMessageType(), (msg) => new OdometryMsg(msg));
        //_rosOdometrySubscriber.OnDataReceived += ReceivedOdometryUpdate;

        //odometry publisher
        _rosOdometry = new ROSGenericPublisher(_rosBridge, "/odom", OdometryMsg.GetMessageType());
        //robot_gps_pose publisher
        _rosOdometryGPSPose = new ROSGenericPublisher(_rosBridge, "/robot_gps_pose", OdometryMsg.GetMessageType());
        //odo_calib_pose publisher
        _rosOdometryOverride = new ROSGenericPublisher(_rosBridge, "/odo_calib_pose", OdometryMsg.GetMessageType());

        _transformUpdateCoroutine = StartCoroutine(SendTransformUpdate(_publishInterval));

        _rosLocomotionWaypointState = new ROSLocomotionWaypointState(ROSAgent.AgentJob.Publisher, _rosBridge, "/waypoint/state");
        _rosLocomotionWaypoint = new ROSLocomotionWaypoint(ROSAgent.AgentJob.Publisher, _rosBridge, "/waypoint");
        _rosLocomotionLinear = new ROSGenericPublisher(_rosBridge, "/waypoint/max_linear_speed", Float32Msg.GetMessageType());
        _rosLocomotionAngular = new ROSGenericPublisher(_rosBridge, "/waypoint/max_angular_speed", Float32Msg.GetMessageType());
        _rosLocomotionControlParams = new ROSLocomotionControlParams(ROSAgent.AgentJob.Publisher, _rosBridge, "/waypoint/control_parameters");
        _rosLogger = new ROSGenericPublisher(_rosBridge, "/debug_output", StringMsg.GetMessageType());

        _rosLocomotionLinear.PublishData(new Float32Msg(RobotConfig.MaxLinearSpeed));
        _rosLocomotionAngular.PublishData(new Float32Msg(RobotConfig.MaxAngularSpeed));
        _rosLocomotionControlParams.PublishData(RobotConfig.LinearSpeedParameter, RobotConfig.RollSpeedParameter, RobotConfig.PitchSpeedParameter, RobotConfig.AngularSpeedParameter);
    }

    //publishing on the /odom topic 
    // the newposition should be the difference between the current position  
    //and the InitialPosition of the robot in Unity vec3, effectively simulating the local space coords of odom
    //The geopoint conversion is in here.
    public void PublishOdometryData(Vector3 newPosition, Quaternion newOrientation)
    {
        GeoPointWGS84 wgs84 = newPosition.ToUTM().ToWGS84();

        PoseWithCovarianceMsg pose = new PoseWithCovarianceMsg(
            new PoseMsg(
                new PointMsg(wgs84.longitude, wgs84.latitude, wgs84.altitude),
                new QuaternionMsg(newOrientation.x, newOrientation.z, newOrientation.y, newOrientation.w)
            ));

        OdometryMsg odometryUpdate= new OdometryMsg(pose);
       // _rosOdometry.PublishData(odometryUpdate);
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

       
        _rosLogger.PublishData(new StringMsg("Starting new waypoint route"));
        CurrenLocomotionType = RobotLocomotionType.WAYPOINT;
        _currentWaypoint = Waypoints[0].Point.ToUTM().ToUnity();
        Move(_currentWaypoint);
    }

    private void MoveToNextWaypoint()
    {
       
        _rosLogger.PublishData(new StringMsg("Moving to next waypoint"));
        Waypoints = Waypoints.GetRange(1, Waypoints.Count - 1);
        _currentWaypoint = Waypoints[0].Point.ToUTM().ToUnity();
        Move(_currentWaypoint);
        WaypointController.Instance.CreateRoute(Waypoints);
    }

    private void EndWaypointPath()
    {
        _rosLogger.PublishData(new StringMsg("Route ended"));
        StopRobot();
        if (RobotMasterController.SelectedRobot == this)
            PlayerUIController.Instance.UpdateUI(this);
        Waypoints = new List<WaypointController.Waypoint>();
        WaypointController.Instance.ClearAllWaypoints();
    }

    private void Move(Vector3 position)
    {
        
        _rosLogger.PublishData(new StringMsg("Moving to: " + position.ToUTM().ToWGS84()));
        GeoPointWGS84 point = position.ToUTM().ToWGS84();
        _rosLocomotionWaypoint.PublishData(point);
        _currentWaypoint = position;
        CurrenLocomotionType = RobotLocomotionType.WAYPOINT;
        _rosLocomotionWaypointState.PublishData(ROSLocomotionWaypointState.RobotWaypointState.RUNNING);
        CurrentRobotLocomotionState = RobotLocomotionState.MOVING;
    }

    public override void MovePath(List<WaypointController.Waypoint> waypoints)
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
       
        _rosLogger.PublishData(new StringMsg("Stopping"));
        CurrentRobotLocomotionState = RobotLocomotionState.STOPPED;
        _rosLocomotionWaypointState.PublishData(ROSLocomotionWaypointState.RobotWaypointState.STOP);
        _rosLocomotionDirect.PublishData(0, 0);
    }

    public override void OverridePositionAndOrientation(Vector3 newPosition, Quaternion newOrientation)
    {
        _rosLogger.PublishData(new StringMsg("Odometry overwritten"));

        //Unity repositioning for the virtual robot.
        transform.SetPositionAndRotation(newPosition, newOrientation);

        //reset initial poses for the odom calculation. This is the equivalent of moving the robot in the physical room.
        // We simply move the local coords.
        InitialPosition = newPosition;
        InitialRotation = newOrientation;

        //Publishing to odo_calib_pose for initial gps position, just like the arlobot controller.
        GeoPointWGS84 wgs84 = newPosition.ToUTM().ToWGS84();
        PoseWithCovarianceMsg pose = new PoseWithCovarianceMsg(
            new PoseMsg(
                new PointMsg(wgs84.longitude, wgs84.latitude, wgs84.altitude),
                new QuaternionMsg(newOrientation.x, newOrientation.z, newOrientation.y, newOrientation.w)
            ));

       
        OdometryMsg odometryUpdate = new OdometryMsg(pose);
        _rosOdometryOverride.PublishData(odometryUpdate);


}

    public override void OnDeselected()
    {
        //throw new NotImplementedException();
    }

    public override List<RobotLog> GetRobotLogs()
    {
        return _robotLogger.GetLogs();
    }
}