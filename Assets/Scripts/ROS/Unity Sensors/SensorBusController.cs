using System;
using System.Collections.Generic;

public class SensorBusController
{
    public List<SensorBus> SensorBusses { get; private set; }

    private VirtualRobot _controller;

    public SensorBusController(VirtualRobot controller)
    {
        SensorBusses = new List<SensorBus>();
        _controller = controller;
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
            if (b.GetType().ToString().Contains(typeof(T).ToString()))
            {
                b.Register(sensor);
                SensorBusses.Remove(b);
                SensorBusses.Add(b);
                return;
            }
        }
        T sensorBus = (T) Activator.CreateInstance(typeof(T), new object[] { });
        sensorBus.Register(sensor);
        SensorBusses.Add(sensorBus);
        _controller.StartAgent(sensorBus.ROSAgentType);
    }
}