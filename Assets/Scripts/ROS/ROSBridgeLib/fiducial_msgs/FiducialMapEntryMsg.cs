using SimpleJSON;

namespace ROSBridgeLib
{
    namespace fiducial_msgs
    {
        public class FiducialMapEntryMsg : ROSBridgeMsg
        {
            public int _fiducial_id;
            public double _x;
            public double _y;
            public double _z;
            public double _rx;
            public double _ry;
            public double _rz;

            public FiducialMapEntryMsg(JSONNode msg)
            {
                _fiducial_id = int.Parse(msg["fiducial_id"]);
                _x = double.Parse(msg["x"]);
                _y = double.Parse(msg["y"]);
                _z = double.Parse(msg["z"]);
                _rx = double.Parse(msg["rx"]);
                _ry = double.Parse(msg["ry"]);
                _rz = double.Parse(msg["rz"]);
            }

            public FiducialMapEntryMsg(int fiducial_id, double x, double y, double z, double rx, double ry, double rz)
            {
                _fiducial_id = fiducial_id;
                _x = x;
                _y = y;
                _z = z;
                _rx = rx;
                _ry = ry;
                _rz = rz;
            }

            public static string GetMessageType()
            {
                return "fiducial_msgs/FiducialMapEntry";
            }

            public override string ToString()
            {
                return string.Format("fiducial_msgs/FiducialMapEntry [fiducial_id={0}][x={1}, y={2}, z={3}][rx={4}, ry={5}, rz={6}]", _fiducial_id, _x, _y, _z, _rx, _ry, _rz);
            }

            public override string ToYAMLString()
            {
                return "{\"fiducial_id\" : " + _fiducial_id + ",\"x\" :" + _x + ",\"y\" :" + _y + ",\"z\" :" + _z + ",\"rx\" :" + _rx + ",\"ry\" :" + _ry + ",\"rz\" :" + _rz + "}";
            }
        }
    }
}