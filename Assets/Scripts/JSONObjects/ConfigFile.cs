using System;
using System.Collections.Generic;

[Serializable]
public class ConfigFile
{
    public string RosMasterUri;
    public int RosMasterPort;
    public float WaypointDistanceThreshold;
    public float MaxLinearSpeed;
    public float MaxAngularSpeed;
    public float ControlParameterRho;
    public float ControlParameterRoll;
    public float ControlParameterPitch;
    public float ControlParameterYaw;
    public float FloorHeightAboveGround;
    public float FloorHeightBelowGround;
    public float FloorLineWidth;
    public int UtmZone;
    public bool IsUtmNorth;
    public Fiducial ZeroFiducial;
    public List<WaypointRoute> Routes;

    [Serializable]
    public struct WaypointRoute
    {
        public string Name;
        public List<GeoPointWGS84> Points;
    }
}