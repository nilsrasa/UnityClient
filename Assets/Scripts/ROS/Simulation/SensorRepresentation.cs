using System;
using UnityEngine;

public class SensorRepresentation : MonoBehaviour {

    public string SensorId { get; protected set; }
    public bool IsRunning { get; private set; }

    public void SetRunning(bool isRunning) {
        IsRunning = isRunning;
    }

    public virtual void HandleData(object data)
    {
        throw new NotImplementedException("Override me!");
    }

    protected int GetLevelOfValue(float value, float maxValue, int steps) {
        float stepSize = maxValue / steps;

        for (int i = 0; i < steps; i++) {
            if (value > i * stepSize && value < (i + 1) * stepSize) {
                return i;
            }
        }
        return steps - 1;
    }
}
