using System;
using System.Collections.Generic;

[Serializable]
public class ConfigFile
{
    public float FloorHeightAboveGround;
    public float FloorHeightBelowGround;
    public float FloorLineWidth;
    public int UtmZone;
    public bool IsUtmNorth;
    public List<ZeroFiducial> ZeroFiducials;
    public List<WaypointRoute> Routes;

    [Serializable]
    public struct WaypointRoute
    {
        public string Name;
        public List<WaypointController.Waypoint> Waypoints;
    }

    [Serializable]
    public struct ZeroFiducial
    {
        public int CampusId;
        public FiducialData FiducialData;
    }
}