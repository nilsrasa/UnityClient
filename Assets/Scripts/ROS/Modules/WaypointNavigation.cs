using ROSBridgeLib;
using ROSBridgeLib.geometry_msgs;
using ROSBridgeLib.sensor_msgs;
using ROSBridgeLib.std_msgs;
using UnityEngine;

public class WaypointNavigation : MonoBehaviour
{

    [SerializeField] private float _publishInterval = 0.2f;

    private float _publishTimer = 0;
    private bool _sentCommand;

    private float k_rho = 0.3f;
    private float k_alpha = 0.8f;
    private string state = "STOP";
    private string subState = "STOP";
    private bool goal_set = false;
    private float distance = 0;
    private float angle = Mathf.PI;
    private Vector3 goal = Vector3.zero;
    private TwistMsg vel;
    private float vel_maxlin = 1;
    private float vel_maxang = 2;
    private string prestate = "STOP";

    //Publishers
    private ROSLocomotionDirect _rosLocomotionDirect;
    private ROSGenericPublisher _rosLocomotionState;

    //Subscribers
    private ROSLocomotionWaypoint _rosLocomotionWaypoint;
    private ROSLocomotionWaypointState _rosLocomotionWaypointState;
    private ROSLocomotionControlParams _rosLocomotionControlParams;
    private ROSGenericSubscriber<Float32Msg> _rosLocomotionLinear;
    private ROSGenericSubscriber<Float32Msg> _rosLocomotionAngular;

    void Awake()
    {
        vel = new TwistMsg(
            new Vector3Msg(0, 0, 0),
            new Vector3Msg(0, 0, 0));
    }

    void Update()
    {
        if (!goal_set)
        {
            if (!_sentCommand)
            {
                _rosLocomotionState.PublishData(new StringMsg(subState));
                _sentCommand = true;
            }
            return;
        }
        else
            _sentCommand = false;

        angle = Vector3.SignedAngle(goal - transform.position, transform.forward, transform.up) * Mathf.Deg2Rad;
        distance = Vector3.Distance(transform.position, goal);
        switch (state)
        {
            case "RUNNING":
                switch (subState)
                {
                    default:
                        subState = "TURNING";
                        break;
                    case "TURNING":
                        vel._linear._x = 0;
                        vel._angular._z = k_alpha * angle;
                        if (Mathf.Abs(angle) < 0.2f)
                            subState = "FORWARDING";
                        break;
                    case "FORWARDING":
                        if (angle > Mathf.PI / 2)
                        {
                            vel._linear._x = -k_rho * distance;
                            vel._angular._z = k_alpha * (angle - Mathf.PI);
                        }
                        else if (angle < -Mathf.PI / 2)
                        {
                            vel._linear._x = -k_rho * distance;
                            vel._angular._z = k_alpha * (angle + Mathf.PI);
                        }
                        else
                        {
                            vel._linear._x = k_rho * distance;
                            vel._angular._z = k_alpha * angle;
                        }

                        vel._linear._x = Mathf.Clamp((float) vel._linear._x, -vel_maxlin, vel_maxlin);
                        vel._angular._z = Mathf.Clamp((float) vel._angular._z, -vel_maxang, vel_maxang);

                        PublishVelocity(vel);
                        break;
                }

                PublishVelocity(vel);
                break;
            case "PARK":
                subState = "STOP";
                vel._linear._x = 0;
                vel._angular._z = 0;
                PublishVelocity(vel);
                break;
            default:
                if (prestate == "RUNNING")
                {
                    subState = "STOP";
                    vel._linear._x = 0;
                    vel._angular._z = 0;
                    PublishVelocity(vel);
                }
                else
                {
                    subState = "IDLE";
                }
                break;
        }

        prestate = state;
        _publishTimer -= Time.deltaTime;

        if (_publishTimer <= 0)
        {
            _publishTimer = _publishInterval;
            _rosLocomotionWaypointState.PublishData(subState);
        }
    }

    public void InitialiseRos(ROSBridgeWebSocketConnection rosConnection)
    {
        _rosLocomotionDirect = new ROSLocomotionDirect(ROSAgent.AgentJob.Publisher, rosConnection, "/cmd_vel");
        _rosLocomotionState = new ROSGenericPublisher(rosConnection, "/waypoint/robot_state", StringMsg.GetMessageType());

        _rosLocomotionWaypoint = new ROSLocomotionWaypoint(ROSAgent.AgentJob.Subscriber, rosConnection, "/waypoint");
        _rosLocomotionWaypoint.OnDataReceived += ReceivedWaypoint;
        _rosLocomotionWaypointState = new ROSLocomotionWaypointState(ROSAgent.AgentJob.Subscriber, rosConnection, "/waypoint/state");
        _rosLocomotionWaypointState.OnDataReceived += ReceivedNavigationState;
        _rosLocomotionControlParams = new ROSLocomotionControlParams(ROSAgent.AgentJob.Subscriber, rosConnection, "/waypoint/control_parameters");
        _rosLocomotionControlParams.OnDataReceived += ReceivedNavigationParameters;
        _rosLocomotionAngular = new ROSGenericSubscriber<Float32Msg>(rosConnection, "/waypoint/max_angular_speed", Float32Msg.GetMessageType(), (msg) => new Float32Msg(msg));
        _rosLocomotionAngular.OnDataReceived += ReceivedNavigationAngularSpeedParameter;
        _rosLocomotionLinear = new ROSGenericSubscriber<Float32Msg>(rosConnection, "/waypoint/max_linear_speed", Float32Msg.GetMessageType(), (msg) => new Float32Msg(msg));
        _rosLocomotionLinear.OnDataReceived += ReceivedNavigationLinearSpeedParameter;
    }

    private void PublishVelocity(TwistMsg twist)
    {
        _rosLocomotionDirect.PublishData((float)twist._linear._x, (float)twist._angular._z);
    }

    private void ReceivedNavigationParameters(ROSBridgeMsg parameters)
    {
        StringMsg data = (StringMsg) parameters;
        string[] split = data._data.Split(',');
        k_rho = float.Parse(split[0]);
        k_alpha = float.Parse(split[1]);
    }

    private void ReceivedNavigationLinearSpeedParameter(ROSBridgeMsg parameter)
    {
        Float32Msg data = (Float32Msg) parameter;
        vel_maxlin = data._data;
    }

    private void ReceivedNavigationAngularSpeedParameter(ROSBridgeMsg parameter)
    {
        Float32Msg data = (Float32Msg) parameter;
        vel_maxang = data._data;
    }

    public void SetNavigationMovementParameters(Float32Msg maxLinearVel = null, Float32Msg maxAngularVel = null) 
    {
        if (maxLinearVel != null)
            vel_maxlin = maxLinearVel._data;

        if (maxAngularVel != null)
            vel_maxang = maxAngularVel._data;
    }

    public void ReceivedNavigationState(ROSBridgeMsg newState)
    {
        StringMsg data = (StringMsg)newState;
        state = data._data;
    }

    public void ReceivedWaypoint(ROSBridgeMsg newGoal)
    {
        NavSatFixMsg waypoint = (NavSatFixMsg) newGoal;
        GeoPointWGS84 coordinate = new GeoPointWGS84()
        {
            latitude = waypoint._latitude,
            longitude = waypoint._longitude,
            altitude = waypoint._altitude
        };
        goal = coordinate.ToUTM().ToUnity();
        goal_set = true;
        subState = "STOP";
    }

}
