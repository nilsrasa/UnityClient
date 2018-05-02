using SimpleJSON;

/**
 * Define a geometry_msgs point message. This has been hand-crafted from the corresponding
 * geometry_msgs message file.
 * 
 * @author Michael Jenkin, Robert Codd-Downey, Andrew Speers and Miquel Massot Campos
 */

namespace ROSBridgeLib
{
    namespace geometry_msgs
    {
        public class PointMsg : ROSBridgeMsg
        {
            private double _x, _y, _z;

            public PointMsg(JSONNode msg)
            {
                //Debug.Log ("PointMsg with " + msg.ToString());
                _x = double.Parse(msg["x"]);
                _y = double.Parse(msg["y"]);
                _z = double.Parse(msg["z"]);
            }

            public PointMsg(double x, double y, double z)
            {
                _x = x;
                _y = y;
                _z = z;
            }

            public static string GetMessageType()
            {
                return "geometry_msgs/Point";
            }

            public double GetX()
            {
                return _x;
            }

            public double GetY()
            {
                return _y;
            }

            public double GetZ()
            {
                return _z;
            }

            public override string ToString()
            {
                return "geometry_msgs/Point [x=" + _x + ",  y=" + _y + ", z=" + _z + "]";
            }

            public override string ToYAMLString()
            {
                return "{\"x\": " + _x + ", \"y\": " + _y + ", \"z\": " + _z + "}";
            }
        }
    }
}