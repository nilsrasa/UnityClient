using System.Diagnostics;
using ROSBridgeLib.geometry_msgs;
using SimpleJSON;

namespace ROSBridgeLib {
	namespace fiducial_msgs {
		public class FiducialTransformMsg : ROSBridgeMsg {
		    public int _fiducial_id;
		    public TransformMsg _transform;
		    public double _image_error;
		    public double _object_error;
		    public double _fiducial_area;

            public FiducialTransformMsg(JSONNode msg)
            {
                _fiducial_id = int.Parse(msg["fiducial_id"]);
                _transform = new TransformMsg(msg["transform"]);
                _image_error = float.Parse(msg["image_error"]);
                _object_error = float.Parse(msg["object_error"]);
                _fiducial_area = float.Parse(msg["fiducial_area"]);
			}

			public FiducialTransformMsg(int fiducial_id, TransformMsg transform, double image_error, double object_error, double fiducial_area)
			{
			    _fiducial_id = fiducial_id;
			    _transform = transform;
			    _image_error = image_error;
			    _object_error = object_error;
			    _fiducial_area = fiducial_area;
			}
			
			public static string getMessageType() {
				return "fiducial_msgs/FiducialTransform";
			}
			
			public override string ToString()
			{
			    return string.Format("fiducial_msgs/FiducialTransform [id={0}][transform={1}][image_error={2}][object_error={3}][fiducial_area={4}]", 
                    _fiducial_id, _transform, _image_error, _object_error, _fiducial_area);
			}
					
			public override string ToYAMLString()
			{
			    return "{\"fiducial_id\" : " + _fiducial_id + ",\"transform\" :" + _transform.ToYAMLString() + ",\"image_error\" :" + _image_error +",\"object_error\" :" + _object_error + ",\"fiducial_area\" :" + _fiducial_area + "}";
            }
		}
	}
}
