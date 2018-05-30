using ROSBridgeLib;
using ROSBridgeLib.sensor_msgs;
using ROSBridgeLib.std_msgs;
using UnityEngine;

public class CameraSensor : SensorModule
{
    [Range(0, 100)]
    [SerializeField] private int _cameraQualityLevel;

    [SerializeField] private int _resolutionWidth = 1640;
    [SerializeField] private int _resolutionHeight = 1232;

    private Camera _camera;
    private ROSGenericPublisher _cameraInfoPublisher;
    private ROSCameraSensorPublisher _cameraSensorPublisher;
    private Texture2D _texture2D;
    private RenderTexture _renderTexture;
    private Rect _rect;
    private int _sequenceId;

    void Awake()
    {
        _camera = GetComponentInChildren<Camera>();
    }

    public override void Initialise(ROSBridgeWebSocketConnection rosBridge)
    {
        _texture2D = new Texture2D(_resolutionWidth, _resolutionHeight, TextureFormat.RGB24, false);
        _rect = new Rect(0, 0, _resolutionWidth, _resolutionHeight);
        _renderTexture = new RenderTexture(_resolutionWidth, _resolutionHeight, 24);

        _cameraInfoPublisher = new ROSGenericPublisher(rosBridge, "/raspicam_node/camera_info", CameraInfoMsg.GetMessageType());
        _cameraSensorPublisher = new ROSCameraSensorPublisher(rosBridge, "/raspicam_node/image/compressed");

        base.Initialise(rosBridge);
    }

    protected override void PublishSensorData()
    {
        _camera.targetTexture = _renderTexture;
        _camera.Render();
        RenderTexture.active = _renderTexture;
        _texture2D.ReadPixels(_rect, 0, 0);
        _camera.targetTexture = null;
        RenderTexture.active = null;


        HeaderMsg header = new HeaderMsg(_sequenceId, new TimeMsg(0, 0), "raspicam");
        RegionOfInterestMsg roi = new RegionOfInterestMsg(0, 0, 0, 0, false);
        CameraInfoMsg camInfo = new CameraInfoMsg(header, (uint)_resolutionHeight, (uint)_resolutionWidth, "plumb_bob", new double[5], new double[9], new double[9], new double[12], 0, 0, roi);
        _cameraSensorPublisher.PublishData(_texture2D, _cameraQualityLevel, _sequenceId);
        _cameraInfoPublisher.PublishData(camInfo);

        _sequenceId++;
    }

}
