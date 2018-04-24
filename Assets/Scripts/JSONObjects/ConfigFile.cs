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
    public FiducialData ZeroFiducial;
    public List<WaypointRoute> Routes;

    [Serializable]
    public struct WaypointRoute
    {
        public string Name;
        public List<GeoPointWGS84> Points;
    }
}