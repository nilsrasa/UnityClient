using System;
using ROSBridgeLib.std_msgs;

public class UltrasoundRepresentationBus : SensorRepresentationBus {

    public UltrasoundRepresentationBus()
    {
        
        //ROSAgentType = typeof(ROSUltrasound);
    }

    public override void HandleData(ROSBridgeMsg data)
    {
        StringMsg dataString = (StringMsg) data;
        JSONObject root = new JSONObject(dataString._data);
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
