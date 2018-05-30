using System;
using System.Globalization;
using ROSBridgeLib.std_msgs;
using SimpleJSON;

namespace ROSBridgeLib
{
    namespace sensor_msgs
    {
        public class CameraInfoMsg : ROSBridgeMsg
        {
            private HeaderMsg _header;
            private uint _height;
            private uint _width;
            private string _distortion_model;
            private double[] _d;
            private double[] _k;
            private double[] _r;
            private double[] _p;
            private uint _binning_x;
            private uint _binning_y;
            private RegionOfInterestMsg _roi;

            public CameraInfoMsg(JSONNode msg)
            {
                _header = new HeaderMsg(msg["header"]);
                _height = uint.Parse(msg["height"]);
                _distortion_model = msg["distortion_model"];
                _d = new double[msg["D"].Count];
                _k = new double[msg["K"].Count];
                _r = new double[msg["R"].Count];
                _p = new double[msg["P"].Count];
                for (int i = 0; i < msg["D"].Count; i++)
                {
                    _d[i] = double.Parse(msg["D"][i], CultureInfo.InvariantCulture);
                }
                for (int i = 0; i < msg["K"].Count; i++)
                {
                    _k[i] = double.Parse(msg["K"][i], CultureInfo.InvariantCulture);
                }
                for (int i = 0; i < msg["R"].Count; i++)
                {
                    _r[i] = double.Parse(msg["R"][i], CultureInfo.InvariantCulture);
                }
                for (int i = 0; i < msg["P"].Count; i++)
                {
                    _p[i] = double.Parse(msg["P"][i], CultureInfo.InvariantCulture);
                }
                _binning_x = uint.Parse(msg["binning_x"], CultureInfo.InvariantCulture);
                _binning_y = uint.Parse(msg["binning_y"], CultureInfo.InvariantCulture);
                _roi = new RegionOfInterestMsg(msg["roi"]);
            }

            public CameraInfoMsg(HeaderMsg header, uint height, uint width, string distortion_model, double[] d, double[] k, double[] r, double[] p, uint binning_x, uint binning_y, RegionOfInterestMsg roi)
            {
                _header = header;
                _height = height;
                _width = width;
                _distortion_model = distortion_model;
                _d = d;
                _k = k;
                _r = r;
                _p = p;
                _binning_x = binning_x;
                _binning_y = binning_y;
                _roi = roi;
            }

            public static string GetMessageType()
            {
                return "sensor_msgs/CameraInfo";
            }

            public override string ToString()
            {
               return "CameraInfo [Header " + _header.ToString() + "]";
            }

            public override string ToYAMLString()
            {
                string dArray = "[";
                string kArray = "[";
                string rArray = "[";
                string pArray = "[";

                for (int i = 0; i < _d.Length; i++)
                {
                    dArray = dArray + _d[i];
                    if (i < _d.Length - 1)
                        dArray += ",";
                }
                dArray += "]";

                for (int i = 0; i < _k.Length; i++)
                {
                    kArray = kArray + _k[i];
                    if (i < _k.Length - 1)
                        kArray += ",";
                }
                kArray += "]";

                for (int i = 0; i < _r.Length; i++)
                {
                    rArray = rArray + _r[i];
                    if (i < _r.Length - 1)
                        rArray += ",";
                }
                rArray += "]";

                for (int i = 0; i < _p.Length; i++)
                {
                    pArray = pArray + _p[i];
                    if (i < _p.Length - 1)
                        pArray += ",";
                }
                pArray += "]";

                return "{\"header\" : " + _header.ToYAMLString() + ", \"height\" : " + _height + ", \"width\" : " + _width +
                    ", \"distortion_model\" : \"" + _distortion_model + "\", \"D\" : " + dArray + ", \"K\" : " + kArray +
                       ", \"R\" : " + rArray + ", \"P\" : " + pArray + ", \"binning_x\" : " + _binning_x + 
                       ", \"binning_y\" : " + _binning_y + ", \"roi\" : " + _roi.ToYAMLString() + "}";
            }
        }
    }
}