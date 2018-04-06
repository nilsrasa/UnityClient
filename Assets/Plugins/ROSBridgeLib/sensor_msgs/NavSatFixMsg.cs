using System.Collections;
using System.IO;
using System.Text;
using SimpleJSON;
using ROSBridgeLib.std_msgs;
using UnityEngine;

/**
 * Define a compressed image message. Note: the image is assumed to be in Base64 format.
 * Which seems to be what is normally found in json strings. Documentation. Got to love it.
 * 
 * @author Michael Jenkin, Robert Codd-Downey and Andrew Speers
 * @version 3.1
 */

namespace ROSBridgeLib {
	namespace sensor_msgs {
		public class NavSatFixMsg : ROSBridgeMsg {
		    public HeaderMsg _header; //woo
		    public NavSatStatusMsg _status;
		    public double _latitude; //woo
		    public double _longitude; //woo
		    public double _altitude; //woo
		    public double[] _position_covariance = new double[9];
		    public byte _position_covariance_type; //woo


            public NavSatFixMsg(JSONNode msg) {
				_header = new HeaderMsg (msg ["header"]);
                _status = new NavSatStatusMsg(msg["status"]);
                _latitude = double.Parse(msg["latitude"]);
                _longitude = double.Parse(msg["longitude"]);
                _altitude = double.Parse(msg["altitude"]);

                _position_covariance = new double[msg["position_covariance"].Count];
                for (int i = 0; i < _position_covariance.Length; i++)
                {
                    _position_covariance[i] = double.Parse(msg["position_covariance"][i]);
                }

                _position_covariance_type = byte.Parse(msg["position_covariance_type"]);
            }
			
			public NavSatFixMsg(HeaderMsg header, NavSatStatusMsg status, double latitude, double longitude, double altitude, double[] position_covariance, byte position_covariance_type) {
				_header = header;
			    _status = status;
			    _latitude = latitude;
			    _longitude = longitude;
			    _altitude = altitude;
			    _position_covariance = position_covariance;
			    _position_covariance_type = position_covariance_type;
			}

		    public NavSatFixMsg(double latitude, double longitude, double altitude)
		    {
		        _header = new HeaderMsg(0, new TimeMsg(0,0), "0");
                _status = new NavSatStatusMsg(0, 0);
		        _latitude = latitude;
		        _longitude = longitude;
		        _altitude = altitude;
                _position_covariance = new double[9];
		        _position_covariance_type = 0;
		    }
			
			public static string GetMessageType() {
				return "sensor_msgs/NavSatFix";
			}
			
			public override string ToString()
			{
			    return string.Format(
			        "sensor_msgs/NavSatFix [header={0}][status={1}][latitude={2}][longitude={3}][altitude={4}][position_covariance_type={5}]", 
                    _header, _status, _latitude, _longitude, _altitude, _position_covariance_type);
			}

		    public override string ToYAMLString()
		    {
		        string array = "[";
		        for (int i = 0; i < _position_covariance.Length; i++)
		        {
		            array = array + _position_covariance[i];
		            if (i < _position_covariance.Length - 1)
		                array += ",";
		        }
		        array += "]";

		        return "{\"header\" : " + _header.ToYAMLString() + ",\"status\" :" + _status.ToYAMLString() + ",\"latitude\" :" + _latitude + ",\"longitude\" :" + _longitude + ",\"altitude\" :" 
                    + _altitude + ",\"position_covariance\" :" + array + ",\"position_covariance_type\" :" + _position_covariance_type + "}";
		    }
        }
	}
}
