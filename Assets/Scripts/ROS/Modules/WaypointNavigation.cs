using Messages;
using Messages.geometry_msgs;
using Messages.sensor_msgs;
using Messages.std_msgs;
using UnityEngine;
using String = Messages.std_msgs.String;
using Time = UnityEngine.Time;
using Vector3 = UnityEngine.Vector3;

public class WaypointNavigation : MonoBehaviour
{

    [SerializeField] private float _publishInterval = 0.2f;

    private float _publishTimer = 0;

    private float k_rho = 0.3f;
    private float k_alpha = 0.8f;
    private string state = "STOP";
    private string subState = "STOP";
    private bool goal_set = false;
    private float distance = 0;
    private float angle = Mathf.PI;
    private Vector3 goal = Vector3.zero;
    private Twist vel = new Twist();
    private float vel_maxlin = 1;
    private float vel_maxang = 2;
    private string prestate = "STOP";

    //Publishers
    private ROSLocomotionDirect _rosLocomotionDirect;
    private ROSLocomotionState _rosLocomotionState;

    //Subscribers
    private ROSLocomotionWaypoint _rosLocomotionWaypoint;
    private ROSLocomotionWaypointState _rosLocomotionWaypointState;
    private ROSLocomotionSpeedParams _rosLocomotionSpeedParams;
    private ROSLocomotionLinearSpeed _rosLocomotionLinear;
    private ROSLocomotionAngularSpeed _rosLocomotionAngular;

    void Awake()
    {
        vel = new Twist
        {
            angular = new Messages.geometry_msgs.Vector3
            {
                x = 0, y = 0, z = 0
            },
            linear = new Messages.geometry_msgs.Vector3
            {
                x = 0, y = 0, z = 0
            }
        };
    }

    void Update()
    {
        if (!goal_set)
        {
            _rosLocomotionState.PublishData(new String() {data = subState});
            return;
        }
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
                        vel.linear.x = 0;
                        vel.angular.z = k_alpha * angle;
                        if (Mathf.Abs(angle) < 0.2f)
                            subState = "FORWARDING";
                        break;
                    case "FORWARDING":
                        if (angle > Mathf.PI / 2)
                        {
                            vel.linear.x = -k_rho * distance;
                            vel.angular.z = k_alpha * (angle - Mathf.PI);
                        }
                        else if (angle < -Mathf.PI / 2)
                        {
                            vel.linear.x = -k_rho * distance;
                            vel.angular.z = k_alpha * (angle + Mathf.PI);
                        }
                        else
                        {
                            vel.linear.x = k_rho * distance;
                            vel.angular.z = k_alpha * angle;
                        }

                        vel.linear.x = Mathf.Clamp((float) vel.linear.x, -vel_maxlin, vel_maxlin);
                        vel.angular.z = Mathf.Clamp((float) vel.angular.z, -vel_maxang, vel_maxang);

                        PublishVelocity(vel);
                        break;
                }

                PublishVelocity(vel);
                break;
            case "PARK":
                subState = "STOP";
                vel.linear.x = 0;
                vel.angular.z = 0;
                PublishVelocity(vel);
                break;
            default:
                if (prestate == "RUNNING")
                {
                    subState = "STOP";
                    vel.linear.x = 0;
                    vel.angular.z = 0;
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

    public void InitialiseRos()
    {
        _rosLocomotionDirect = new ROSLocomotionDirect();
        _rosLocomotionDirect.StartAgent(ROSAgent.AgentJob.Publisher);
        _rosLocomotionState = new ROSLocomotionState();
        _rosLocomotionState.StartAgent(ROSAgent.AgentJob.Publisher);

        _rosLocomotionWaypoint = new ROSLocomotionWaypoint();
        _rosLocomotionWaypoint.StartAgent(ROSAgent.AgentJob.Subscriber);
        _rosLocomotionWaypoint.DataWasReceived += ReceivedWaypoint;
        _rosLocomotionWaypointState = new ROSLocomotionWaypointState();
        _rosLocomotionWaypointState.StartAgent(ROSAgent.AgentJob.Subscriber);
        _rosLocomotionWaypointState.DataWasReceived += ReceivedNavigationState;
        _rosLocomotionSpeedParams = new ROSLocomotionSpeedParams();
        _rosLocomotionSpeedParams.StartAgent(ROSAgent.AgentJob.Subscriber);
        _rosLocomotionSpeedParams.DataWasReceived += ReceivedNavigationParameters;
        _rosLocomotionAngular = new ROSLocomotionAngularSpeed();
        _rosLocomotionAngular.StartAgent(ROSAgent.AgentJob.Subscriber);
        _rosLocomotionAngular.DataWasReceived += ReceivedNavigationAngularSpeedParameter;
        _rosLocomotionLinear = new ROSLocomotionLinearSpeed();
        _rosLocomotionLinear.StartAgent(ROSAgent.AgentJob.Subscriber);
        _rosLocomotionLinear.DataWasReceived += ReceivedNavigationLinearSpeedParameter;
    }

    private void PublishVelocity(Twist twist)
    {
        _rosLocomotionDirect.PublishData(new Vector2((float)vel.angular.z, (float)vel.linear.x));
    }

    private void ReceivedNavigationParameters(ROSAgent sender, IRosMessage parameters)
    {
        String data = (String) parameters;
        string[] split = data.data.Split(',');
        k_rho = float.Parse(split[0]);
        k_alpha = float.Parse(split[1]);
    }

    private void ReceivedNavigationLinearSpeedParameter(ROSAgent sender, IRosMessage parameter)
    {
        Float32 data = (Float32) parameter;
        vel_maxlin = data.data;
    }

    private void ReceivedNavigationAngularSpeedParameter(ROSAgent sender, IRosMessage parameter)
    {
        Float32 data = (Float32) parameter;
        vel_maxang = data.data;
    }

    public void SetNavigationMovementParameters(Float32 maxLinearVel = null, Float32 maxAngularVel = null) 
    {
        if (maxLinearVel != null)
            vel_maxlin = maxLinearVel.data;

        if (maxAngularVel != null)
            vel_maxang = maxAngularVel.data;
    }

    public void ReceivedNavigationState(ROSAgent sender, IRosMessage newState)
    {
        String data = (String)newState;
        state = data.data;
    }

    public void ReceivedWaypoint(ROSAgent sender, IRosMessage newGoal)
    {
        NavSatFix waypoint = (NavSatFix) newGoal;
        GeoPointWGS84 coordinate = new GeoPointWGS84()
        {
            latitude = waypoint.latitude,
            longitude = waypoint.longitude,
            altitude = waypoint.altitude
        };
        goal = coordinate.ToUTM().ToUnity();
        goal_set = true;
        subState = "STOP";
    }

}
