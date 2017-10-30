using System;
using System.Collections.Generic;
using UnityEngine;

public class SensorBusController
{

    public static SensorBusController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new SensorBusController();
            }
            return _instance;
        }
    }
    private static SensorBusController _instance;

    public List<SensorBus> SensorBusses { get; private set; }

    public SensorBusController()
    {
        SensorBusses = new List<SensorBus>();
    }

    public SensorBus GetSensorBus<T>()
    {
        foreach (SensorBus bus in SensorBusses)
        {
            if (bus.GetType().ToString().Contains(typeof(T).ToString()))
                return bus;
        }
        return null;
    }
    
    public void Register<T>(UnitySensor sensor) where T : SensorBus
    {
        for (int i = 0; i < SensorBusses.Count; i++)
        {
            SensorBus b = SensorBusses[i];
            if (b.GetType().ToString().Contains(typeof(T).ToString())) {
                b.Register(sensor);
                SensorBusses.Remove(b);
                SensorBusses.Add(b);
                return;
            }
        }
        T sensorBus = (T)Activator.CreateInstance(typeof(T), new object[]{});
        sensorBus.Register(sensor);
        SensorBusses.Add(sensorBus);
        SimulatedROSRobotController.Instance.StartAgent(sensorBus.ROSAgentType);
    }
    
}
