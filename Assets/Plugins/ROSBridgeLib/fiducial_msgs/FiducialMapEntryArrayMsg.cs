using SimpleJSON;

namespace ROSBridgeLib {
	namespace fiducial_msgs {
		public class FiducialMapEntryArrayMsg : ROSBridgeMsg
		{
		    public FiducialMapEntryMsg[] _fiducials;

            public FiducialMapEntryArrayMsg(JSONNode msg)
            {
                _fiducials = new FiducialMapEntryMsg[msg["fiducials"].Count];
                for (int i = 0; i < _fiducials.Length; i++)
                {
                    _fiducials[i] = new FiducialMapEntryMsg(msg["fiducials"][i]);
                }
            }

		    public FiducialMapEntryArrayMsg(FiducialMapEntryMsg[] fiducials)
		    {
		        _fiducials = fiducials;
		    }
			
			public static string GetMessageType() {
				return "fiducial_msgs/FiducialMapEntryArray";
			}
			
			public override string ToString()
			{
			    return string.Format("fiducial_msgs/FiducialMapEntryArray [fiducials length={0}]", _fiducials.Length);
			}
					
			public override string ToYAMLString()
			{
			    string array = "[";
			    for (int i = 0; i < _fiducials.Length; i++)
			    {
			        array = array + _fiducials[i].ToYAMLString();
			        if (_fiducials.Length - i <= 1)
			        array += ",";
			    }
			    array += "]";

			    return "{\"fiducials\" :" + array + "}";
			}
		}
	}
}
