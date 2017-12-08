using GeoAPI.CoordinateSystems.Transformations;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using UnityEngine;

namespace Assets.Scripts {
    public static class GeoUtils {
        public static bool MercatorOriginSet;

        static GeoPointMercator _mercatorOrigin;
        private static readonly ICoordinateTransformation _transformationFromWGS84ToMercator;
        private static readonly ICoordinateTransformation _transformationFromMercatorToWGS84;
        private static readonly ICoordinateTransformation _transformationFromWGS84ToUTM;
        private static readonly ICoordinateTransformation _transformationFromUTMToUGS84;

        private const int UTM_ZONE = 34;
        private const bool UTM_NORTH = true;

        static GeoUtils()
        {
            CoordinateTransformationFactory _ctf = new CoordinateTransformationFactory();
            _transformationFromWGS84ToMercator = _ctf.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, ProjectedCoordinateSystem.WebMercator);
            _transformationFromMercatorToWGS84 = _ctf.CreateFromCoordinateSystems(ProjectedCoordinateSystem.WebMercator, GeographicCoordinateSystem.WGS84);
            _transformationFromWGS84ToUTM = _ctf.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, ProjectedCoordinateSystem.WGS84_UTM(UTM_ZONE, UTM_NORTH));
            _transformationFromUTMToUGS84 = _ctf.CreateFromCoordinateSystems(ProjectedCoordinateSystem.WGS84_UTM(UTM_ZONE, UTM_NORTH), GeographicCoordinateSystem.WGS84);

            _mercatorOrigin = default(GeoPointMercator);
        }

        public static GeoPointWGS84 ToWGS84(this GeoPointMercator geoPoint) 
        {
            return new GeoPointWGS84(_transformationFromMercatorToWGS84.MathTransform.Transform(geoPoint.ToArray()));
        }

        public static GeoPointWGS84 ToWGS84(this GeoPointUTM geoPoint)
        {
            return new GeoPointWGS84(_transformationFromUTMToUGS84.MathTransform.Transform(geoPoint.ToArray()));
        }

        public static GeoPointUTM ToUTM(this GeoPointWGS84 geoPoint)
        {
            return new GeoPointUTM(_transformationFromWGS84ToUTM.MathTransform.Transform(geoPoint.ToArray()));
        }

        public static GeoPointMercator ToMercator(this GeoPointWGS84 geoPoint)
        {
            return new GeoPointMercator(_transformationFromWGS84ToMercator.MathTransform.Transform(geoPoint.ToArray()));
        }

        public static GeoPointMercator MercatorOrigin
        {
            get { return _mercatorOrigin; }
            set
            {
                _mercatorOrigin = value;
                MercatorOriginSet = true;
            }
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
