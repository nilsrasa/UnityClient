using System.Collections;
using System.Collections.Generic;
using Messages;
using Messages.std_msgs;
using SimpleJSON;
using UnityEngine;

public class UltrasoundRepresentationBus : SensorRepresentationBus {

    public UltrasoundRepresentationBus()
    {
        ROSAgentType = typeof(ROSUltrasound);
    }

    public override void HandleData(IRosMessage data)
    {
        String dataString = (String) data;
        foreach (KeyValuePair<string, JSONNode> pair in JSON.Parse(dataString.data).AsObject)
        {
            foreach (SensorRepresentation sensor in _sensorRepresentations)
            {
                if (sensor.SensorId == pair.Key)
                {
                    sensor.HandleData(float.Parse(pair.Value));
                }
            }
        }
    }
}
