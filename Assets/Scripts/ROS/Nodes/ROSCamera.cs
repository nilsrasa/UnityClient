using System.Collections.Generic;
using Messages;
using Messages.sensor_msgs;
using Ros_CSharp;
using UnityEngine;

public class ROSCamera : ROSAgent {

    private const string CAMERA_TOPIC = "/raspicam_node/image/compressed";
    private const string CAMERA_INFO_TOPIC = "/raspicam_node/camera_info";

    public new delegate void DataReceived(ROSAgent sender, CompressedImage image, CameraInfo info);
    public new event DataReceived DataWasReceived;

    private NodeHandle _nodeHandleImage;
    private NodeHandle _nodeHandleCameraInfo;
    private Subscriber<CompressedImage> _cameraImageSubscriber;
    private Subscriber<CameraInfo> _cameraInfoSubscriber;
    private Publisher<CompressedImage> _publisherImage;
    private Publisher<CameraInfo> _publisherCameraInfo;
    private bool _isRunning;
    private AgentJob _job;
    private readonly Dictionary<string, CameraInfo> _cameraInfos = new Dictionary<string, CameraInfo>();

    public static Texture2D ConvertToTexture2D(CompressedImage image, CameraInfo imageInfo)
    {
        Texture2D texture = new Texture2D((int)imageInfo.width, (int)imageInfo.height);
        texture.LoadImage(image.data);
        return texture;
    }

    private void ReceivedCameraInfo(IRosMessage data)
    {
        var cameraInfo = data as CameraInfo;
        if (cameraInfo != null)
        {
            lock (_cameraInfos)
            {
                _cameraInfos[cameraInfo.header.frame_id] = cameraInfo;
            }
        }
    }

    ///<summary>
    ///Starts advertising loop
    /// <param name="job">Defines behaviour of agent</param>
    ///</summary>
    public override void StartAgent(AgentJob job) {
        base.StartAgent(job);
        _nodeHandleImage = new NodeHandle();
        _nodeHandleCameraInfo = new NodeHandle();
        if (job == AgentJob.Subscriber)
        {
            _cameraImageSubscriber = _nodeHandleImage.subscribe<CompressedImage>(CAMERA_TOPIC, 10, ReceivedData);
            _cameraInfoSubscriber = _nodeHandleCameraInfo.subscribe<CameraInfo>(CAMERA_INFO_TOPIC, 10, ReceivedCameraInfo);
        }
        else if (job == AgentJob.Publisher)
        {
            _publisherImage = _nodeHandleImage.advertise<CompressedImage>(CAMERA_TOPIC, 1, false);
            _publisherCameraInfo = _nodeHandleCameraInfo.advertise<CameraInfo>(CAMERA_INFO_TOPIC, 1, false);
        }
        _isRunning = true;
        _job = job;
        //Application.logMessageReceived += LogMessage;
    }

    public override void PublishData(object data) {
        if (_job != AgentJob.Publisher) return;
        CompressedImage dataString = (CompressedImage)data;
        _publisherImage.publish(dataString);
    }

    protected override void ReceivedData(IRosMessage data)
    {
        var image = data as CompressedImage;
        if (image != null)
        {
            lock (_cameraInfos)
            {
                CameraInfo cameraInfo;
                if (_cameraInfos.TryGetValue(image.header.frame_id, out cameraInfo))
                {
                    if (DataWasReceived != null)
                        DataWasReceived(this, image, cameraInfo);
                }
            }
        }
    }

    ///<summary>
    ///Stops advertising loop
    ///</summary>
    public void Stop() {
        if (!_isRunning) return;
        _nodeHandleImage.shutdown();
        _nodeHandleCameraInfo.shutdown();
        _cameraImageSubscriber.shutdown();
        _cameraImageSubscriber = null;
        _cameraInfoSubscriber = null;
        _nodeHandleImage = null;

    }


}
