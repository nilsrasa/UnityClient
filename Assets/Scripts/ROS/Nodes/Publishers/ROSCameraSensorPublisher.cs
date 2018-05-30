using System;
using ROSBridgeLib;
using ROSBridgeLib.sensor_msgs;
using ROSBridgeLib.std_msgs;
using UnityEngine;

public class ROSCameraSensorPublisher : ROSAgent {
    
    private ROSGenericPublisher _publisher;

    public ROSCameraSensorPublisher(ROSBridgeWebSocketConnection rosConnection, string topicName)
    {
        _publisher = new ROSGenericPublisher(rosConnection, topicName, CompressedImageMsg.GetMessageType());
        rosConnection.AddPublisher(_publisher);
    }

    protected override void StartAgent(ROSBridgeWebSocketConnection rosConnection, string topicName, string messageType)
    {
        throw new NotImplementedException();
    }

    public void PublishData(Texture2D texture2D, int qualityLevel, int sequenceId)
    {
        HeaderMsg header = new HeaderMsg(sequenceId, new TimeMsg(0, 0), "raspicam");
        CompressedImageMsg imageMsg = new CompressedImageMsg(header, "jpg", texture2D.EncodeToJPG(qualityLevel));
        _publisher.PublishData(imageMsg);
    }
}
