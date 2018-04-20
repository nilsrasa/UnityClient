using System;
using System.Collections.Generic;

/// <summary>
/// Handles collecting and managing visual sensor representations
/// </summary>
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

    /// <summary>
    /// Distributes ROS messages to relevant sensor representation busses
    /// </summary>
    public void HandleData(ROSAgent sender, ROSBridgeMsg data)
    {
        foreach (SensorRepresentationBus bus in SensorRepresentationsBusses)
        {
            if (bus.ROSAgentType != sender.GetType()) continue;

            bus.HandleData(data);
        }
    }

    //TODO: Change to subscriber/publisher pattern, using topics as keys
    public void Register<T>(SensorRepresentation sensorRepresentation) where T : SensorRepresentationBus {
        /*
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
        */
    }
}
