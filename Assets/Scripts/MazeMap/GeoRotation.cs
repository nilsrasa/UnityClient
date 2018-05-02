using System;

[Serializable]
public struct GeoRotation
{
    public double heading;
    public double north;
    public double east;

    public GeoRotation(double heading, double north, double east)
    {
        this.heading = heading;
        this.north = north;
        this.east = east;
    }

    public double[] ToArray()
    {
        return new[] {heading, north, east};
    }

    public static GeoRotation operator +(GeoRotation a, GeoRotation b)
    {
        return new GeoRotation
        {
            heading = a.heading + b.heading,
            north = a.north + b.north,
            east = a.east + b.east,
        };
    }

    public static GeoRotation operator -(GeoRotation a, GeoRotation b)
    {
        return new GeoRotation
        {
            heading = a.heading - b.heading,
            north = a.north - b.north,
            east = a.east - b.east,
        };
    }

    public override string ToString()
    {
        return String.Format("heading: {0}, north: {1}, east: {2}", heading, north, east);
    }
}