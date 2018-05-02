using System.Collections.Generic;
using UnityEngine;

public class Building
{
    public int Id;
    public int BuildingId;
    public int CampusId;
    public string Name;
    public int AccessLevel;
    public Dictionary<int, Floor> Floors = new Dictionary<int, Floor>();
    public Transform RenderedModel;

    public override string ToString()
    {
        return string.Format("Building - Id:{0}, BuildingId:{1}, CampusId:{2}, Name:{3}, AccessLevel:{4}, FloorCount:{5}", Id, BuildingId, CampusId, Name, AccessLevel, Floors.Count);
    }
}