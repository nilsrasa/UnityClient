using System;
using UnityEngine;

[Serializable]
public class FiducialObject
{
    public int Id;
    public GeoPointWGS84 Position;
    public Vector3 Rotation;
    public FiducialData OriginalData;

    [Serializable]
    public struct FiducialData
    {
        public double X;
        public double Y;
        public double Z;
        public double RX;
        public double RY;
        public double RZ;
    }
}
