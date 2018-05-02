using System;

[Serializable]
public struct GeoPointMercator
{
    public double longitude;
    public double latitude;
    public double altitude;

    public GeoPointMercator(double[] point)
    {
        longitude = point[0];
        latitude = point[1];
        altitude = 0;
    }

    public double[] ToArray()
    {
        return new[] {longitude, latitude};
    }

    public static GeoPointMercator operator +(GeoPointMercator a, GeoPointMercator b)
    {
        return new GeoPointMercator
        {
            longitude = a.longitude + b.longitude,
            latitude = a.latitude + b.latitude,
            altitude = a.altitude + b.altitude,
        };
    }

    public static GeoPointMercator operator -(GeoPointMercator a, GeoPointMercator b)
    {
        return new GeoPointMercator
        {
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