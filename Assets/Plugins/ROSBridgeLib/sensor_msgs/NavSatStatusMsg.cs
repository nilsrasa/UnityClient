using SimpleJSON;

/**
 * Define a compressed image message. Note: the image is assumed to be in Base64 format.
 * Which seems to be what is normally found in json strings. Documentation. Got to love it.
 * 
 * @author Michael Jenkin, Robert Codd-Downey and Andrew Speers
 * @version 3.1
 */

namespace ROSBridgeLib {
	namespace sensor_msgs {
		public class NavSatStatusMsg : ROSBridgeMsg
		{
		    public sbyte _status;
		    public ushort _service;
			
			public NavSatStatusMsg(JSONNode msg)
			{
			    _status = sbyte.Parse(msg["status"]);
			    _service = ushort.Parse(msg["service"]);
			}
			
			public NavSatStatusMsg(sbyte status, ushort service)
			{
			    _status = status;
			    _service = service;
			}

			public static string GetMessageType() {
				return "sensor_msgs/NavSatStatus";
			}
			
			public override string ToString()
			{
			    return string.Format("sensor_msgs/NavSatStatus [status={0}][service={1}]", _status, _service);
            }
			
			public override string ToYAMLString() {
				return "{\"status\" : " + _status + ", \"service\" : " + _service + "}";
			}
		}
	}
}
