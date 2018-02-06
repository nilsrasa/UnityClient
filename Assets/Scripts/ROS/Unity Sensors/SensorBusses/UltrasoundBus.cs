using System.Collections.Generic;
using System.Linq;
using Messages;
using String = Messages.std_msgs.String;

public class UltrasoundBus : SensorBus {

    public UltrasoundBus()
    {
        ROSAgentType = typeof(ROSUltrasound);
    }

    public override IRosMessage GetSensorData()
    {
        List<Sim_UltrasoundSensor> ultrasoundSensors = Sensors.Cast<Sim_UltrasoundSensor>().ToList();
        JSONObject json = new JSONObject();
        foreach (Sim_UltrasoundSensor sensor in ultrasoundSensors)
        {
            json.AddField(sensor.SensorId, sensor.GetSensorData());
        }
        return new String(json.ToString());
    }
}
