using System;

[Serializable]
public class BuildingMeasurements
{
    public int BuildingId;
    public float BuildingGroundHeight;
    public SerializableDictionaryIntFloat FloorHeights;
}