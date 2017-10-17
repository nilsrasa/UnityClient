using Messages.geometry_msgs;
using Ros_CSharp;
using UnityEngine;

public class ROSLocomotion
{
    private const string TOPIC = "/control/locomotion";

    private NodeHandle _nodeHandle;
    private Publisher<Twist> _publisher;
    private Twist _dataToSend;
    private float _messageInterval;
    private bool _isRunning;

    ///<summary>
    ///Starts advertising loop
    ///</summary>
    ///<param name="messageInterval">Minimum time (ms) between messages sent</param>
    public void Start(float messageInterval)
    {
        if (_isRunning) return;
        _nodeHandle = new NodeHandle();
        _publisher = _nodeHandle.advertise<Twist>(ROSController.NAMESPACE_VRClient + TOPIC, 1, false);

        _isRunning = true;
    }

    ///<summary>
    ///Stops advertising loop
    ///</summary>
    public void Stop() {
        if (!_isRunning) return;
        _nodeHandle.shutdown();
        _publisher = null;
        _nodeHandle = null;
    }

    public void PublishData(Vector2 data)
    {
        Twist twist = new Twist
        {
            angular = new Messages.geometry_msgs.Vector3
            {
                x = 0,
                y = 0,
                z = data.x
            },
            linear = new Messages.geometry_msgs.Vector3 {
                x = data.y,
                y = 0,
                z = 0
            }
        };

        _publisher.publish(twist);
    }
}
