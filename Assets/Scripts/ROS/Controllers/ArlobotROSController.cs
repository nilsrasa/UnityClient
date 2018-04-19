using System.Collections.Generic;
using ROSBridgeLib.geometry_msgs;
using ROSBridgeLib.nav_msgs;
using ROSBridgeLib.std_msgs;
using UnityEngine;
using UnityEngine.UI;

public class ArlobotROSController : ROSController {

    [SerializeField] private RawImage _cameraImage;

    public static ArlobotROSController Instance { get; private set; }

    private ROSLocomotionDirect _rosLocomotionDirect;
    private ROSLocomotionWaypoint _rosLocomotionWaypoint;
    private ROSLocomotionWaypointState _rosLocomotionWaypointState;
    private ROSGenericPublisher _rosLocomotionLinear;
    private ROSGenericPublisher _rosLocomotionAngular;
    private ROSLocomotionControlParams _rosLocomotionControlParams;
    private ROSGenericSubscriber<StringMsg> _rosUltrasound;
    private ROSGenericSubscriber<OdometryMsg> _rosOdometry;
    private ROSGenericPublisher _rosOdometryOverride;

    private bool _hasOdometryDataToConsume;
    private OdometryData _odometryDataToConsume;
    //private CompressedImageMsg _cameraDataToConsume;
    //private CameraInfo _cameraInfoToConsume;
    //private bool _hasCameraDataToConsume;

    //Navigation
    private Vector3 _currentWaypoint;
    private int _waypointIndex;
    private float _waypointDistanceThreshhold = 0.1f;
    private float _maxLinearSpeed;
    private float _maxAngularSpeed;
    private float _controlParameterRho;
    private float _controlParameterRoll;
    private float _controlParameterPitch;
    private float _controlParameterYaw;

    void Awake()
    {
        Instance = this;
        _waypointDistanceThreshhold = ConfigManager.ConfigFile.WaypointDistanceThreshold;
        _maxLinearSpeed = ConfigManager.ConfigFile.MaxLinearSpeed;
        _maxAngularSpeed = ConfigManager.ConfigFile.MaxAngularSpeed;
        _controlParameterRho = ConfigManager.ConfigFile.ControlParameterRho;
        _controlParameterRoll = ConfigManager.ConfigFile.ControlParameterRoll;
        _controlParameterPitch = ConfigManager.ConfigFile.ControlParameterPitch;
        _controlParameterYaw = ConfigManager.ConfigFile.ControlParameterYaw;
        CurrenLocomotionType = RobotLocomotionType.DIRECT;
        CurrentRobotLocomotionState = RobotLocomotionState.STOPPED;

    }

    void Update()
    {
        //Navigation to waypoint
        if (CurrenLocomotionType != RobotLocomotionType.DIRECT && CurrentRobotLocomotionState != RobotLocomotionState.STOPPED)
        {
            //Waypoint reached
            if (Vector3.Distance(transform.position, _currentWaypoint) < _waypointDistanceThreshhold)
            {
                if (_waypointIndex < Waypoints.Count - 1)
                    MoveToNextWaypoint();
                else
                {
                    EndWaypointPath();
                }
            }
        }

        if (_hasOdometryDataToConsume)
        {
            transform.rotation = _odometryDataToConsume.Orientation;
            transform.position = _odometryDataToConsume.Position.ToUTM().ToUnity();
            _hasOdometryDataToConsume = false;
        }
        /*
        if (_hasCameraDataToConsume)
        {
            lock (_cameraDataToConsume)
            {
                _cameraImage.texture = ROSCamera.ConvertToTexture2D(_cameraDataToConsume, _cameraInfoToConsume);
                _hasCameraDataToConsume = false;
            }
        }*/
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
        _waypointIndex = 0;
        CurrenLocomotionType = RobotLocomotionType.WAYPOINT;
        _currentWaypoint = Waypoints[_waypointIndex].ToUTM().ToUnity();
        Move(_currentWaypoint);
    }

    private void MoveToNextWaypoint()
    {
        _waypointIndex++;
        _currentWaypoint = Waypoints[_waypointIndex].ToUTM().ToUnity();
        Move(_currentWaypoint);
    }

    private void EndWaypointPath()
    {
        StopRobot();
        if (RobotMasterController.SelectedRobot == this)
            PlayerUIController.Instance.UpdateUI(this);
    }
    /*
    private void HandleImage(ROSAgent sender, CompressedImageMsg compressedImage, CameraInfo info)
    {
        lock (_cameraDataToConsume) 
        {
            _cameraInfoToConsume = info;
            _cameraDataToConsume = compressedImage;
            _hasCameraDataToConsume = true;
        }
    }*/

    protected override void StartROS()
    {
        _rosLocomotionDirect = new ROSLocomotionDirect(ROSAgent.AgentJob.Publisher, _rosBridge, "/cmd_vel");
        _rosLocomotionWaypoint = new ROSLocomotionWaypoint(ROSAgent.AgentJob.Publisher, _rosBridge, "/waypoint");
        _rosLocomotionWaypointState = new ROSLocomotionWaypointState(ROSAgent.AgentJob.Publisher, _rosBridge, "/waypoint/state");
        _rosLocomotionControlParams = new ROSLocomotionControlParams(ROSAgent.AgentJob.Publisher, _rosBridge, "/waypoint/control_parameters");
        _rosLocomotionLinear = new ROSGenericPublisher(_rosBridge, "/waypoint/max_linear_speed", Float32Msg.GetMessageType());
        _rosLocomotionAngular = new ROSGenericPublisher(_rosBridge, "/waypoint/max_angular_speed", Float32Msg.GetMessageType());
        _rosOdometryOverride = new ROSGenericPublisher(_rosBridge, "/odo_calib_pose", OdometryMsg.GetMessageType());

        _rosUltrasound = new ROSGenericSubscriber<StringMsg>(_rosBridge, "/ultrasonic_data", StringMsg.GetMessageType(), (msg) => new StringMsg(msg));
        _rosUltrasound.OnDataReceived += ReceivedUltrasoundUpdata;
        _rosOdometry = new ROSGenericSubscriber<OdometryMsg>(_rosBridge, "/robot_gps_pose", OdometryMsg.GetMessageType(), (msg) => new OdometryMsg(msg));
        _rosOdometry.OnDataReceived += ReceivedOdometryUpdate;

        _rosLocomotionLinear.PublishData(new Float32Msg(_maxLinearSpeed));
        _rosLocomotionAngular.PublishData(new Float32Msg(_maxAngularSpeed));
        _rosLocomotionControlParams.PublishData(_controlParameterRho, _controlParameterRoll, _controlParameterPitch, _controlParameterYaw);
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

    public override void StopRobot()
    {
        CurrentRobotLocomotionState = RobotLocomotionState.STOPPED;
        _rosLocomotionWaypointState.PublishData(ROSLocomotionWaypointState.RobotWaypointState.STOP);
        _rosLocomotionDirect.PublishData(0, 0);
    }


    public override void OnDeselected()
    {
        //throw new System.NotImplementedException();
    }

    public override void MoveToPoint(GeoPointWGS84 point)
    {
        Waypoints.Clear();
        Waypoints.Add(point);
        _waypointIndex = 0;
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

    public void ReceivedOdometryUpdate(ROSBridgeMsg data)
    {
        //In WGS84
        OdometryMsg nav = (OdometryMsg) data;

        GeoPointWGS84 geoPoint = new GeoPointWGS84
        {
            latitude = nav._pose._pose._position.GetY(),
            longitude = nav._pose._pose._position.GetX(),
            altitude = nav._pose._pose._position.GetZ(),
        };
        Quaternion orientation = new Quaternion(
            x: nav._pose._pose._orientation.GetX(), 
            z: nav._pose._pose._orientation.GetY(), 
            y: nav._pose._pose._orientation.GetZ(), 
            w: nav._pose._pose._orientation.GetW()
        );
        _odometryDataToConsume = new OdometryData
        {
            Position = geoPoint,
            Orientation = orientation
        };
        _hasOdometryDataToConsume = true;
    }

    //TODO: Not yet implemented
    public void ReceivedLocomotionStateUpdata(ROSBridgeMsg data)
    {
        //TODO: Not implemented yet

        StringMsg s = (StringMsg) data;
        //_currentRobotLocomotionState = (RobotLocomotionState) Enum.Parse(typeof(RobotLocomotionState), s.data);
    }

    public void ReceivedUltrasoundUpdata(ROSBridgeMsg data)
    {
        //TODO: Not implemented yet

        StringMsg s = (StringMsg) data;
    }

    public override void OverridePositionAndOrientation(Vector3 newPosition, Quaternion newOrientation)
    {
        GeoPointWGS84 wgs84 = newPosition.ToUTM().ToWGS84();

        PoseWithCovarianceMsg pose = new PoseWithCovarianceMsg(
            new PoseMsg(
                new PointMsg(wgs84.longitude, wgs84.latitude, wgs84.altitude), 
                new QuaternionMsg(newOrientation.x, newOrientation.z, newOrientation.y, newOrientation.w)
            ));

        OdometryMsg odometryOverride = new OdometryMsg(pose);
        _rosOdometryOverride.PublishData(odometryOverride);
    }

    private struct OdometryData
    {
        public GeoPointWGS84 Position;
        public Quaternion Orientation;
    }
}