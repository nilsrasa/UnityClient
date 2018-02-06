using System;
using UnityEngine;

[Serializable]
public struct SorroundPictureMeta
{
    public GeoPointWGS84 GpsCoordinate;
    public Vector3 Orientation;
    public int CampusId;
}
