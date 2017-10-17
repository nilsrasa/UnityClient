using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Arlobot : ROSRobot
{
    private List<UnitySensor> sensors;

    //SensorBusses
    private SensorBus<Ultrasound> ultrasoundBus;

    void Awake()
    {
        sensors = GetComponentsInChildren<UnitySensor>().ToList();
    }

    void Start()
    {
        foreach (UnitySensor sensor in sensors)
        {
            sensor.SetRunning(true);
        }
    }
    
    private SensorDataDTO GetSensorData<T>(SensorBus<T> bus) where T : UnitySensor
    {
        SensorDataDTO dto = (SensorDataDTO) JsonUtility.FromJson(bus.GetSensorData(), typeof(SensorDataDTO));
        return dto;
    }

    private void HandleUltrasoundSensorData(SensorDataDTO data)
    {
        //Automated control
    }

    public SensorDataDTO GetSensorData(Type type)
    {
        SensorDataDTO data = null;
        if (type == typeof(Ultrasound))
        {
            if (ultrasoundBus == null) {
                ultrasoundBus = (SensorBus<Ultrasound>)SensorBusController.Instance.GetSensorBus<Ultrasound>();
            }
            if (ultrasoundBus != null)
            {
                data = GetSensorData<Ultrasound>(ultrasoundBus);
            }
        }

        return data;
    } 
}
