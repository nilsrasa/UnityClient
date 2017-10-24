using System;
using System.Collections.Generic;
using Messages;

public class SensorBus
{
    public Type ROSAgentType { get; protected set; }

    protected List<UnitySensor> Sensors;

    public SensorBus()
    {
        Sensors = new List<UnitySensor>();
    }

    public virtual void Register(UnitySensor sensor)
    {
        Sensors.Add(sensor);
    }

    public virtual IRosMessage GetSensorData()
    {
        return null;
    }
}