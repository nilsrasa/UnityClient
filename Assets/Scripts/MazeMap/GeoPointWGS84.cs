using System;

[Serializable]
public struct GeoPointWGS84
{
    public double longitude;
    public double latitude;
    public double altitude;

    public GeoPointWGS84(double[] point)
    {
        longitude = point[0];
        latitude = point[1];
        altitude = 0;
    }

    public double[] ToArray()
    {
        return new[] { longitude, latitude };
    }

    public static GeoPointWGS84 operator +(GeoPointWGS84 a, GeoPointWGS84 b)
    {
        return new GeoPointWGS84 {
            longitude = a.longitude + b.longitude,
            latitude = a.latitude + b.latitude,
            altitude = a.altitude + b.altitude,
        };
    }

    public static GeoPointWGS84 operator -(GeoPointWGS84 a, GeoPointWGS84 b) {
        return new GeoPointWGS84 {
            longitude = a.longitude - b.longitude,
            latitude = a.latitude - b.latitude,
            altitude = a.altitude - b.altitude,
        };
    }

    public override string ToString()
    {
        return String.Format("long: {0}, lat: {1}, alt: {2}", longitude, latitude, altitude);
    }
}
