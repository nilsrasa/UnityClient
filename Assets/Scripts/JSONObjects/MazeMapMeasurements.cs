using System;
using System.Collections.Generic;

[Serializable]
public class MazeMapMeasurements
{
    public List<CampusMeasurements> CampusMeasurements;

    [NonSerialized]
    private Dictionary<int, int> _register = new Dictionary<int, int>();
    [NonSerialized]
    private int _lastIndex = 0;
    public CampusMeasurements GetCampusMeasurement(int campusId)
    {
        if (_register.ContainsKey(campusId))
            return CampusMeasurements[_register[campusId]];
        else if (_register.Count == CampusMeasurements.Count)
        {
            return null;
        }
        else
        {
            for (int i = _lastIndex; i < CampusMeasurements.Count; i++)
            {
                _register[CampusMeasurements[i].CampusId] = i;
                _lastIndex = i;
                if (CampusMeasurements[i].CampusId == campusId)
                    return CampusMeasurements[i];
            }
            return null;
        }
    }
}
