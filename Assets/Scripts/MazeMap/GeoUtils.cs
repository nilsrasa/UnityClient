using GeoAPI.CoordinateSystems.Transformations;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using UnityEngine;

namespace Assets.Scripts {
    public static class GeoUtils {
        public static bool MercatorOriginSet;

        static GeoPointMercator _mercatorOrigin;
        static readonly ICoordinateTransformation _transformationFromWGS84ToMercator;
        static readonly ICoordinateTransformation _transformationFromMercatorToWGS84;

        static GeoUtils()
        {
            CoordinateTransformationFactory _ctf = new CoordinateTransformationFactory();
            _transformationFromWGS84ToMercator = _ctf.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, ProjectedCoordinateSystem.WebMercator);
            _transformationFromMercatorToWGS84 = _ctf.CreateFromCoordinateSystems(ProjectedCoordinateSystem.WebMercator, GeographicCoordinateSystem.WGS84);
            _mercatorOrigin = default(GeoPointMercator);
        }

        public static GeoPointWGS84 ToWGS84(this GeoPointMercator geoPoint) {
            return new GeoPointWGS84(_transformationFromMercatorToWGS84.MathTransform.Transform(geoPoint.ToArray()));
        }

        public static GeoPointMercator ToMercator(this GeoPointWGS84 geoPoint)
        {
            return new GeoPointMercator(_transformationFromWGS84ToMercator.MathTransform.Transform(geoPoint.ToArray()));
        }

        public static GeoPointMercator MercatorOrigin
        {
            get { return _mercatorOrigin; }
            set { _mercatorOrigin = value; }
        }

        public static GeoPointMercator ToMercator(this Vector3 position)
        {
            return _mercatorOrigin + new GeoPointMercator {
                latitude = position.z,
                longitude = position.x,
                altitude = position.y,
            };
        }

        public static Vector3 ToUnity(this GeoPointMercator geoPoint)
        {
            GeoPointMercator ucs = geoPoint - _mercatorOrigin;
            return new Vector3(x: (float)ucs.longitude, y: (float)ucs.altitude, z: (float)ucs.latitude);
        }
    }
}
