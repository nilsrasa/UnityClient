using System;
using System.Collections.Generic;
using ROSBridgeLib;
using ROSBridgeLib.geometry_msgs;
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
    private ROSGenericSubscriber<TwistMsg> _rosJoystick;
    private bool _hasJoystickDataToConsume;
    private TwistMsg _joystickDataToConsume;

    void Awake() {
        Instance = this;
        _rosAgents = new Dictionary<Type, ROSAgent>();
        _agentsWaitingToStart = new List<Type>();
        _rigidbody = GetComponent<Rigidbody>();
    }

    void Start() {
        _sensorRepresentationBusController = SensorRepresentationBusController.Instance;
    }

    void Update() {
        if (_rosBridge == null) return;
        if (_agentsWaitingToStart.Count > 0) {
            foreach (Type type in _agentsWaitingToStart) {
                StartAgent(type);
            }
            _agentsWaitingToStart = new List<Type>();
        }

        if (_hasJoystickDataToConsume)
        {
            _rigidbody.velocity = transform.forward * (float) _joystickDataToConsume._linear._x;
            _rigidbody.angularVelocity = new Vector3(0, (float)-_joystickDataToConsume._angular._z, 0);
            _hasJoystickDataToConsume = false;
        }
    }

    void DataReceived(ROSBridgeMsg data)
    {
        //TODO: Update SIM
        //_sensorRepresentationBusController.HandleData(sender, data);
    }

    private void ReceivedJoystickUpdate(ROSBridgeMsg data)
    {
        _joystickDataToConsume = (TwistMsg) data;
        _hasJoystickDataToConsume = true;
    }

    //TODO: Needs to be changed to better thing
    public void StartAgent(Type agentType) {
        /*
        if (_rosBridge == null) {
            _agentsWaitingToStart.Add(agentType);
            return;
        }
        ROSAgent agent = (ROSAgent)Activator.CreateInstance(agentType);
        agent.StartAgent(ROSAgent.AgentJob.Subscriber);
        _rosAgents.Add(agentType, agent);
        agent.DataWasReceived += DataReceived;
        */
    }

    public override void StopRobot()
    {
        throw new NotImplementedException();
    }

    public override void OverridePositionAndOrientation(Vector3 position, Quaternion orientation)
    {
        throw new NotImplementedException();
    }

    public override void OnSelected()
    {
        throw new NotImplementedException();
    }

    public override void OnDeselected()
    {
        throw new NotImplementedException();
    }

    protected override void StartROS()
    {
        _rosJoystick = new ROSGenericSubscriber<TwistMsg>(_rosBridge, "/teleop_velocity_smoother/raw_cmd_vel", TwistMsg.GetMessageType(), (msg) => new TwistMsg(msg));
        _rosJoystick.OnDataReceived += ReceivedJoystickUpdate;
    }

    public override void MoveDirect(Vector2 movementCommand)
    {
        throw new NotImplementedException();
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
