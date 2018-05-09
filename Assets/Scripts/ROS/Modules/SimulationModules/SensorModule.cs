using System.Collections;
using ROSBridgeLib;
using UnityEngine;

public abstract class SensorModule : RobotModule
{

    [SerializeField] protected float _publishInterval = 0.5f;

    [SerializeField] protected string _sensorId;
    public string SensorId
    {
        get { return _sensorId; }
        protected set { _sensorId = value; }
    }

    [SerializeField] protected float _sensorRange;

    public float SensorRange
    {
        get { return _sensorRange; }
        protected set { _sensorRange = value; }
    }

    protected Coroutine _publishingLoop;

    protected abstract void PublishSensorData();

    protected virtual IEnumerator PublishLoop(float interval)
    {
        while (true)
        {
            PublishSensorData();
            yield return new WaitForSeconds(interval);
        }
    }

    public override void Initialise(ROSBridgeWebSocketConnection rosBridge)
    {
        base.Initialise(rosBridge);
        _publishingLoop = StartCoroutine(PublishLoop(_publishInterval));
    }

    public override void StopModule()
    {
        StopCoroutine(_publishingLoop);
    }

}
