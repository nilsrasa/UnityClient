using SimpleJSON;

namespace ROSBridgeLib {
	namespace fiducial_msgs {
		public class FiducialMapEntryArrayMsg : ROSBridgeMsg
		{
		    public FiducialMapEntryMsg[] Fiducials;

            public FiducialMapEntryArrayMsg(JSONNode msg)
            {
                Fiducials = new FiducialMapEntryMsg[msg["fiducials"].Count];
                for (int i = 0; i < Fiducials.Length; i++)
                {
                    Fiducials[i] = new FiducialMapEntryMsg(msg["fiducials"][i]);
                }
            }

		    public FiducialMapEntryArrayMsg(FiducialMapEntryMsg[] fiducials)
		    {
		        Fiducials = fiducials;
		    }
			
			public static string GetMessageType() {
				return "fiducial_msgs/FiducialMapEntryArray";
			}
			
			public override string ToString()
			{
			    return string.Format("fiducial_msgs/FiducialMapEntryArray [fiducials length={0}]", Fiducials.Length);
			}
					
			public override string ToYAMLString()
			{
			    string array = "[";
			    for (int i = 0; i < Fiducials.Length; i++)
			    {
			        array = array + Fiducials[i].ToYAMLString();
			        if (i < Fiducials.Length - 1)
			        array += ",";
			    }
			    array += "]";

			    return "{\"fiducials\" :" + array + "}";
			}
		}
	}
}
