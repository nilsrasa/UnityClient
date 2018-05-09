using ROSBridgeLib;
using ROSBridgeLib.std_msgs;
using UnityEngine;

public class UltrasoundSensor : SensorModule
{
    private ROSGenericPublisher _rosUltrasoundPublisher;

    public override void Initialise(ROSBridgeWebSocketConnection rosBridge)
    {
        _rosUltrasoundPublisher = new ROSGenericPublisher(rosBridge, "/ultrasonic_data", StringMsg.GetMessageType());
        base.Initialise(rosBridge);
    }

    void Update()
    {
        Debug.DrawRay(transform.position, transform.forward * _sensorRange, Color.green);
    }

    private float GetSensorData()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        int layer = 1 << 14;
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, _sensorRange, layer))
        {
            return hit.distance;
        }
        else
        {
            return _sensorRange;
        }
    }

    protected override void PublishSensorData()
    {
        JSONObject json = new JSONObject();
        json.AddField(_sensorId, GetSensorData());
        //_rosUltrasoundPublisher.PublishData(new StringMsg(json.ToString()));
    }
}
