using System;

[Serializable]
public class SensorDataDTO
{
    public SensorData[] Data;

    public SensorDataDTO(SensorData[] data)
    {
        Data = data;
    }
}
