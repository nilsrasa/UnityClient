using System.Collections.Generic;
using Messages;
using Messages.nav_msgs;
using Messages.sensor_msgs;
using Messages.std_msgs;
using UnityEngine;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using String = Messages.std_msgs.String;
using Vector3 = UnityEngine.Vector3;

public class ArlobotROSController : ROSController {

    [SerializeField] private int _waypointStartIndex = 0;
    [SerializeField] private RawImage _cameraImage;

    public enum RobotLocomotionState { MOVING, STOPPED }
    public enum RobotLocomotionType { WAYPOINT, DIRECT }

    public static ArlobotROSController Instance { get; private set; }
    public RobotLocomotionState CurrentRobotLocomotionState { get; private set; }
    public RobotLocomotionType CurrenLocomotionType { get; private set; }

    private ROSLocomotionDirect _rosLocomotionDirect;
    private ROSLocomotionWaypoint _rosLocomotionWaypoint;
    private ROSLocomotionWaypointState _rosLocomotionWaypointState;
    private ROSUltrasound _rosUltrasound;
    private ROSLocomotionState _rosLocomotionState;
    private ROSLocomotionLinearSpeed _rosLocomotionLinear;
    private ROSLocomotionAngularSpeed _rosLocomotionAngular;
    private ROSLocomotionControlParams _rosLocomotionControlParams;
    private ROSOdometry _rosOdometry;
    private ROSCamera _rosCamera;

    private bool _hasOdometryDataToConsume;
    private OdometryData _odometryDataToConsume;
    private CompressedImage _cameraDataToConsume;
    private CameraInfo _cameraInfoToConsume;
    private bool _hasCameraDataToConsume;

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
    private List<GeoPointWGS84> _waypoints;

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

    void Start()
    {
        StartROS(ConfigManager.ConfigFile.RosMasterUri);
    }

    void Update()
    {
        //Direct control of robot
        float linear = 0;
        float angular = 0;
        
        if (Input.GetKey(KeyCode.UpArrow))
        {
            linear = 1f;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            linear = -1f;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            angular = 1f;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            angular = -1f;
        }
        if (linear == 0 && angular == 0 && CurrentRobotLocomotionState != RobotLocomotionState.STOPPED && CurrenLocomotionType == RobotLocomotionType.DIRECT)
        {
            StopRobot();
        }
        else if (linear != 0 || angular != 0)
        {
            MoveDirect(new Vector2(angular, linear));
        }
        
        //Navigation to waypoint
        if (CurrenLocomotionType != RobotLocomotionType.DIRECT && CurrentRobotLocomotionState != RobotLocomotionState.STOPPED)
        {
            //Waypoint reached
            if (Vector3.Distance(transform.position, _currentWaypoint) < _waypointDistanceThreshhold)
            {
                if (_waypointIndex < _waypoints.Count - 1)
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
        if (_hasCameraDataToConsume)
        {
            lock (_cameraDataToConsume)
            {
                _cameraImage.texture = ROSCamera.ConvertToTexture2D(_cameraDataToConsume, _cameraInfoToConsume);
                _hasCameraDataToConsume = false;
            }
        }
    }

    public override void MoveDirect(Vector2 command)
    {
        if (CurrenLocomotionType != RobotLocomotionType.DIRECT)
            _rosLocomotionWaypointState.PublishData(ROSLocomotionWaypointState.RobotWaypointState.STOP);
        _rosLocomotionDirect.PublishData(command);
        CurrenLocomotionType = RobotLocomotionType.DIRECT;
        CurrentRobotLocomotionState = RobotLocomotionState.MOVING;
    }

    private void StartWaypointRoute()
    {
        _waypointIndex = _waypointStartIndex;
        CurrenLocomotionType = RobotLocomotionType.WAYPOINT;
        _currentWaypoint =_waypoints[_waypointIndex].ToUTM().ToUnity();
        Move(_currentWaypoint);
    }

    private void MoveToNextWaypoint()
    {
        _waypointIndex++;
        _currentWaypoint = _waypoints[_waypointIndex].ToUTM().ToUnity();
        Move(_currentWaypoint);
    }

    private void EndWaypointPath()
    {
        StopRobot();
        PlayerUIController.Instance.SetDriveMode(false);
    }

    private void HandleImage(ROSAgent sender, CompressedImage compressedImage, CameraInfo info)
    {
        lock (_cameraDataToConsume) 
        {
            _cameraInfoToConsume = info;
            _cameraDataToConsume = compressedImage;
            _hasCameraDataToConsume = true;
        }
    }

    public override void StopRobot()
    {
        CurrentRobotLocomotionState = RobotLocomotionState.STOPPED;
        _rosLocomotionWaypointState.PublishData(ROSLocomotionWaypointState.RobotWaypointState.STOP);
        _rosLocomotionDirect.PublishData(Vector2.zero);
    }

    public override void StartROS(string uri) {
        base.StartROS(uri);
        _rosLocomotionDirect = new ROSLocomotionDirect();
        _rosLocomotionDirect.StartAgent(ROSAgent.AgentJob.Publisher);
        _rosLocomotionWaypoint = new ROSLocomotionWaypoint();
        _rosLocomotionWaypoint.StartAgent(ROSAgent.AgentJob.Publisher);
        _rosLocomotionWaypointState = new ROSLocomotionWaypointState();
        _rosLocomotionWaypointState.StartAgent(ROSAgent.AgentJob.Publisher);
        _rosLocomotionControlParams = new ROSLocomotionControlParams();
        _rosLocomotionControlParams.StartAgent(ROSAgent.AgentJob.Publisher);
        _rosLocomotionLinear = new ROSLocomotionLinearSpeed();
        _rosLocomotionLinear.StartAgent(ROSAgent.AgentJob.Publisher);
        _rosLocomotionAngular = new ROSLocomotionAngularSpeed();
        _rosLocomotionAngular.StartAgent(ROSAgent.AgentJob.Publisher);
        //_rosUltrasound = new ROSUltrasound();
       //_rosUltrasound.StartAgent(ROSAgent.AgentJob.Subscriber, _clientNamespace);
        _rosLocomotionState = new ROSLocomotionState();
        _rosLocomotionState.StartAgent(ROSAgent.AgentJob.Subscriber);
        _rosLocomotionState.DataWasReceived += ReceivedLocomotionStateUpdata;
        _rosOdometry = new ROSOdometry();
        _rosOdometry.StartAgent(ROSAgent.AgentJob.Subscriber);
        _rosOdometry.DataWasReceived += ReceivedOdometryUpdate;
        //_rosCamera = new ROSCamera();
        //_rosCamera.StartAgent(ROSAgent.AgentJob.Subscriber);
        //_rosCamera.DataWasReceived += HandleImage;

        _rosLocomotionLinear.PublishData(_maxLinearSpeed);
        _rosLocomotionAngular.PublishData(_maxAngularSpeed);
        _rosLocomotionControlParams.PublishData(_controlParameterRho, _controlParameterRoll, _controlParameterPitch, _controlParameterYaw);
    }

    private void Move(Vector3 position)
    {
        Debug.Log(position);
        GeoPointWGS84 point = position.ToUTM().ToWGS84();
        _rosLocomotionWaypoint.PublishData(point);
        _currentWaypoint = position;
        CurrenLocomotionType = RobotLocomotionType.WAYPOINT;
        _rosLocomotionWaypointState.PublishData(ROSLocomotionWaypointState.RobotWaypointState.RUNNING);
        CurrentRobotLocomotionState = RobotLocomotionState.MOVING;
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

    public void ReceivedOdometryUpdate(ROSAgent sender, IRosMessage data)
    {
        //In WGS84
        Odometry nav = (Odometry) data;

        GeoPointWGS84 geoPoint = new GeoPointWGS84
        {
            latitude = nav.pose.pose.position.y,
            longitude = nav.pose.pose.position.x,
            altitude = nav.pose.pose.position.z,
        };
        Quaternion orientation = new Quaternion(
            x: (float)nav.pose.pose.orientation.x, 
            y: (float)nav.pose.pose.orientation.y, 
            z: (float)nav.pose.pose.orientation.z, 
            w: (float)nav.pose.pose.orientation.w
        );
        _odometryDataToConsume = new OdometryData
        {
            Position = geoPoint,
            Orientation = orientation
        };
        _hasOdometryDataToConsume = true;
    }

    //TODO: Not yet implemented
    public void ReceivedLocomotionStateUpdata(ROSAgent sender, IRosMessage state)
    {
        //TODO: Not implemented yet

        String s = (String) state;
        //_currentRobotLocomotionState = (RobotLocomotionState) Enum.Parse(typeof(RobotLocomotionState), s.data);
    }

    private struct OdometryData
    {
        public GeoPointWGS84 Position;
        public Quaternion Orientation;
    }
}