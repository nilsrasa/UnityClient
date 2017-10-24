using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Messages;
using Ros_CSharp;
using SimpleJSON;
using UnityEngine;
using String = Messages.std_msgs.String;

public class UltrasoundBus : SensorBus {

    public UltrasoundBus()
    {
        ROSAgentType = typeof(ROSUltrasound);
    }

    public override IRosMessage GetSensorData()
    {
        List<Sim_UltrasoundSensor> ultrasoundSensors = Sensors.Cast<Sim_UltrasoundSensor>().ToList();
        //      {"p0":31, "p1":412, "p2":54, "p3":511}
        JSONObject container = new JSONObject();
        foreach (Sim_UltrasoundSensor sensor in ultrasoundSensors)
        {
            container.Add(sensor.SensorId, new JSONNumber(sensor.GetSensorData()));
        }
        return new String(container.ToString());
    }
}
