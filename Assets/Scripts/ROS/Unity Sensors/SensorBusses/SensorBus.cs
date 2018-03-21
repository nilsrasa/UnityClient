using System;
using System.Collections.Generic;

public abstract class SensorBus
{
    public Type ROSAgentType { get; protected set; }

    protected List<UnitySensor> Sensors;

    public virtual void Register(UnitySensor sensor)
    {
        if (Sensors == null) Sensors = new List<UnitySensor>();
        Sensors.Add(sensor);
    }

    public abstract ROSBridgeMsg GetSensorData();
}
