using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SensorBusController : MonoBehaviour {

	public static SensorBusController Instance { get; private set; }

    private Dictionary<string, ISensorBus> SensorBusDict;
    private List<ISensorBus> SensorBusses;

    void Awake()
    {
        Instance = this;
        SensorBusses = new List<ISensorBus>();
        SensorBusDict = new Dictionary<string, ISensorBus>();
    }

    public ISensorBus GetSensorBus<T>()
    {
        foreach (ISensorBus bus in SensorBusses)
        {
            if (bus.GetType().ToString().Contains(typeof(T).ToString()))
                return bus;
        }
        return null;
    }

    public void Register<T>(UnitySensor sensor) where T : UnitySensor
    {
        for (int i = 0; i < SensorBusses.Count; i++)
        {
            ISensorBus b = SensorBusses[i];
            if (b.GetType().ToString().Contains(typeof(T).ToString())) {
                SensorBus<T> buss = (SensorBus<T>)b;
                buss.Register(sensor);
                SensorBusses.Remove(b);
                SensorBusses.Add(buss);
                return;
            }
        }
        SensorBus<T> bus = new SensorBus<T>();
        bus.Register(sensor);
        SensorBusses.Add(bus);
    }
}
