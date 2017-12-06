using System.Collections;
using System.Collections.Generic;
using Messages;
using Messages.std_msgs;
using UnityEngine;

public class UltrasoundRepresentationBus : SensorRepresentationBus {

    public UltrasoundRepresentationBus()
    {
        ROSAgentType = typeof(ROSUltrasound);
    }

    public override void HandleData(IRosMessage data)
    {
        String dataString = (String) data;
        JSONObject root = new JSONObject(dataString.data);
        foreach (string key in root.keys)
        {
            foreach (SensorRepresentation sensor in _sensorRepresentations) {
                if (sensor.SensorId == key)
                {
                    string dat = "";
                    root.GetField(out dat, key, "");
                    sensor.HandleData(float.Parse(dat));
                }
            }
        }
    }
}
