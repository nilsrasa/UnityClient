using ROSBridgeLib.std_msgs;
using SimpleJSON;

namespace ROSBridgeLib {
	namespace fiducial_msgs {
		public class FiducialTransformArrayMsg : ROSBridgeMsg
		{
		    public HeaderMsg _header;
		    public int _image_seq;
		    public FiducialTransformMsg[] _transforms;

            public FiducialTransformArrayMsg(JSONNode msg)
            {
                _header = new HeaderMsg(msg["header"]);
                _image_seq = int.Parse(msg["image_seq"]);

                _transforms = new FiducialTransformMsg[msg["transforms"].Count];
                for (int i = 0; i < _transforms.Length; i++)
                {
                    _transforms[i] = new FiducialTransformMsg(msg["transforms"][i]);
                }
            }

		    public FiducialTransformArrayMsg(HeaderMsg header, int image_seq, FiducialTransformMsg[] transforms)
		    {
		        _header = header;
		        _image_seq = image_seq;
		        _transforms = transforms;
		    }
			
			public static string getMessageType() {
				return "fiducial_msgs/FiducialTransformArray";
			}
			
			public override string ToString()
			{
			    return string.Format("fiducial_msgs/FiducialTransformArray [header={0}][image_seq={1}][transforms length={2}]", _header, _image_seq, _transforms.Length);
			}
					
			public override string ToYAMLString()
			{
			    string array = "[";
			    for (int i = 0; i < _transforms.Length; i++)
			    {
			        array = array + _transforms[i].ToYAMLString();
			        if (_transforms.Length - i <= 1)
			        array += ",";
			    }
			    array += "]";

			    return "{\"header\" : " + _header + ",\"image_seq\" :" + _image_seq + ",\"transforms\" :" + array + "}";
			}
		}
	}
}
