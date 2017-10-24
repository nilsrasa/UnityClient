
using System;
using System.CodeDom;
using System.Collections.Generic;
using Messages;

class SensorRepresentationBusController {
    public static SensorRepresentationBusController Instance
    {
        get
        {
            if (_instance == null) {
                _instance = new SensorRepresentationBusController();
            }
            return _instance;
        }
    }
    private static SensorRepresentationBusController _instance;

    public List<SensorRepresentationBus> SensorRepresentationsBusses { get; private set; }

    public SensorRepresentationBusController() {
        SensorRepresentationsBusses = new List<SensorRepresentationBus>();
    }

    public void HandleData(ROSAgent sender, IRosMessage data)
    {
        foreach (SensorRepresentationBus bus in SensorRepresentationsBusses)
        {
            if (bus.ROSAgentType != sender.GetType()) continue;

            bus.HandleData(data);
        }
    }

    public void Register<T>(SensorRepresentation sensorRepresentation) where T : SensorRepresentationBus {
        for (int i = 0; i < SensorRepresentationsBusses.Count; i++) {
            SensorRepresentationBus b = SensorRepresentationsBusses[i];
            if (b.GetType().ToString().Contains(typeof(T).ToString())) {
                b.Register(sensorRepresentation);
                SensorRepresentationsBusses.Remove(b);
                SensorRepresentationsBusses.Add(b);
                return;
            }
        }
        T sensorBus = (T)Activator.CreateInstance(typeof(T), new object[] { });
        sensorBus.Register(sensorRepresentation);
        SensorRepresentationsBusses.Add(sensorBus);
        RobotVisualisation.Instance.StartAgent(sensorBus.ROSAgentType);
    }
}
