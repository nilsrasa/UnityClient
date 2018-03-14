using SimpleJSON;

namespace ROSBridgeLib {
	namespace fiducial_msgs {
		public class FiducialMsg : ROSBridgeMsg {
		    public int _fiducial_id; 
		    public int _direction; 
		    public double _x0; 
		    public double _y0; 
		    public double _x1; 
		    public double _y1; 
		    public double _x2; 
		    public double _y2; 
		    public double _x3; 
		    public double _y3;  

            public FiducialMsg(JSONNode msg)
            {
                _fiducial_id = int.Parse(msg["fiducial_id"]);
                _direction = int.Parse(msg["direction"]);
				_x0 = float.Parse(msg["x0"]);
				_x1 = float.Parse(msg["x1"]);
				_x2 = float.Parse(msg["x2"]);
				_x3 = float.Parse(msg["x3"]);
				_y0 = float.Parse(msg["y0"]);
				_y1 = float.Parse(msg["y1"]);
				_y2 = float.Parse(msg["y2"]);
				_y3 = float.Parse(msg["y3"]);
			}

			public FiducialMsg(int fiducialId, int direction, float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3)
			{
			    _fiducial_id = fiducialId;
			    _direction = direction;
			    _x0 = x0;
			    _y0 = y0;
			    _x1 = x1;
			    _y1 = y1;
			    _x2 = x2;
			    _y2 = y2;
			    _x3 = x3;
			    _y3 = y3;
			}
			
			public static string getMessageType() {
				return "fiducial_msgs/Fiducial";
			}
			
			public override string ToString()
			{
			    return string.Format("fiducial_msgs/Fiducial [id={0}][direction={1}][x0={2}, y0={3}][x1={4}, y1={5}][x2={6}, y2={7}][x3={8}, y3={9}]", 
                    _fiducial_id, _direction, _x0, _y0, _x1, _y1, _x2, _y2, _x3, _y3);
			}
					
			public override string ToYAMLString()
			{
			    return string.Format("{\"fiducial_id\": {0}, \"direction\": {1}, \"x0\": {2}, \"y0\": {3}, \"x1\": {4}, \"y1\": {5}, \"x2\": {6}, \"y2\": {7}, \"x3\": {8}, \"y3\": {9}}", 
                    _fiducial_id, _direction, _x0, _y0, _x1, _y1, _x2, _y2, _x3, _y3);
			}
		}
	}
}
