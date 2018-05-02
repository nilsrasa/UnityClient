using System;
using System.Collections.Generic;

[Serializable]
public class CampusMeasurements
{
    public int CampusId;
    public List<BuildingMeasurements> BuildingMeasurements;

    [NonSerialized] private Dictionary<int, int> _register = new Dictionary<int, int>();
    [NonSerialized] private int _lastIndex = 0;

    public BuildingMeasurements GetBuildingMeasurement(int buildingId)
    {
        if (_register.ContainsKey(buildingId))
            return BuildingMeasurements[_register[buildingId]];
        else if (_register.Count == BuildingMeasurements.Count)
        {
            return null;
        }
        else
        {
            for (int i = _lastIndex; i < BuildingMeasurements.Count; i++)
            {
                _register[BuildingMeasurements[i].BuildingId] = i;
                _lastIndex = i;
                if (BuildingMeasurements[i].BuildingId == buildingId)
                    return BuildingMeasurements[i];
            }
            return null;
        }
    }
}