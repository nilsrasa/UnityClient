using System;
using System.Collections.Generic;
using Messages;
using Messages.geometry_msgs;
using Ros_CSharp;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class RobotVisualisation : ROSController
{
    [SerializeField] private bool _runConjoinedWithSim;
    [SerializeField] private string _ROS_MASTER_URI = "127.0.0.1:11311";

    private SensorRepresentationBusController _sensorRepresentationBusController;
    private Dictionary<Type, ROSAgent> _rosAgents;
    private List<Type> _agentsWaitingToStart;
    private Rigidbody _rigidbody;

    public static RobotVisualisation Instance { get; private set; }

    private ROSLocomotionDirect _rosLocomotionDirect;
    private ROSJoystick _rosJoystick;
    private bool _hasJoystickDataToConsume;
    private Twist _joystickDataToConsume;

    void Awake() {
        Instance = this;
        _rosAgents = new Dictionary<Type, ROSAgent>();
        _agentsWaitingToStart = new List<Type>();
        _rigidbody = GetComponent<Rigidbody>();
    }

    void Start() {
        _sensorRepresentationBusController = SensorRepresentationBusController.Instance;
        if (!_runConjoinedWithSim)
            StartROS(_ROS_MASTER_URI);
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

        if (_hasJoystickDataToConsume)
        {
            _rigidbody.velocity = transform.forward * (float) _joystickDataToConsume.linear.x;
            _rigidbody.angularVelocity = new Vector3(0, (float)-_joystickDataToConsume.angular.z, 0);
            _hasJoystickDataToConsume = false;
        }
    }

    void DataReceived(ROSAgent sender, IRosMessage data)
    {
        _sensorRepresentationBusController.HandleData(sender, data);
    }

    private void ReceivedJoystickUpdate(ROSAgent sender, IRosMessage data)
    {
        _joystickDataToConsume = (Twist) data;
        _hasJoystickDataToConsume = true;
    }

    public void StartAgent(Type agentType) {
        if (!(ROS.ok || ROS.isStarted())) {
            _agentsWaitingToStart.Add(agentType);
            return;
        }
        ROSAgent agent = (ROSAgent)Activator.CreateInstance(agentType);
        agent.StartAgent(ROSAgent.AgentJob.Subscriber);
        _rosAgents.Add(agentType, agent);
        agent.DataWasReceived += DataReceived;
    }

    public override void StartROS(string uri)
    {
        base.StartROS(uri);

        _rosJoystick = new ROSJoystick();
        _rosJoystick.StartAgent(ROSAgent.AgentJob.Subscriber);
        _rosJoystick.DataWasReceived += ReceivedJoystickUpdate;
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
