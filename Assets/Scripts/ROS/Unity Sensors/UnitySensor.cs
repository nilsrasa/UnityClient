using UnityEngine;

public abstract class UnitySensor : MonoBehaviour
{
    public string SensorId { get; protected set; }
    public bool IsRunning { get; private set; }

    public void SetRunning(bool isRunning) {
        IsRunning = isRunning;
    }
}
