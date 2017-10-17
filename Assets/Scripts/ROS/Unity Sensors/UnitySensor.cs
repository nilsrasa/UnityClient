using UnityEngine;

public abstract class UnitySensor : MonoBehaviour
{
    public string SensorId { get; protected set; }
    public object SensorData { get; protected set; }
    public bool IsRunning { get; private set; }

    [SerializeField] protected int PollingRateMs;

    public void SetRunning(bool isRunning) {
        IsRunning = isRunning;
    }
}
