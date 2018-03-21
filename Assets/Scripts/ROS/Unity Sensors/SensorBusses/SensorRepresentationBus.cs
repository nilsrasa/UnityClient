using System;
using System.Collections.Generic;

public abstract class SensorRepresentationBus {

    public Type ROSAgentType { get; protected set; }

    protected List<SensorRepresentation> _sensorRepresentations;

    public virtual void Register(SensorRepresentation sensorRepresentation)
    {
        if (_sensorRepresentations == null) _sensorRepresentations = new List<SensorRepresentation>();
        _sensorRepresentations.Add(sensorRepresentation);
    }

    public abstract void HandleData(ROSBridgeMsg data);
}
