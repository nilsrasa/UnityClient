using SimpleJSON;

namespace ROSBridgeLib
{
    namespace geometry_msgs
    {
        public class TransformMsg : ROSBridgeMsg
        {
            public Vector3Msg _translation;
            public QuaternionMsg _rotation;

            public TransformMsg(JSONNode msg)
            {
                _translation = new Vector3Msg(msg["translation"]);
                _rotation = new QuaternionMsg(msg["rotation"]);
            }

            public TransformMsg(Vector3Msg translation, QuaternionMsg rotation)
            {
                _translation = translation;
                _rotation = rotation;
            }

            public static string GetMessageType()
            {
                return "geometry_msgs/Transform";
            }

            public override string ToString()
            {
                return string.Format("geometry_msgs/Transform [translation={0}][rotation={1}]", _translation, _rotation);
            }

            public override string ToYAMLString()
            {
                return "{\"translation\" : " + _translation.ToYAMLString() + ",\"rotation\" :" + _rotation.ToYAMLString() + "}";
            }
        }
    }
}