using System;
using UnityEngine;

[Serializable]
public class FiducialObject
{
    public int Id;
    public GeoPointWGS84 Position;
    public GeoRotation Rotation;
}
