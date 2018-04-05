using ROSBridgeLib.std_msgs;
using SimpleJSON;

namespace ROSBridgeLib {
	namespace fiducial_msgs {
		public class FiducialArrayMsg : ROSBridgeMsg
		{
		    public HeaderMsg _header;
		    public int _image_seq;
		    public FiducialMsg[] _fiducials;

            public FiducialArrayMsg(JSONNode msg)
            {
                _header = new HeaderMsg(msg["header"]);
                _image_seq = int.Parse(msg["image_seq"]);

                _fiducials = new FiducialMsg[msg["fiducials"].Count];
                for (int i = 0; i < _fiducials.Length; i++)
                {
                    _fiducials[i] = new FiducialMsg(msg["fiducials"][i]);
                }
            }

		    public FiducialArrayMsg(HeaderMsg header, int image_seq, FiducialMsg[] fiducials)
		    {
		        _header = header;
		        _image_seq = image_seq;
		        _fiducials = fiducials;
		    }
			
			public static string GetMessageType() {
				return "fiducial_msgs/FiducialArray";
			}
			
			public override string ToString()
			{
			    return string.Format("fiducial_msgs/FiducialArray [header={0}][image_seq={1}][fiducials length={2}]", _header.ToString(), _image_seq, _fiducials.Length);
			}
					
			public override string ToYAMLString()
			{
			    string array = "[";
			    for (int i = 0; i < _fiducials.Length; i++)
			    {
			        array = array + _fiducials[i].ToYAMLString();
			        if (i < _fiducials.Length - 1)
			            array += ",";
			    }
			    array += "]";

			    return "{\"header\" : " + _header.ToYAMLString() + ",\"image_seq\" :" + _image_seq + ",\"fiducials\" :" + array + "}";
			}
		}
	}
}
