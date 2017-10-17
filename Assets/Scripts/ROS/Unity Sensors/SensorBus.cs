using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SensorBus<T> : ISensorBus where T : UnitySensor {

	public List<UnitySensor> Sensors { get; private set; }

    public SensorBus()
    {
        Sensors = new List<UnitySensor>();
    }

    public void Register(UnitySensor sensor)
    {
        Sensors.Add(sensor);
    }

    public string GetSensorData()
    {
        List<SensorData> data = new List<SensorData>();
        foreach (T sensor in Sensors)
        {
            data.Add(new SensorData(sensor.SensorId, sensor.SensorData.ToString(), sensor.transform.localPosition, sensor.transform.localEulerAngles));
        }
        SensorDataDTO dto = new SensorDataDTO(data.ToArray());

        string jsonString = JsonUtility.ToJson(dto);
        return jsonString;
    }
}
