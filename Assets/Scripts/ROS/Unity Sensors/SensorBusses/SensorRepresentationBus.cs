using System;
using System.Collections.Generic;
using Messages;
using UnityEngine;

public class SensorRepresentationBus {

    public Type ROSAgentType { get; protected set; }

    protected List<SensorRepresentation> _sensorRepresentations;

    public SensorRepresentationBus()
    {
        _sensorRepresentations = new List<SensorRepresentation>();
    }

    public virtual void Register(SensorRepresentation sensorRepresentation)
    {
        _sensorRepresentations.Add(sensorRepresentation);
    }

    public virtual void HandleData(IRosMessage data)
    {
    }
}
