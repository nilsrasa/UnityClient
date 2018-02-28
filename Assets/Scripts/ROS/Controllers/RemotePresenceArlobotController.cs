using System.Collections.Generic;
using Messages.sensor_msgs;
using UnityEngine;
using UnityEngine.UI;

public class RemotePresenceArlobotController : ROSController {

    [SerializeField] private RawImage _cameraImage;

    public static RemotePresenceArlobotController Instance { get; private set; }

    private ROSLocomotionDirect _rosLocomotionDirect;
    private ROSLocomotionWaypointState _rosLocomotionWaypointState;
    private ROSUltrasound _rosUltrasound;

    private List<GeoPointWGS84> _waypoints;
    
    private CompressedImage _cameraDataToConsume;
    private CameraInfo _cameraInfoToConsume;
    private bool _hasCameraDataToConsume;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
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
        _rosLocomotionDirect.PublishData(command);
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
        _rosLocomotionDirect.PublishData(Vector2.zero);
    }

    public override void StartROS(string uri) {
        base.StartROS(uri);
        _rosLocomotionDirect = new ROSLocomotionDirect();
        _rosLocomotionDirect.StartAgent(ROSAgent.AgentJob.Publisher);
        _rosLocomotionWaypointState = new ROSLocomotionWaypointState();
        _rosLocomotionWaypointState.StartAgent(ROSAgent.AgentJob.Publisher);
        //_rosCamera = new ROSCamera();
        //_rosCamera.StartAgent(ROSAgent.AgentJob.Subscriber);
        //_rosCamera.DataWasReceived += HandleImage;
    }
}