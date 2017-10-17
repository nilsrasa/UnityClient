using System;
using UnityEngine;

[Serializable]
public class SensorData
{
    public string SensorId;
    public string Value;
    public Vector3 LocalPosition;
    public Vector3 LocalRotation;

    public SensorData(string id, string value, Vector3 localPosition, Vector3 localRotation)
    {
        SensorId = id;
        Value = value;
        LocalPosition = localPosition;
        LocalRotation = localRotation;
    }
}
