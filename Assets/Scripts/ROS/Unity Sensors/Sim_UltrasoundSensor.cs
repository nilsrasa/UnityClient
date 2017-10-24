using System.Collections;
using System.Collections.Generic;
using Messages.std_msgs;
using UnityEngine;

public class Sim_UltrasoundSensor : UnitySensor {

    [SerializeField] private string _sensorId;
    [SerializeField] private float _minSensorRange = 0.03f;
    [SerializeField] private float _maxSensorRange = 3f;
    [SerializeField] private bool _startsRunning = true;

    void Awake()
    {
        SetRunning(_startsRunning);
        SensorId = _sensorId;
    }

    void Start() {
        SensorBusController.Instance.Register<UltrasoundBus>(this);
    }

    public float GetSensorData()
    {
        if (!IsRunning) return -1;
        Ray ray = new Ray(transform.position, transform.forward*_maxSensorRange);
        RaycastHit hit;
        float sensorData = _maxSensorRange;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.distance < _minSensorRange)
                sensorData = _minSensorRange;
            else if (hit.distance > _maxSensorRange)
                sensorData = _maxSensorRange;
            else
                sensorData = hit.distance;
        }
        Debug.DrawRay(ray.origin, ray.direction * sensorData, Color.green);
        return sensorData;
    }

    
}
