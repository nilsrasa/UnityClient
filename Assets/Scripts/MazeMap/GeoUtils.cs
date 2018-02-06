using GeoAPI.CoordinateSystems.Transformations;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using UnityEngine;

public static class GeoUtils {
    public static bool UtmOriginSet;

    static GeoPointUTM _utmOrigin;
    private static readonly ICoordinateTransformation _transformationFromWGS84ToMercator;
    private static readonly ICoordinateTransformation _transformationFromMercatorToWGS84;
    private static readonly ICoordinateTransformation _transformationFromWGS84ToUTM;
    private static readonly ICoordinateTransformation _transformationFromUTMToUGS84;
    private static readonly ICoordinateTransformation _transformationFromUTMToMercator;
    private static readonly ICoordinateTransformation _transformationFromMercatorToUTM;

    private static int _utmZone;
    private static bool _utmNorth;

    static GeoUtils()
    {
        _utmZone = ConfigManager.ConfigFile.UtmZone;
        _utmNorth = ConfigManager.ConfigFile.IsUtmNorth;
        CoordinateTransformationFactory _ctf = new CoordinateTransformationFactory();
        _transformationFromWGS84ToUTM = _ctf.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, ProjectedCoordinateSystem.WGS84_UTM(_utmZone, _utmNorth));
        _transformationFromWGS84ToMercator = _ctf.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, ProjectedCoordinateSystem.WebMercator);
        _transformationFromMercatorToWGS84 = _ctf.CreateFromCoordinateSystems(ProjectedCoordinateSystem.WebMercator, GeographicCoordinateSystem.WGS84);
        _transformationFromUTMToUGS84 = _ctf.CreateFromCoordinateSystems(ProjectedCoordinateSystem.WGS84_UTM(_utmZone, _utmNorth), GeographicCoordinateSystem.WGS84);
        _transformationFromUTMToMercator = _ctf.CreateFromCoordinateSystems(ProjectedCoordinateSystem.WGS84_UTM(_utmZone, _utmNorth), ProjectedCoordinateSystem.WebMercator);
        _transformationFromMercatorToUTM = _ctf.CreateFromCoordinateSystems(ProjectedCoordinateSystem.WebMercator, ProjectedCoordinateSystem.WGS84_UTM(_utmZone, _utmNorth));

        _utmOrigin = default(GeoPointUTM);
    }

    public static GeoPointWGS84 ToWGS84(this GeoPointMercator geoPoint) 
    {
        GeoPointWGS84 wgs84 = new GeoPointWGS84(_transformationFromMercatorToWGS84.MathTransform.Transform(geoPoint.ToArray()))
            {
                altitude = geoPoint.altitude
            };
        return wgs84;
    }

    public static GeoPointWGS84 ToWGS84(this GeoPointUTM geoPoint)
    {
        GeoPointWGS84 wgs84 = new GeoPointWGS84(_transformationFromUTMToUGS84.MathTransform.Transform(geoPoint.ToArray()))
            {
                altitude = geoPoint.altitude
            };
        return wgs84;
    }

    public static GeoPointUTM ToUTM(this GeoPointWGS84 geoPoint)
    {
        GeoPointUTM utm = new GeoPointUTM(_transformationFromWGS84ToUTM.MathTransform.Transform(geoPoint.ToArray()))
            {
                altitude = geoPoint.altitude
            };
        return utm;
    }

    public static GeoPointUTM ToUTM(this GeoPointMercator geoPoint)
    {
        GeoPointUTM utm = new GeoPointUTM(_transformationFromMercatorToUTM.MathTransform.Transform(geoPoint.ToArray()))
            {
                altitude = geoPoint.altitude
            };
        return utm;
    }

    public static GeoPointMercator ToMercator(this GeoPointUTM geoPoint)
    {
        GeoPointMercator mercator = new GeoPointMercator(_transformationFromUTMToMercator.MathTransform.Transform(geoPoint.ToArray()))
            {
                altitude = geoPoint.altitude
            };
        return mercator;
    }

    public static GeoPointMercator ToMercator(this GeoPointWGS84 geoPoint)
    {
        GeoPointMercator mercator = new GeoPointMercator(_transformationFromWGS84ToMercator.MathTransform.Transform(geoPoint.ToArray()))
            {
                altitude = geoPoint.altitude
            };
        return mercator;
    }

    public static GeoPointUTM UtmOrigin
    {
        get { return _utmOrigin; }
        set
        {
            _utmOrigin = value;
            UtmOriginSet = true;
        }
    }

    public static GeoPointUTM ToUTM(this Vector3 position)
    {
        return _utmOrigin + new GeoPointUTM {
            latitude = position.z,
            longitude = position.x,
            altitude = position.y,
        };
    }

    public static Vector3 ToUnity(this GeoPointUTM geoPoint)
    {
        GeoPointUTM ucs = geoPoint - _utmOrigin;
        return new Vector3(x: (float)ucs.longitude, y: (float)ucs.altitude, z: (float)ucs.latitude);
    }
}
