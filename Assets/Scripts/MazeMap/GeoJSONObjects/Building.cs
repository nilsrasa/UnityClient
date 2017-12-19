using System.Collections.Generic;

public class Building
{
    public int Id;
    public int BuildingId;
    public int CampusId;
    public string Name;
    public int AccessLevel;
    public List<Floor> Floors = new List<Floor>();

    public override string ToString() {
        return string.Format("Building - Id:{0}, BuildingId:{1}, CampusId:{2}, Name:{3}, AccessLevel:{4}, FloorCount:{5}", Id, BuildingId, CampusId, Name, AccessLevel, Floors.Count);
    }

    public Floor GetFloorByZ(int z)
    {
        foreach (Floor floor in Floors)
            if (floor.Z == z)
                return floor;
        return null;
    }
}

