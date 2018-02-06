using System;
using System.Collections;
using System.Collections.Generic;
using Messages;
using Messages.geometry_msgs;
using Messages.sensor_msgs;
using Messages.std_msgs;
using Ros_CSharp;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class VirtualRobot : ROSController
{
    private SensorBusController _sensorBusController;
    private Dictionary<Type, ROSAgent> _rosAgents;
    private List<Type> _agentsWaitingToStart;
    private Rigidbody _rigidbody;
    private string _rosMasterUri;

    //Subscribers
    private ROSLocomotionDirect _rosLocomotionDirect;
    private bool _hasLocomotionDirectDataToConsume;
    private Twist _locomotionDirectDataToConsume;
    private ROSJoystick _rosJoystick;
    private bool _hasJoystickDataToConsume;
    private Twist _joystickDataToConsume;

    //Publishers
    private ROSTransformPosition _rosTransformPosition;
    private ROSTransformHeading _rosTransformHeading;
    private Coroutine _transformUpdateCoroutine;

    void Awake() {
        _rosAgents = new Dictionary<Type, ROSAgent>();
        _agentsWaitingToStart = new List<Type>();
        _rigidbody = GetComponent<Rigidbody>();
        _rosMasterUri = ConfigManager.ConfigFile.RosMasterUri;
    }

    void Start() {
        _sensorBusController = new SensorBusController(this);
        StartROS(_rosMasterUri);
    }

    void Update() {
        if (!(ROS.ok || ROS.isStarted()))
            return;
        if (_agentsWaitingToStart.Count > 0) {
            foreach (Type type in _agentsWaitingToStart) {
                StartAgent(type);
            }
            _agentsWaitingToStart = new List<Type>();
        }

        if (_hasJoystickDataToConsume) {
            _rigidbody.velocity = transform.forward * (float)_joystickDataToConsume.linear.x;
            _rigidbody.angularVelocity = new Vector3(0, (float)-_joystickDataToConsume.angular.z, 0);
            _hasJoystickDataToConsume = false;
        }
        if (_hasLocomotionDirectDataToConsume)
        {
            _rigidbody.velocity = transform.forward * (float)_locomotionDirectDataToConsume.linear.x;
            _rigidbody.angularVelocity = new Vector3(0, (float)-_locomotionDirectDataToConsume.angular.z, 0);
            _hasLocomotionDirectDataToConsume = false;
        }
    }

    private void ReceivedJoystickUpdate(ROSAgent sender, IRosMessage data) {
        _joystickDataToConsume = (Twist)data;
        _hasJoystickDataToConsume = true;
    }

    private void ReceivedLocomotionDirectUpdate(ROSAgent sender, IRosMessage data)
    {
        _locomotionDirectDataToConsume = (Twist) data;
        _hasLocomotionDirectDataToConsume = true;
    }

    private IEnumerator SendTransformUpdate( )
    {
        while (true)
        {
            GeoPointWGS84 wgs = transform.position.ToUTM().ToWGS84();
            NavSatFix pos = new NavSatFix
            {
                altitude = wgs.altitude,
                longitude = wgs.longitude,
                latitude = wgs.latitude,
            };
            _rosTransformPosition.PublishData(pos);

            Float32 rot = new Float32();
            rot.data = transform.eulerAngles.y;
            _rosTransformHeading.PublishData(rot);

            yield return new WaitForEndOfFrame();
        }
    }

    private void TransmitSensorData() {
        foreach (SensorBus sensorBus in _sensorBusController.SensorBusses) {
            if (!_rosAgents.ContainsKey(sensorBus.ROSAgentType)) continue;
            _rosAgents[sensorBus.ROSAgentType].PublishData(sensorBus.GetSensorData());
        }
    }

    public override void StopROS() {
        StopCoroutine(_transformUpdateCoroutine);
        base.StopROS();
    }

    public void StartAgent(Type agentType)
    {
        if (!(ROS.ok || ROS.isStarted()))
        {
            _agentsWaitingToStart.Add(agentType);
            return;
        }
        ROSAgent agent = (ROSAgent) Activator.CreateInstance(agentType);
        agent.StartAgent(ROSAgent.AgentJob.Publisher);
        _rosAgents.Add(agentType, agent);
    }

    public override void StartROS(string uri) {
        base.StartROS(uri);

        _rosLocomotionDirect = new ROSLocomotionDirect();
        _rosLocomotionDirect.StartAgent(ROSAgent.AgentJob.Subscriber);
        _rosLocomotionDirect.DataWasReceived += ReceivedLocomotionDirectUpdate;
        _rosJoystick = new ROSJoystick();
        _rosJoystick.StartAgent(ROSAgent.AgentJob.Subscriber);
        _rosJoystick.DataWasReceived += ReceivedJoystickUpdate;

        _rosTransformPosition = new ROSTransformPosition();
        _rosTransformPosition.StartAgent(ROSAgent.AgentJob.Publisher);
        _rosTransformHeading = new ROSTransformHeading();
        _rosTransformHeading.StartAgent(ROSAgent.AgentJob.Publisher);
        _transformUpdateCoroutine = StartCoroutine(SendTransformUpdate());
    }

    public override void MoveToPoint(GeoPointWGS84 point) {
    }

    public override void MovePath(List<GeoPointWGS84> waypoints) {
    }

    public override void PausePath() {
    }

    public override void ResumePath() {
    }

}