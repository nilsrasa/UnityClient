using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ultrasound : UnitySensor {

    [SerializeField] private string _sensorId;
    [SerializeField] private float _minSensorRange = 0.03f;
    [SerializeField] private float _maxSensorRange = 3f;
    [SerializeField] private bool _startsRunning = true;


    private float _pollingTimer;

    void Awake()
    {
        SetRunning(_startsRunning);
        SensorId = _sensorId;
    }

    void Start() {
        SensorBusController.Instance.Register<Ultrasound>(this);
    }

    void Update ()
	{
	    if (!IsRunning) return;

	    _pollingTimer += Time.deltaTime;
        if (_pollingTimer >= PollingRateMs)
	    {
	        CollectSensorData();
	        _pollingTimer = 0;
	    }
	}

    private void CollectSensorData()
    {
        Ray ray = new Ray(transform.position, transform.forward*_maxSensorRange);
        RaycastHit hit;
        SensorData = _maxSensorRange;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.distance < _minSensorRange)
                SensorData = _minSensorRange;
            else if (hit.distance > _maxSensorRange)
                SensorData = _maxSensorRange;
            else
                SensorData = hit.distance;
        }
        Debug.DrawRay(ray.origin, ray.direction * (float)SensorData, Color.green);
    }

    
}
