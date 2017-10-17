using System;
using Ros_CSharp;
using UnityEngine;

public class SensorVisualiser : MonoBehaviour {

    public string SensorId { get; protected set; }

    public virtual void HandleData(SensorData data)
    {
        throw new NotImplementedException("Override me!");
    }
}
