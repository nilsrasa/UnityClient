using System.Globalization;
using SimpleJSON;

namespace ROSBridgeLib
{
    namespace sensor_msgs
    {
        public class RegionOfInterestMsg : ROSBridgeMsg
        {
            private uint _x_offset;
            private uint _y_offset;
            private uint _height;
            private uint _width;
            private bool _do_rectify;

            public RegionOfInterestMsg(JSONNode msg)
            {
                _x_offset = uint.Parse(msg["x_offset"], CultureInfo.InvariantCulture);
                _y_offset = uint.Parse(msg["x_offset"], CultureInfo.InvariantCulture);
                _height = uint.Parse(msg["x_offset"], CultureInfo.InvariantCulture);
                _width = uint.Parse(msg["x_offset"], CultureInfo.InvariantCulture);
                _do_rectify = bool.Parse(msg["do_rectify"]);
            }

            public RegionOfInterestMsg(uint x_offset, uint y_offset, uint height, uint width, bool do_rectify)
            {
                _x_offset = x_offset;
                _y_offset = y_offset;
                _height = height;
                _width = width;
                _do_rectify = do_rectify;
            }

            public static string GetMessageType()
            {
                return "sensor_msgs/RegionOfInterest";
            }

            public override string ToString()
            {
                return "RegionOfInterest [_x_offset=" + _x_offset + ",  _y_offset=" + _y_offset + ", _height " + _height + ", _width " + _width + ", _do_rectify " + _do_rectify +"]";
            }

            public override string ToYAMLString()
            {
                return "{\"x_offset\" : " + _x_offset.ToString(CultureInfo.InvariantCulture)
                       + ", \"y_offset\" : " + _y_offset.ToString(CultureInfo.InvariantCulture)
                       + ", \"height\" : " + _height.ToString(CultureInfo.InvariantCulture)
                       + ", \"width\" : " + _width.ToString(CultureInfo.InvariantCulture)
                       + ", \"do_rectify\" : " + _do_rectify.ToString().ToLower() + "}";
            }
        }
    }
}