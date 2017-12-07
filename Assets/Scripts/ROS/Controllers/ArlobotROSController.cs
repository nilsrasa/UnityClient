using Assets.Scripts;
using Messages;
using Messages.sensor_msgs;
using Messages.std_msgs;
using UnityEngine;

public class ArlobotROSController : ROSController {

    [SerializeField] private string _ROS_MASTER_URI = "127.0.0.1:11311";

    public static ArlobotROSController Instance { get; private set; }

    private ROSLocomotionWaypoint _rosLocomotionWaypoint;
    private ROSLocomotionWaypointState _rosLocomotionWaypointState;
    private ROSUltrasound _rosUltrasound;
    private ROSTransformPosition _rosTransformPosition;
    private ROSTransformHeading _rosTransformHeading;

    void Awake()
    {
        Instance = this;
    }

    public override void StartROS() {
        base.StartROS();
        _rosLocomotionWaypoint = new ROSLocomotionWaypoint();
        _rosLocomotionWaypoint.StartAgent(ROSAgent.AgentJob.Publisher, _clientNamespace);
        _rosLocomotionWaypointState = new ROSLocomotionWaypointState();
        _rosLocomotionWaypointState.StartAgent(ROSAgent.AgentJob.Publisher, _clientNamespace);
        _rosUltrasound = new ROSUltrasound();
        _rosUltrasound.StartAgent(ROSAgent.AgentJob.Subscriber, _clientNamespace);
        _rosTransformPosition = new ROSTransformPosition();
        _rosTransformPosition.StartAgent(ROSAgent.AgentJob.Subscriber, _clientNamespace);
        _rosTransformPosition.DataWasReceived += ReceivedPositionUpdate;
        _rosTransformHeading = new ROSTransformHeading();
        _rosTransformHeading.StartAgent(ROSAgent.AgentJob.Subscriber, _clientNamespace);
        _rosTransformHeading.DataWasReceived += ReceivedHeadingUpdate;
    }

    public void Move(Vector3 position)
    {
        GeoPointWGS84 point = position.ToMercator().ToWGS84();
        Debug.Log("Waypoint - Mercator: " + position.ToMercator());
        Debug.Log("Waypoint - WGS84: " + point);
        _rosLocomotionWaypoint.PublishData(point);
    }

    public void ReceivedPositionUpdate(ROSAgent sender, IRosMessage position)
    {
        //In WGS84
        NavSatFix nav = (NavSatFix) position;
        GeoPointWGS84 geoPoint = new GeoPointWGS84
        {
            latitude = nav.latitude,
            longitude = nav.longitude,
            altitude = nav.altitude,
        };
        if (GeoUtils.MercatorOriginSet)
            transform.position = geoPoint.ToMercator().ToUnity();
    }

    public void ReceivedHeadingUpdate(ROSAgent sender, IRosMessage heading)
    {
        Float32 f = (Float32) heading;
        transform.rotation = Quaternion.Euler(0, f.data, 0);
    }
}